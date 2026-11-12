using System.Collections.Generic;
using EmpireBuilder.World;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Autonomous vehicle lighting system.
    /// Manages dual forward headlights (shining on the road), dual rear taillights,
    /// glowing emissive lamp lenses, and dynamic brake light flares.
    /// Automatically turns on at night and syncs with the Day/Night cycle.
    /// </summary>
    [DisallowMultipleComponent]
    public class AICarLightController : MonoBehaviour
    {
        [Header("Headlight Configuration")]
        [SerializeField] private Color headlightColor = new Color(1.0f, 0.98f, 0.92f);
        [SerializeField] private float headlightIntensity = 28.0f;
        [SerializeField] private float headlightRange = 19.0f;
        [SerializeField] private float headlightSpotAngle = 58.0f;
        [SerializeField] private float headlightInnerAngle = 38.0f;

        [Header("Taillight Configuration")]
        [SerializeField] private Color taillightColor = new Color(1.0f, 0.12f, 0.05f);
        [SerializeField] private float taillightNightIntensity = 3.5f;
        [SerializeField] private float taillightBrakeIntensity = 14.0f;
        [SerializeField] private float taillightRange = 4.8f;

        [Header("State (Read-Only)")]
        [SerializeField] private bool isNight = false;
        [SerializeField] private bool isBraking = false;
        [SerializeField] private bool lightsInitialized = false;

        // References to light components
        private Light headlightLeft;
        private Light headlightRight;
        private Light taillightLeft;
        private Light taillightRight;

        // Glowing visual bulb/lens meshes
        private GameObject lensHeadlightL;
        private GameObject lensHeadlightR;
        private GameObject lensTaillightL;
        private GameObject lensTaillightR;

        // Shared materials
        private static Material cachedHeadlightMat;
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
            // Headlights are active during night
            bool enableHeadlights = isNight;
            if (headlightLeft != null) headlightLeft.enabled = enableHeadlights;
            if (headlightRight != null) headlightRight.enabled = enableHeadlights;
            if (lensHeadlightL != null) lensHeadlightL.SetActive(enableHeadlights);
            if (lensHeadlightR != null) lensHeadlightR.SetActive(enableHeadlights);

            ApplyBrakeLights();
        }

        private void ApplyBrakeLights()
        {
            // Taillights are active if it's night OR if car is braking
            bool enableTaillights = isNight || isBraking;
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

            if (lensTaillightL != null)
            {
                lensTaillightL.SetActive(enableTaillights);
                lensTaillightL.transform.localScale = Vector3.one * (isBraking ? 0.22f : 0.16f);
            }

            if (lensTaillightR != null)
            {
                lensTaillightR.SetActive(enableTaillights);
                lensTaillightR.transform.localScale = Vector3.one * (isBraking ? 0.22f : 0.16f);
            }
        }

        /// <summary>
        /// Ensures all headlights, taillights, and emissive lens meshes are created and positioned.
        /// </summary>
        public void EnsureLightsSetup()
        {
            if (lightsInitialized && headlightLeft != null && headlightRight != null) return;

            // Determine car bounding box dimensions
            BoxCollider col = GetComponent<BoxCollider>();
            float frontZ = 1.8f;
            float rearZ = -1.8f;
            float halfWidth = 0.65f;
            float heightY = 0.58f;

            if (col != null)
            {
                frontZ = col.center.z + col.size.z * 0.5f;
                rearZ = col.center.z - col.size.z * 0.5f;
                halfWidth = Mathf.Clamp(col.size.x * 0.38f, 0.45f, 0.85f);
                heightY = Mathf.Clamp(col.center.y * 0.82f, 0.45f, 0.80f);
            }
            else
            {
                // Fallback estimate from renderers
                Renderer[] rends = GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    Bounds b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    frontZ = b.extents.z * 0.95f;
                    rearZ = -b.extents.z * 0.95f;
                    halfWidth = Mathf.Clamp(b.extents.x * 0.75f, 0.45f, 0.85f);
                    heightY = Mathf.Clamp(b.extents.y * 0.70f, 0.45f, 0.80f);
                }
            }

            // Cleanup or disable any misplaced existing spot lights that were inside the car body
            Light[] existingLights = GetComponentsInChildren<Light>(true);
            foreach (var el in existingLights)
            {
                if (el.gameObject.name.Contains("Spot Light"))
                {
                    // Repurpose or disable legacy misplaced lights
                    el.enabled = false;
                }
            }

            Material headMat = GetOrCreateHeadlightMaterial();
            Material tailMat = GetOrCreateTaillightMaterial();

            // 1. Setup Headlights
            Transform hlTr = transform.Find("Car_Headlight_L");
            if (hlTr == null)
            {
                headlightLeft = CreateSpotlight("Car_Headlight_L", new Vector3(-halfWidth, heightY, frontZ + 0.05f), Quaternion.Euler(8f, -1.5f, 0f), headlightColor, headlightIntensity, headlightRange, headlightSpotAngle, headlightInnerAngle);
                lensHeadlightL = CreateGlowLens("Lens_HL_L", headlightLeft.transform, Vector3.zero, 0.18f, headMat);
            }
            else
            {
                headlightLeft = hlTr.GetComponent<Light>();
                var lens = hlTr.Find("Lens_HL_L");
                if (lens != null) lensHeadlightL = lens.gameObject;
            }

            Transform hrTr = transform.Find("Car_Headlight_R");
            if (hrTr == null)
            {
                headlightRight = CreateSpotlight("Car_Headlight_R", new Vector3(halfWidth, heightY, frontZ + 0.05f), Quaternion.Euler(8f, 1.5f, 0f), headlightColor, headlightIntensity, headlightRange, headlightSpotAngle, headlightInnerAngle);
                lensHeadlightR = CreateGlowLens("Lens_HL_R", headlightRight.transform, Vector3.zero, 0.18f, headMat);
            }
            else
            {
                headlightRight = hrTr.GetComponent<Light>();
                var lens = hrTr.Find("Lens_HL_R");
                if (lens != null) lensHeadlightR = lens.gameObject;
            }

            // 2. Setup Taillights
            Transform tlTr = transform.Find("Car_Taillight_L");
            if (tlTr == null)
            {
                taillightLeft = CreatePointlight("Car_Taillight_L", new Vector3(-halfWidth, heightY, rearZ - 0.05f), taillightColor, taillightNightIntensity, taillightRange);
                lensTaillightL = CreateGlowLens("Lens_TL_L", taillightLeft.transform, Vector3.zero, 0.16f, tailMat);
            }
            else
            {
                taillightLeft = tlTr.GetComponent<Light>();
                var lens = tlTr.Find("Lens_TL_L");
                if (lens != null) lensTaillightL = lens.gameObject;
            }

            Transform trTr = transform.Find("Car_Taillight_R");
            if (trTr == null)
            {
                taillightRight = CreatePointlight("Car_Taillight_R", new Vector3(halfWidth, heightY, rearZ - 0.05f), taillightColor, taillightNightIntensity, taillightRange);
                lensTaillightR = CreateGlowLens("Lens_TL_R", taillightRight.transform, Vector3.zero, 0.16f, tailMat);
            }
            else
            {
                taillightRight = trTr.GetComponent<Light>();
                var lens = trTr.Find("Lens_TL_R");
                if (lens != null) lensTaillightR = lens.gameObject;
            }

            lightsInitialized = true;
            ApplyLightState();
        }

        private Light CreateSpotlight(string name, Vector3 localPos, Quaternion localRot, Color col, float intensity, float range, float spotAngle, float innerAngle)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = localRot;

            Light l = obj.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spotAngle;
            l.innerSpotAngle = innerAngle;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private Light CreatePointlight(string name, Vector3 localPos, Color col, float intensity, float range)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.identity;

            Light l = obj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            l.enabled = false;
            return l;
        }

        private GameObject CreateGlowLens(string name, Transform parent, Vector3 localPos, float scale, Material mat)
        {
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lens.name = name;
            lens.transform.SetParent(parent, false);
            lens.transform.localPosition = localPos;
            lens.transform.localScale = Vector3.one * scale;

            Collider c = lens.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

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

        public static Material GetOrCreateHeadlightMaterial()
        {
            if (cachedHeadlightMat != null) return cachedHeadlightMat;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            cachedHeadlightMat = new Material(s);
            cachedHeadlightMat.name = "Dynamic_Car_Headlight_Glow";
            cachedHeadlightMat.EnableKeyword("_EMISSION");
            cachedHeadlightMat.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.92f, 1f));
            cachedHeadlightMat.SetColor("_EmissionColor", new Color(3.2f, 3.1f, 2.7f, 1f));
            return cachedHeadlightMat;
        }

        public static Material GetOrCreateTaillightMaterial()
        {
            if (cachedTaillightMat != null) return cachedTaillightMat;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            cachedTaillightMat = new Material(s);
            cachedTaillightMat.name = "Dynamic_Car_Taillight_Glow";
            cachedTaillightMat.EnableKeyword("_EMISSION");
            cachedTaillightMat.SetColor("_BaseColor", new Color(1.0f, 0.15f, 0.05f, 1f));
            cachedTaillightMat.SetColor("_EmissionColor", new Color(2.8f, 0.20f, 0.08f, 1f));
            return cachedTaillightMat;
        }
    }
}

