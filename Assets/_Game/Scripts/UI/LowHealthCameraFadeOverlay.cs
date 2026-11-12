using System.Collections;
using EmpireBuilder.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    /// <summary>
    /// Immersive low-health screen darkening and fainting camera fade effect.
    /// When player health decreases (due to starvation, hunger, or lack of eating),
    /// the camera view progressively darkens with a realistic peripheral vignette,
    /// tunnel vision, and rhythmic fainting pulses at critical HP.
    /// When the player eats at restaurants/cafes or receives hospital treatment,
    /// vision smoothly transitions back to clear, normal brightness.
    /// </summary>
    [DisallowMultipleComponent]
    public class LowHealthCameraFadeOverlay : MonoBehaviour
    {
        public static LowHealthCameraFadeOverlay Instance { get; private set; }

        [Header("Health Thresholds")]
        [Tooltip("Health level below which darkening begins (e.g. 60 HP)")]
        [SerializeField] private float healthDarkenThreshold = 60f;

        [Tooltip("Health level where critical fainting heartbeat pulse begins (e.g. 18 HP)")]
        [SerializeField] private float criticalThreshold = 18f;

        [Header("Darkness Settings")]
        [Tooltip("Maximum screen darkening opacity (0.0 to 1.0)")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float maxDarkness = 0.82f;

        [Tooltip("Maximum vignette tunnel vision opacity")]
        [Range(0.5f, 1.0f)]
        [SerializeField] private float maxVignetteAlpha = 0.92f;

        [Tooltip("Speed of darkness fade transitions")]
        [SerializeField] private float fadeLerpSpeed = 3.2f;

        [Tooltip("Fainting heartbeat pulse speed when critically low")]
        [SerializeField] private float pulseFrequency = 2.8f;

        [Tooltip("Fainting heartbeat pulse amplitude")]
        [SerializeField] private float pulseMagnitude = 0.08f;

        [Header("UI Element References")]
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private Image blackoutImage;
        [SerializeField] private Image vignetteImage;

        // Runtime state
        private float currentFadeAlpha = 0f;
        private float targetFadeAlpha = 0f;
        private float currentVignetteAlpha = 0f;
        private float targetVignetteAlpha = 0f;
        private Sprite cachedVignetteSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureOverlayUI();
        }

        private void Start()
        {
            UpdateDarknessValues(true);
        }

        private void OnEnable()
        {
            if (PlayerHealthManager.Instance != null)
            {
                PlayerHealthManager.Instance.OnHealthChanged += HandleHealthChanged;
                PlayerHealthManager.Instance.OnIllnessStateChanged += HandleIllnessStateChanged;
            }
        }

        private void OnDisable()
        {
            if (PlayerHealthManager.Instance != null)
            {
                PlayerHealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
                PlayerHealthManager.Instance.OnIllnessStateChanged -= HandleIllnessStateChanged;
            }
        }

        private void HandleHealthChanged(float curHp, float maxHp)
        {
            UpdateDarknessValues(false);
        }

        private void HandleIllnessStateChanged(bool isIll, string reason)
        {
            UpdateDarknessValues(false);
        }

        private void Update()
        {
            // Calculate dynamic targets
            CalculateTargetDarkness();

            // Smoothly interpolate current alpha towards target
            currentFadeAlpha = Mathf.MoveTowards(currentFadeAlpha, targetFadeAlpha, Time.deltaTime * fadeLerpSpeed);
            currentVignetteAlpha = Mathf.MoveTowards(currentVignetteAlpha, targetVignetteAlpha, Time.deltaTime * fadeLerpSpeed);

            ApplyAlphaToUI();
        }

        private void CalculateTargetDarkness()
        {
            float curHp = (PlayerHealthManager.Instance != null) ? PlayerHealthManager.Instance.CurrentHealth : 100f;
            bool isIll = (PlayerHealthManager.Instance != null) && PlayerHealthManager.Instance.IsIll;

            if (curHp >= healthDarkenThreshold && !isIll)
            {
                targetFadeAlpha = 0f;
                targetVignetteAlpha = 0f;
                return;
            }

            // Health is below threshold or player is ill
            float hungerProgress;
            if (curHp < healthDarkenThreshold)
            {
                hungerProgress = Mathf.Clamp01(1f - (curHp / healthDarkenThreshold));
            }
            else
            {
                hungerProgress = 0.3f; // Ill but health slightly elevated from food
            }

            // Non-linear perceptual darkening curve (smooth cubic acceleration towards zero)
            float curve = Mathf.Pow(hungerProgress, 1.25f);
            float baseBlackout = curve * maxDarkness;
            float baseVignette = Mathf.Clamp01(curve * 1.25f) * maxVignetteAlpha;

            // Extra darkness penalty if suffering from severe illness
            if (isIll)
            {
                baseBlackout = Mathf.Clamp(baseBlackout + 0.08f, 0f, maxDarkness);
                baseVignette = Mathf.Clamp(baseVignette + 0.10f, 0f, maxVignetteAlpha);
            }

            // If critical, introduce pulsating fainting / consciousness loss
            if (curHp <= criticalThreshold)
            {
                float pulse = Mathf.Sin(Time.time * pulseFrequency) * pulseMagnitude;
                baseBlackout = Mathf.Clamp(baseBlackout + pulse, 0f, 0.95f);
                baseVignette = Mathf.Clamp(baseVignette + (pulse * 1.2f), 0f, 0.98f);
            }

            targetFadeAlpha = baseBlackout;
            targetVignetteAlpha = baseVignette;
        }

        private void UpdateDarknessValues(bool immediate)
        {
            CalculateTargetDarkness();
            if (immediate)
            {
                currentFadeAlpha = targetFadeAlpha;
                currentVignetteAlpha = targetVignetteAlpha;
                ApplyAlphaToUI();
            }
        }

        private void ApplyAlphaToUI()
        {
            if (overlayCanvasGroup != null)
            {
                // Root is active whenever there is any darkness
                bool shouldBeActive = currentFadeAlpha > 0.001f || currentVignetteAlpha > 0.001f;
                overlayCanvasGroup.gameObject.SetActive(shouldBeActive);
            }

            if (blackoutImage != null)
            {
                Color c = blackoutImage.color;
                c.a = currentFadeAlpha;
                blackoutImage.color = c;
            }

            if (vignetteImage != null)
            {
                Color vc = vignetteImage.color;
                vc.a = currentVignetteAlpha;
                vignetteImage.color = vc;
            }
        }

        /// <summary>
        /// Ensures all UI elements, canvas group, and procedural vignette texture exist.
        /// </summary>
        public void EnsureOverlayUI()
        {
            Transform existingRoot = transform.Find("LowHealthFadeOverlay");
            GameObject rootObj;

            if (existingRoot == null)
            {
                rootObj = new GameObject("LowHealthFadeOverlay", typeof(RectTransform));
                rootObj.transform.SetParent(transform, false);

                RectTransform rt = rootObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);

                // Ensure it renders above background/world, but modals remain accessible
                rootObj.transform.SetSiblingIndex(1);
            }
            else
            {
                rootObj = existingRoot.gameObject;
            }

            // Ensure CanvasGroup
            overlayCanvasGroup = rootObj.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
            {
                overlayCanvasGroup = rootObj.AddComponent<CanvasGroup>();
            }
            overlayCanvasGroup.blocksRaycasts = false;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.ignoreParentGroups = true;

            // 1. Blackout Image Layer
            Transform blackoutTr = rootObj.transform.Find("BlackoutLayer");
            if (blackoutTr == null)
            {
                GameObject boObj = new GameObject("BlackoutLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                boObj.transform.SetParent(rootObj.transform, false);

                RectTransform boRt = boObj.GetComponent<RectTransform>();
                boRt.anchorMin = Vector2.zero;
                boRt.anchorMax = Vector2.one;
                boRt.anchoredPosition = Vector2.zero;
                boRt.sizeDelta = Vector2.zero;

                blackoutImage = boObj.GetComponent<Image>();
                blackoutImage.color = new Color(0.02f, 0.02f, 0.04f, 0f);
                blackoutImage.raycastTarget = false;
            }
            else
            {
                blackoutImage = blackoutTr.GetComponent<Image>();
            }

            // 2. Vignette Tunnel Vision Layer
            Transform vignetteTr = rootObj.transform.Find("VignetteLayer");
            if (vignetteTr == null)
            {
                GameObject vgObj = new GameObject("VignetteLayer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                vgObj.transform.SetParent(rootObj.transform, false);

                RectTransform vgRt = vgObj.GetComponent<RectTransform>();
                vgRt.anchorMin = Vector2.zero;
                vgRt.anchorMax = Vector2.one;
                vgRt.anchoredPosition = Vector2.zero;
                vgRt.sizeDelta = Vector2.zero;

                vignetteImage = vgObj.GetComponent<Image>();
                vignetteImage.raycastTarget = false;
            }
            else
            {
                vignetteImage = vignetteTr.GetComponent<Image>();
            }

            if (vignetteImage != null)
            {
                if (cachedVignetteSprite == null)
                {
                    cachedVignetteSprite = CreateProceduralVignetteSprite();
                }
                vignetteImage.sprite = cachedVignetteSprite;
                vignetteImage.type = Image.Type.Simple;
                vignetteImage.color = new Color(0.01f, 0.01f, 0.02f, 0f);
            }
        }

        /// <summary>
        /// Generates a smooth, high-fidelity radial vignette texture in memory.
        /// Fully compatible with WebGL and all graphics tiers with zero external asset dependencies.
        /// </summary>
        private Sprite CreateProceduralVignetteSprite()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Procedural_Starvation_Vignette";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = size * 0.5f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Elliptical distance to match widescreen monitors
                    float dx = (x - center.x) / maxRadius;
                    float dy = (y - center.y) / maxRadius;
                    float dist = Mathf.Sqrt(dx * dx * 0.85f + dy * dy * 1.15f);

                    // Clear tunnel in the center, smooth falloff into deep darkness at edges
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1.10f, dist));
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true); // Upload to GPU and make non-readable to save memory
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Auto-attaches to the GamePlayCanvas if not already instantiated in the scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToScene()
        {
            if (Instance != null) return;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas targetCanvas = null;

            foreach (var c in canvases)
            {
                if (c.gameObject.name == "GamePlayCanvas" || c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    targetCanvas = c;
                    break;
                }
            }

            if (targetCanvas != null)
            {
                var existing = targetCanvas.GetComponent<LowHealthCameraFadeOverlay>();
                if (existing == null)
                {
                    targetCanvas.gameObject.AddComponent<LowHealthCameraFadeOverlay>();
                    Debug.Log("[LowHealthCameraFadeOverlay] Auto-attached to " + targetCanvas.name);
                }
            }
        }
    }
}
