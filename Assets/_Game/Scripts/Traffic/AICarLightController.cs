using System.Collections.Generic;
using EmpireBuilder.World;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Autonomous vehicle lighting system.
    /// - Controls ultra-efficient volumetric fake light cones ("V Cone LiT") without heavy dynamic Unity spotlights.
    /// - Auto-initialization of real-time Spotlight headlights is removed to guarantee 60 FPS lag-free mobile gameplay.
    /// - Precision rear bumper alignment for taillights and dynamic brake lights (measured directly from vehicle body mesh).
    /// - Automatically syncs with the Day/Night cycle and vehicle braking status.
    /// </summary>
    [DisallowMultipleComponent]
    public class AICarLightController : MonoBehaviour
    {
        [Header("Volumetric Headlights (V Cone LiT)")]
        [Tooltip("References to V Cone LiT volumetric light objects attached to this car.")]
        [SerializeField] private List<GameObject> volumetricHeadlightCones = new List<GameObject>();

        [Tooltip("If true, disables any real-time Light component inside V Cone LiT so only the lightweight volumetric mesh renders on mobile.")]
        [SerializeField] private bool disableInternalConeSpotlights = true;

        [Header("Taillight & Brake Light Appearance")]
        [SerializeField] private Color taillightColor = new Color(1.0f, 0.12f, 0.05f);
        [SerializeField] private float taillightNightIntensity = 1.0f;
        [SerializeField] private float taillightBrakeIntensity = 4.0f;
        [SerializeField] private float taillightRange = 2.2f;

        [Tooltip("Enable dynamic point lights at rear. Kept false by default on mobile for optimal FPS.")]
        [SerializeField] private bool useDynamicPointlights = false;

        [Header("Taillight Alignment Overrides (0 = Auto-detect from mesh)")]
        [SerializeField] private float rearZOverride = 0f;
        [SerializeField] private float taillightHeightOverride = 0f;
        [SerializeField] private float taillightWidthOverride = 0f;

        [Header("State (Read-Only)")]
        [SerializeField] private bool isNight = false;
        [SerializeField] private bool isBraking = false;
        [SerializeField] private bool lightsInitialized = false;

        // Taillights & Brake Lights
        private Light taillightLeft;
        private Light taillightRight;
        private GameObject lensTaillightL;
        private GameObject lensTaillightR;

        // Shared material
        private static Material cachedTaillightMat;

        private AICarController aiCar;
        private ActiveRentedTaxi rentedTaxi;

        private void Awake()
        {
            aiCar = GetComponent<AICarController>();
            rentedTaxi = GetComponent<ActiveRentedTaxi>();
            EnsureLightsSetup();
        }

        private void Start()
        {
            UpdateNightState(true);
        }

        private void OnEnable()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnDayNightTransition += HandleDayNightTransition;
            }
            UpdateNightState(true);
        }

        private void OnDisable()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnDayNightTransition -= HandleDayNightTransition;
            }
        }

        private void HandleDayNightTransition(bool isDay)
        {
            UpdateNightState(false);
        }

        private void Update()
        {
            if (!lightsInitialized)
            {
                EnsureLightsSetup();
            }

            // Sync with DayNightCycleManager state
            bool nightNow = (DayNightCycleManager.Instance != null) ? !DayNightCycleManager.Instance.IsDaytime : false;
            if (nightNow != isNight)
            {
                isNight = nightNow;
                ApplyLightState();
            }

            // Check if braking
            bool brakingNow = (aiCar != null && aiCar.IsBraking);
            if (brakingNow != isBraking)
            {
                isBraking = brakingNow;
                ApplyBrakeLights();
            }
        }

        public void UpdateNightState(bool immediate)
        {
            if (DayNightCycleManager.Instance != null)
            {
                isNight = !DayNightCycleManager.Instance.IsDaytime;
            }
            else
            {
                isNight = false;
            }

            if (aiCar != null)
            {
                isBraking = aiCar.IsBraking;
            }

            ApplyLightState();
        }

        private void ApplyLightState()
        {
            // 1. Volumetric Headlights ("V Cone LiT") are active strictly during night
            for (int i = 0; i < volumetricHeadlightCones.Count; i++)
            {
                if (volumetricHeadlightCones[i] != null)
                {
                    volumetricHeadlightCones[i].SetActive(isNight);
                }
            }

            // 2. Taillights & brake lights
            ApplyBrakeLights();
        }

        private void ApplyBrakeLights()
        {
            // Taillights are active if it's night OR if car is braking
            bool enableTaillights = isNight || isBraking;
            float targetScale = isBraking ? 0.22f : 0.15f;

            if (lensTaillightL != null)
            {
                lensTaillightL.SetActive(enableTaillights);
                lensTaillightL.transform.localScale = Vector3.one * targetScale;
            }

            if (lensTaillightR != null)
            {
                lensTaillightR.SetActive(enableTaillights);
                lensTaillightR.transform.localScale = Vector3.one * targetScale;
            }

            if (useDynamicPointlights)
            {
                float targetIntensity = isBraking ? taillightBrakeIntensity : (isNight ? taillightNightIntensity : 0f);

                if (taillightLeft != null)
                {
                    taillightLeft.enabled = enableTaillights;
                    taillightLeft.intensity = targetIntensity;
                }

                if (taillightRight != null)
                {
                    taillightRight.enabled = enableTaillights;
                    taillightRight.intensity = targetIntensity;
                }
            }
            else
            {
                if (taillightLeft != null && taillightLeft.enabled) taillightLeft.enabled = false;
                if (taillightRight != null && taillightRight.enabled) taillightRight.enabled = false;
            }
        }

        /// <summary>
        /// Discovers volumetric "V Cone LiT" headlights, eliminates any legacy real-time spotlights,
        /// and positions brake lights flush on the vehicle rear bumper.
        /// </summary>
        public void EnsureLightsSetup()
        {
            CleanUpLegacyHeadlights();
            DiscoverVolumetricCones();
            SetupBrakeAndTaillights();

            lightsInitialized = true;
            ApplyLightState();
        }

        /// <summary>
        /// Removes or disables any legacy auto-generated real-time Spotlight headlights.
        /// Prevents dynamic lighting calculation overhead on mobile and WebGL.
        /// </summary>
        private void CleanUpLegacyHeadlights()
        {
            string[] legacyNames = new string[]
            {
                "Car_Headlight_L",
                "Car_Headlight_R",
                "Lens_HL_L",
                "Lens_HL_R"
            };

            for (int i = 0; i < legacyNames.Length; i++)
            {
                Transform oldObj = transform.Find(legacyNames[i]);
                if (oldObj != null)
                {
                    if (Application.isPlaying) Destroy(oldObj.gameObject);
                    else DestroyImmediate(oldObj.gameObject);
                }
            }
        }

        /// <summary>
        /// Finds all "V Cone LiT" volumetric cone meshes on the vehicle.
        /// Disables internal per-pixel dynamic lights inside the cone for 60 FPS mobile performance.
        /// </summary>
        private void DiscoverVolumetricCones()
        {
            volumetricHeadlightCones.Clear();

            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child == transform) continue;
                string lower = child.name.ToLower();

                if (lower.Contains("v cone") || lower.Contains("cone lit") || lower.Contains("volumetric cone") || lower.Contains("light cone"))
                {
                    if (!volumetricHeadlightCones.Contains(child.gameObject))
                    {
                        volumetricHeadlightCones.Add(child.gameObject);

                        if (disableInternalConeSpotlights)
                        {
                            Light[] internalLights = child.GetComponentsInChildren<Light>(true);
                            for (int l = 0; l < internalLights.Length; l++)
                            {
                                internalLights[l].enabled = false;
                            }
                        }
                    }
                }
            }

            // Sync initial visibility
            for (int i = 0; i < volumetricHeadlightCones.Count; i++)
            {
                if (volumetricHeadlightCones[i] != null)
                {
                    volumetricHeadlightCones[i].SetActive(isNight);
                }
            }
        }

        /// <summary>
        /// Measures the car's physical body mesh bounds to position brake lights
        /// exactly on the rear bumper housing instead of floating in the air.
        /// </summary>
        private void SetupBrakeAndTaillights()
        {
            // Default realistic dimensions for city vehicles
            float rearZ = -1.65f;
            float halfWidth = 0.52f;
            float heightY = 0.44f;

            // 1. Discover rear wheels if present
            Transform rearWheel = null;
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform ch in allChildren)
            {
                string n = ch.name.ToLower();
                if (n.Contains("wheel") && n.Contains("rear"))
                {
                    rearWheel = ch;
                    break;
                }
            }

            // 2. Measure actual body mesh bounds in local car space (ignoring wheels, cones, lights)
            Bounds bodyBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBodyBounds = false;
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);

            for (int i = 0; i < meshFilters.Length; i++)
            {
                if (meshFilters[i].sharedMesh == null) continue;
                string mfName = meshFilters[i].name.ToLower();
                if (mfName.Contains("wheel") || mfName.Contains("cone") || mfName.Contains("lens") || mfName.Contains("bulb") || mfName.Contains("light")) continue;

                Bounds mb = meshFilters[i].sharedMesh.bounds;
                Transform mfTr = meshFilters[i].transform;

                Vector3[] corners = new Vector3[8]
                {
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.min.x, mb.min.y, mb.min.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.max.x, mb.min.y, mb.min.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.min.x, mb.max.y, mb.min.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.max.x, mb.max.y, mb.min.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.min.x, mb.min.y, mb.max.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.max.x, mb.min.y, mb.max.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.min.x, mb.max.y, mb.max.z))),
                    transform.InverseTransformPoint(mfTr.TransformPoint(new Vector3(mb.max.x, mb.max.y, mb.max.z)))
                };

                for (int c = 0; c < 8; c++)
                {
                    if (!hasBodyBounds)
                    {
                        bodyBounds = new Bounds(corners[c], Vector3.zero);
                        hasBodyBounds = true;
                    }
                    else
                    {
                        bodyBounds.Encapsulate(corners[c]);
                    }
                }
            }

            if (hasBodyBounds)
            {
                // Align flush right on the rear surface of the car
                rearZ = bodyBounds.min.z + 0.02f;
                halfWidth = Mathf.Clamp(bodyBounds.extents.x * 0.68f, 0.40f, 0.72f);
                heightY = Mathf.Clamp(bodyBounds.min.y + bodyBounds.size.y * 0.38f, 0.38f, 0.58f);
            }
            else if (rearWheel != null)
            {
                rearZ = rearWheel.localPosition.z - 0.54f;
                halfWidth = Mathf.Abs(rearWheel.localPosition.x) * 0.65f;
                heightY = rearWheel.localPosition.y + 0.08f;
            }

            // Apply manual overrides if set in Inspector
            if (rearZOverride != 0f) rearZ = rearZOverride;
            if (taillightHeightOverride != 0f) heightY = taillightHeightOverride;
            if (taillightWidthOverride != 0f) halfWidth = taillightWidthOverride;

            Material tailMat = GetOrCreateTaillightMaterial();

            // Setup Left Taillight
            Vector3 posL = new Vector3(-halfWidth, heightY, rearZ);
            Transform tlTr = transform.Find("Car_Taillight_L");
            if (tlTr == null)
            {
                GameObject goL = new GameObject("Car_Taillight_L");
                goL.transform.SetParent(transform, false);
                tlTr = goL.transform;
            }
            tlTr.localPosition = posL;

            if (useDynamicPointlights)
            {
                taillightLeft = tlTr.GetComponent<Light>();
                if (taillightLeft == null) taillightLeft = tlTr.gameObject.AddComponent<Light>();
                taillightLeft.type = LightType.Point;
                taillightLeft.color = taillightColor;
                taillightLeft.range = taillightRange;
                taillightLeft.shadows = LightShadows.None;
                taillightLeft.renderMode = LightRenderMode.Auto;
                taillightLeft.enabled = false;
            }
            lensTaillightL = CreateGlowLens("Lens_TL_L", tlTr, Vector3.zero, 0.16f, tailMat);

            // Setup Right Taillight
            Vector3 posR = new Vector3(halfWidth, heightY, rearZ);
            Transform trTr = transform.Find("Car_Taillight_R");
            if (trTr == null)
            {
                GameObject goR = new GameObject("Car_Taillight_R");
                goR.transform.SetParent(transform, false);
                trTr = goR.transform;
            }
            trTr.localPosition = posR;

            if (useDynamicPointlights)
            {
                taillightRight = trTr.GetComponent<Light>();
                if (taillightRight == null) taillightRight = trTr.gameObject.AddComponent<Light>();
                taillightRight.type = LightType.Point;
                taillightRight.color = taillightColor;
                taillightRight.range = taillightRange;
                taillightRight.shadows = LightShadows.None;
                taillightRight.renderMode = LightRenderMode.Auto;
                taillightRight.enabled = false;
            }
            lensTaillightR = CreateGlowLens("Lens_TL_R", trTr, Vector3.zero, 0.16f, tailMat);
        }

        private GameObject CreateGlowLens(string name, Transform parent, Vector3 localPos, float scale, Material mat)
        {
            if (parent == null) parent = transform;
            Transform existing = parent.Find(name);
            GameObject lens = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.name = name;
            lens.transform.SetParent(parent, false);
            lens.transform.localPosition = localPos;
            lens.transform.localScale = Vector3.one * scale;

            Collider c = lens.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            MeshRenderer mr = lens.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            lens.SetActive(false);
            return lens;
        }

        public static Material GetOrCreateTaillightMaterial()
        {
            if (cachedTaillightMat != null) return cachedTaillightMat;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            cachedTaillightMat = new Material(s);
            cachedTaillightMat.name = "Dynamic_Car_Taillight_Glow";
            cachedTaillightMat.EnableKeyword("_EMISSION");
            cachedTaillightMat.SetColor("_BaseColor", new Color(1.0f, 0.15f, 0.05f, 1f));
            cachedTaillightMat.SetColor("_EmissionColor", new Color(3.2f, 0.25f, 0.10f, 1f));
            cachedTaillightMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return cachedTaillightMat;
        }
    }
}
