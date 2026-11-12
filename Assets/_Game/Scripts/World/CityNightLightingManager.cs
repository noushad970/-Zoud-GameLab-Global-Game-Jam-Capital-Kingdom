using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Central manager for all street lighting, lamp posts, and urban nighttime illumination.
    /// Features:
    /// - Automatically discovers and manages all streetlights across the city (Spotlight_01, Spotlight_02, StreetLamp1, etc.).
    /// - Adds downward streetlights illuminating roads and pavements.
    /// - Adds radiant HDR emissive glowing bulb meshes at fixture heads so lights visibly glow from high camera angles.
    /// - Synchronizes smoothly with the dynamic 24h Day/Night cycle (DayNightCycleManager).
    /// - WebGL optimized with shared materials, zero colliders, and batchable meshes.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CityNightLightingManager : MonoBehaviour
    {
        public static CityNightLightingManager Instance { get; private set; }

        [System.Serializable]
        public class StreetLightFixture
        {
            public GameObject rootObject;
            public Light fixtureLight;
            public GameObject glowingBulb;
        }

        [Header("Lighting Settings")]
        [Tooltip("Warm incandescent color for street lamps")]
        [SerializeField] private Color streetlightColor = new Color(1.0f, 0.94f, 0.80f);

        [Tooltip("Light intensity for downward road illumination")]
        [SerializeField] private float streetlightIntensity = 32.0f;

        [Tooltip("Range of street light illumination")]
        [SerializeField] private float streetlightRange = 18.0f;

        [Tooltip("Spot angle in degrees for downward streetlights")]
        [SerializeField] private float spotAngle = 95.0f;

        [Header("Glowing Bulb Settings")]
        [Tooltip("HDR Emission color for lamp head glowing lens/bulb")]
        [SerializeField] private Color bulbEmissionColor = new Color(3.8f, 3.5f, 2.4f, 1.0f);

        [Header("State (Read Only)")]
        [SerializeField] private bool isNightActive = false;
        [SerializeField] private int registeredFixtureCount = 0;

        private List<StreetLightFixture> registeredFixtures = new List<StreetLightFixture>();
        private static Material sharedBulbMaterial;
        private bool hasInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            Instance = this;

            InitializeStreetlights();
        }

        private void OnEnable()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnDayNightTransition += HandleDayNightTransition;
            }

            SyncNightState(true);
        }

        private void OnDisable()
        {
            if (DayNightCycleManager.Instance != null)
            {
                DayNightCycleManager.Instance.OnDayNightTransition -= HandleDayNightTransition;
            }
        }

        private void Start()
        {
            if (!hasInitialized || registeredFixtures.Count == 0)
            {
                InitializeStreetlights();
            }

            SyncNightState(true);
        }

        private void Update()
        {
            // Sync with DayNightCycleManager state
            bool nightNow = false;
            if (DayNightCycleManager.Instance != null)
            {
                nightNow = !DayNightCycleManager.Instance.IsDaytime;
            }

            if (nightNow != isNightActive)
            {
                SetNightLighting(nightNow);
            }
        }

        private void HandleDayNightTransition(bool isDay)
        {
            SetNightLighting(!isDay);
        }

        public void SyncNightState(bool force)
        {
            bool nightNow = false;
            if (DayNightCycleManager.Instance != null)
            {
                nightNow = !DayNightCycleManager.Instance.IsDaytime;
            }

            if (force || nightNow != isNightActive)
            {
                SetNightLighting(nightNow);
            }
        }

        /// <summary>
        /// Discovers all street lamps in the scene, ensures each has an active downward light and glowing bulb.
        /// </summary>
        [ContextMenu("Refresh & Initialize Streetlights")]
        public void InitializeStreetlights()
        {
            registeredFixtures.Clear();

            // Find all potential streetlight GameObjects in the scene
            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Material bulbMat = GetOrCreateBulbMaterial();

            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                string objName = t.name;

                // Match streetlight objects
                bool isStreetlight01 = objName.IndexOf("Spotlight_01", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool isStreetlight02 = objName.IndexOf("Spotlight_02", System.StringComparison.OrdinalIgnoreCase) >= 0;
                bool isStreetLamp = objName.IndexOf("street lamp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    objName.IndexOf("StreetLamp", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isStreetlight01 && !isStreetlight02 && !isStreetLamp)
                {
                    continue;
                }

                // Exclude prefabs outside the scene or vehicles
                if (t.GetComponentInParent<Traffic.AICarController>() != null) continue;
                if (t.GetComponentInParent<Traffic.ActiveRentedTaxi>() != null) continue;

                // Process fixture
                var fixture = SetupFixture(t.gameObject, isStreetlight01, isStreetlight02, isStreetLamp, bulbMat);
                if (fixture != null)
                {
                    registeredFixtures.Add(fixture);
                }
            }

            registeredFixtureCount = registeredFixtures.Count;
            hasInitialized = true;
            Debug.Log($"[CityNightLightingManager] Configured {registeredFixtureCount} streetlights with downward lighting and HDR emissive glow bulbs.");

            // Apply current cycle state
            SyncNightState(true);
        }

        private StreetLightFixture SetupFixture(GameObject obj, bool isSL1, bool isSL2, bool isLamp, Material bulbMat)
        {
            StreetLightFixture fixture = new StreetLightFixture();
            fixture.rootObject = obj;

            Transform tr = obj.transform;

            // 1. Locate or create Light component
            Light lightComp = obj.GetComponentInChildren<Light>(true);

            Vector3 bulbLocalPos = Vector3.up * 4.5f;
            float bulbScale = 0.45f;

            if (isSL1)
            {
                bulbLocalPos = new Vector3(0f, 8.13f, 2.56f);
                bulbScale = 0.55f;

                if (lightComp != null)
                {
                    // Ensure spotlight properties
                    lightComp.type = LightType.Spot;
                    lightComp.color = streetlightColor;
                    lightComp.intensity = streetlightIntensity;
                    lightComp.range = streetlightRange;
                    lightComp.spotAngle = spotAngle;
                    lightComp.innerSpotAngle = spotAngle * 0.70f;
                    lightComp.shadows = LightShadows.None;
                }
            }
            else if (isSL2)
            {
                bulbLocalPos = new Vector3(0.16f, 6.51f, 0f);
                bulbScale = 0.50f;

                if (lightComp != null)
                {
                    lightComp.type = LightType.Point;
                    lightComp.color = streetlightColor;
                    lightComp.intensity = streetlightIntensity * 0.75f;
                    lightComp.range = streetlightRange * 0.85f;
                    lightComp.shadows = LightShadows.None;
                }
            }
            else // Graveyard/Colonial street lamp
            {
                bulbLocalPos = new Vector3(0f, 0.35f, 0f);
                bulbScale = 0.22f;

                // If no light exists, add a downward spot light
                if (lightComp == null)
                {
                    GameObject lightGo = new GameObject("StreetLamp_Light");
                    lightGo.transform.SetParent(tr, false);
                    lightGo.transform.localPosition = bulbLocalPos;
                    lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    lightComp = lightGo.AddComponent<Light>();
                    lightComp.type = LightType.Spot;
                    lightComp.color = streetlightColor;
                    lightComp.intensity = streetlightIntensity * 0.8f;
                    lightComp.range = streetlightRange * 0.75f;
                    lightComp.spotAngle = spotAngle;
                    lightComp.innerSpotAngle = spotAngle * 0.65f;
                    lightComp.shadows = LightShadows.None;
                }
            }

            fixture.fixtureLight = lightComp;

            // 2. Setup Glowing Bulb Lens Mesh
            Transform bulbTr = tr.Find("NightGlowBulb");
            GameObject bulbGo = null;

            if (bulbTr == null)
            {
                bulbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulbGo.name = "NightGlowBulb";
                bulbGo.transform.SetParent(tr, false);
                bulbGo.transform.localPosition = bulbLocalPos;
                bulbGo.transform.localScale = Vector3.one * bulbScale;

                // Remove collider to eliminate physics overhead
                Collider col = bulbGo.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }

                MeshRenderer mr = bulbGo.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = bulbMat;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
            }
            else
            {
                bulbGo = bulbTr.gameObject;
                MeshRenderer mr = bulbGo.GetComponent<MeshRenderer>();
                if (mr != null && bulbMat != null)
                {
                    mr.sharedMaterial = bulbMat;
                }
            }

            fixture.glowingBulb = bulbGo;
            return fixture;
        }

        /// <summary>
        /// Controls illumination and glow visibility for all streetlights.
        /// </summary>
        [ContextMenu("Turn On Night Lights")]
        public void TurnOnNightLights() => SetNightLighting(true);

        [ContextMenu("Turn Off Night Lights")]
        public void TurnOffNightLights() => SetNightLighting(false);

        public void SetNightLighting(bool night)
        {
            isNightActive = night;

            for (int i = 0; i < registeredFixtures.Count; i++)
            {
                var f = registeredFixtures[i];
                if (f == null || f.rootObject == null) continue;

                if (f.fixtureLight != null)
                {
                    f.fixtureLight.enabled = night;
                }

                if (f.glowingBulb != null)
                {
                    f.glowingBulb.SetActive(night);
                }
            }
        }

        public static Material GetOrCreateBulbMaterial()
        {
            if (sharedBulbMaterial != null) return sharedBulbMaterial;

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            sharedBulbMaterial = new Material(s);
            sharedBulbMaterial.name = "StreetLight_Glowing_Bulb_Mat";
            sharedBulbMaterial.EnableKeyword("_EMISSION");
            sharedBulbMaterial.SetColor("_BaseColor", new Color(1.0f, 0.98f, 0.88f, 1.0f));
            sharedBulbMaterial.SetColor("_EmissionColor", new Color(3.8f, 3.5f, 2.4f, 1.0f));
            return sharedBulbMaterial;
        }
    }
}

