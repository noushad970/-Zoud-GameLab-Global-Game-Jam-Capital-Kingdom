using System.Collections;
using EmpireBuilder.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    /// <summary>
    /// Displays the player's health condition via a stylized HUD health bar.
    /// Includes animated fill transitions, dynamic color grading, low-health pulsing alerts,
    /// and numerical HP readout.
    /// </summary>
    public class PlayerHealthBarUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthTrackImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image heartIconImage;
        [SerializeField] private GameObject warningBadge;
        [SerializeField] private TextMeshProUGUI warningBadgeText;

        [Header("Color Gradient")]
        [SerializeField] private Color fullHealthColor = new Color(0.18f, 0.82f, 0.44f); // Vibrant Green
        [SerializeField] private Color midHealthColor = new Color(0.96f, 0.65f, 0.12f);  // Amber Yellow
        [SerializeField] private Color lowHealthColor = new Color(0.92f, 0.25f, 0.25f);  // Danger Red

        [Header("Animation Settings")]
        [SerializeField] private float smoothLerpSpeed = 7f;

        private float targetFill = 1f;
        private float currentDisplayFill = 1f;
        private float targetHealth = 100f;
        private float currentDisplayHealth = 100f;
        private float maxHealth = 100f;
        private bool isLowHealth = false;
        private bool isCriticalHealth = false;
        private Vector3 originalHeartScale = Vector3.one;

        private void Awake()
        {
            EnsureBindings();
            if (heartIconImage != null)
            {
                originalHeartScale = heartIconImage.transform.localScale;
            }
        }

        private void Start()
        {
            HookHealthManager();
        }

        private void OnEnable()
        {
            HookHealthManager();
        }

        private void OnDisable()
        {
            UnhookHealthManager();
        }

        private void HookHealthManager()
        {
            if (PlayerHealthManager.Instance != null)
            {
                PlayerHealthManager.Instance.OnHealthChanged += HandleHealthChanged;
                PlayerHealthManager.Instance.OnLowHealthStateChanged += HandleLowHealthStateChanged;
                PlayerHealthManager.Instance.OnWarningTriggered += HandleWarningTriggered;

                // Sync immediately
                HandleHealthChanged(PlayerHealthManager.Instance.CurrentHealth, PlayerHealthManager.Instance.MaxHealth);
            }
        }

        private void UnhookHealthManager()
        {
            if (PlayerHealthManager.Instance != null)
            {
                PlayerHealthManager.Instance.OnHealthChanged -= HandleHealthChanged;
                PlayerHealthManager.Instance.OnLowHealthStateChanged -= HandleLowHealthStateChanged;
                PlayerHealthManager.Instance.OnWarningTriggered -= HandleWarningTriggered;
            }
        }

        private void Update()
        {
            // Continuously track PlayerHealthManager state in real-time
            if (PlayerHealthManager.Instance != null)
            {
                targetHealth = PlayerHealthManager.Instance.CurrentHealth;
                maxHealth = Mathf.Max(1f, PlayerHealthManager.Instance.MaxHealth);
                targetFill = PlayerHealthManager.Instance.HealthNormalized;
                isLowHealth = PlayerHealthManager.Instance.IsLowHealth;
                isCriticalHealth = PlayerHealthManager.Instance.IsCriticalHealth;
                bool isIll = PlayerHealthManager.Instance.IsIll;

                bool shouldShowBadge = isLowHealth || isIll;
                if (warningBadge != null)
                {
                    if (warningBadge.activeSelf != shouldShowBadge)
                    {
                        warningBadge.SetActive(shouldShowBadge);
                    }
                    if (warningBadgeText != null && shouldShowBadge)
                    {
                        warningBadgeText.text = isIll ? "SICK!" : (isCriticalHealth ? "CRITICAL" : "HUNGRY");
                    }
                }
            }

            AnimateHealthBar();
            AnimateLowHealthPulse();
        }

        private void HandleHealthChanged(float current, float max)
        {
            targetHealth = current;
            maxHealth = Mathf.Max(1f, max);
            targetFill = Mathf.Clamp01(current / maxHealth);

            isLowHealth = (PlayerHealthManager.Instance != null) ? PlayerHealthManager.Instance.IsLowHealth : (targetFill <= 0.3f);
            isCriticalHealth = (PlayerHealthManager.Instance != null) ? PlayerHealthManager.Instance.IsCriticalHealth : (targetFill <= 0.15f);

            if (warningBadge != null)
            {
                warningBadge.SetActive(isLowHealth);
            }
        }

        private void HandleLowHealthStateChanged(bool isLow)
        {
            isLowHealth = isLow;
            if (PlayerHealthManager.Instance != null)
            {
                isCriticalHealth = PlayerHealthManager.Instance.IsCriticalHealth;
            }

            if (warningBadge != null)
            {
                warningBadge.SetActive(isLow);
            }
        }

        private void HandleWarningTriggered(string message, bool isCritical)
        {
            // Visual punch when warning triggers
            StartCoroutine(FlashHealthBarRoutine(isCritical ? 0.35f : 0.2f));
        }

        private void AnimateHealthBar()
        {
            // Smoothly interpolate fill amount
            currentDisplayFill = Mathf.Lerp(currentDisplayFill, targetFill, Time.deltaTime * smoothLerpSpeed);
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetHealth, Time.deltaTime * smoothLerpSpeed);

            // Compute dynamic color based on percentage
            Color barColor;
            if (currentDisplayFill >= 0.5f)
            {
                float t = (currentDisplayFill - 0.5f) * 2f;
                barColor = Color.Lerp(midHealthColor, fullHealthColor, t);
            }
            else
            {
                float t = currentDisplayFill * 2f;
                barColor = Color.Lerp(lowHealthColor, midHealthColor, t);
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = currentDisplayFill;
                healthFillImage.color = barColor;
            }

            if (healthText != null)
            {
                int hpDisplay = Mathf.RoundToInt(currentDisplayHealth);
                int hpMax = Mathf.RoundToInt(maxHealth);
                bool isIll = PlayerHealthManager.Instance != null && PlayerHealthManager.Instance.IsIll;
                healthText.text = isIll ? $"ILL: {hpDisplay}/{hpMax} HP" : $"{hpDisplay}/{hpMax} HP";
            }
        }

        private void AnimateLowHealthPulse()
        {
            if (heartIconImage == null) return;

            if (isLowHealth)
            {
                float pulseSpeed = isCriticalHealth ? 11f : 6f;
                float pulseMagnitude = isCriticalHealth ? 0.22f : 0.12f;
                float scaleFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
                heartIconImage.transform.localScale = originalHeartScale * scaleFactor;

                // Pulsate warning badge if present
                if (warningBadge != null && warningBadge.activeSelf)
                {
                    float badgeScale = 1f + Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)) * 0.15f;
                    warningBadge.transform.localScale = Vector3.one * badgeScale;
                }
            }
            else
            {
                // Smoothly return to original scale
                heartIconImage.transform.localScale = Vector3.Lerp(heartIconImage.transform.localScale, originalHeartScale, Time.deltaTime * 8f);
                if (warningBadge != null && warningBadge.activeSelf)
                {
                    warningBadge.SetActive(false);
                }
            }
        }

        private IEnumerator FlashHealthBarRoutine(float duration)
        {
            if (healthFillImage == null) yield break;

            Color originalColor = healthFillImage.color;
            healthFillImage.color = Color.white;
            yield return new WaitForSeconds(duration);
            healthFillImage.color = originalColor;
        }

        /// <summary>
        /// Self-healing fallback to auto-discover child components if references are unassigned.
        /// </summary>
        public void EnsureBindings()
        {
            if (healthFillImage == null)
            {
                Transform fillTr = transform.Find("HealthTrack/HealthFill") ?? transform.Find("HealthFill");
                if (fillTr != null) healthFillImage = fillTr.GetComponent<Image>();
            }

            if (healthTrackImage == null)
            {
                Transform trackTr = transform.Find("HealthTrack");
                if (trackTr != null) healthTrackImage = trackTr.GetComponent<Image>();
            }

            if (healthText == null)
            {
                Transform textTr = transform.Find("HealthTrack/HealthText") ?? transform.Find("HealthText");
                if (textTr != null) healthText = textTr.GetComponent<TextMeshProUGUI>();
            }

            if (heartIconImage == null)
            {
                Transform heartTr = transform.Find("HeartIcon");
                if (heartTr != null) heartIconImage = heartTr.GetComponent<Image>();
            }

            if (warningBadge == null)
            {
                Transform badgeTr = transform.Find("WarningBadge");
                if (badgeTr != null) warningBadge = badgeTr.gameObject;
            }

            if (warningBadge != null && warningBadgeText == null)
            {
                warningBadgeText = warningBadge.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }
}
