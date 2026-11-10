using System;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Static scene manager that smoothly controls the directional light and ambient lighting
    /// to simulate a dynamic 24-hour day/night cycle.
    /// Can be fully customized directly from the Inspector hierarchy.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; private set; }

        [Header("Lighting References")]
        [Tooltip("The primary directional light representing the sun/moon")]
        [SerializeField] private Light directionalLight;

        [Header("Cycle Configuration")]
        [Tooltip("Real-time seconds required for one full 24-hour day/night cycle")]
        [Min(10f)]
        [SerializeField] private float fullDayDurationSeconds = 120f;

        [Tooltip("Current time of day normalized between 0.0 and 1.0 (0.0 = 06:00 Sunrise, 0.25 = 12:00 Noon, 0.5 = 18:00 Sunset, 0.75 = 00:00 Midnight)")]
        [Range(0f, 1f)]
        [SerializeField] private float currentTimeOfDay = 0.20f;

        [Tooltip("Whether the cycle advances automatically in play mode")]
        [SerializeField] private bool autoAdvance = true;

        [Tooltip("Time multiplier speed (1.0 = normal)")]
        [Range(0.1f, 20f)]
        [SerializeField] private float timeScale = 1.0f;

        [Header("Sun & Moon Appearance")]
        [Tooltip("Light intensity multiplier throughout the 24h cycle")]
        [SerializeField] private AnimationCurve lightIntensityCurve;

        [Tooltip("Sunlight color gradient throughout the day")]
        [SerializeField] private Gradient sunColorGradient;

        [Tooltip("Ambient environment color gradient throughout the day")]
        [SerializeField] private Gradient ambientColorGradient;

        [Header("Sun Orbit Orientation")]
        [Tooltip("Sun orbit rotation pitch/yaw axis")]
        [SerializeField] private Vector3 orbitAxis = new Vector3(1f, 0.35f, 0f);

        [Tooltip("Sun shadow strength curve")]
        [SerializeField] private AnimationCurve shadowStrengthCurve;

        // Events
        public event Action<float> OnTimeChanged;
        public event Action<bool> OnDayNightTransition; // true = daytime, false = nighttime

        private bool wasDaytime = true;

        public float CurrentTimeOfDay => currentTimeOfDay;
        public bool IsDaytime => currentTimeOfDay >= 0.05f && currentTimeOfDay <= 0.55f;

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

            if (directionalLight == null)
            {
                directionalLight = GetComponent<Light>();
                if (directionalLight == null)
                {
                    directionalLight = RenderSettings.sun;
                }
                if (directionalLight == null)
                {
                    var allLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (var l in allLights)
                    {
                        if (l != null && l.type == LightType.Directional)
                        {
                            directionalLight = l;
                            break;
                        }
                    }
                }
            }

            InitializeDefaultCurves();
            ApplyLighting(currentTimeOfDay);
        }

        private void Start()
        {
            ApplyLighting(currentTimeOfDay);
        }

        private void Update()
        {
            if (Application.isPlaying && autoAdvance)
            {
                float delta = (Time.deltaTime / Mathf.Max(fullDayDurationSeconds, 1f)) * timeScale;
                currentTimeOfDay = (currentTimeOfDay + delta) % 1f;
            }

            ApplyLighting(currentTimeOfDay);

            bool isDay = IsDaytime;
            if (isDay != wasDaytime)
            {
                wasDaytime = isDay;
                OnDayNightTransition?.Invoke(isDay);
            }

            OnTimeChanged?.Invoke(currentTimeOfDay);
        }

        /// <summary>
        /// Updates sun rotation, intensity, and colors based on timeOfDay (0.0 to 1.0).
        /// </summary>
        public void ApplyLighting(float time)
        {
            if (directionalLight == null) return;

            // 1. Rotation: Map 0.0..1.0 to 360 degrees
            // 0.0 (06:00) = Horizon East (0 deg X)
            // 0.25 (12:00) = Zenith (90 deg X)
            // 0.5 (18:00) = Horizon West (180 deg X)
            // 0.75 (00:00) = Nadir / Midnight (270 deg X)
            float sunAngle = (time * 360f) - 90f;
            directionalLight.transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

            // 2. Light Color & Intensity
            if (sunColorGradient != null)
            {
                directionalLight.color = sunColorGradient.Evaluate(time);
            }

            if (lightIntensityCurve != null)
            {
                directionalLight.intensity = lightIntensityCurve.Evaluate(time);
            }
            else
            {
                // Fallback smooth intensity
                float dayFactor = Mathf.Clamp01(Mathf.Sin(time * Mathf.PI * 2f - Mathf.PI * 0.5f));
                directionalLight.intensity = Mathf.Lerp(0.18f, 1.25f, dayFactor);
            }

            // 3. Shadow Strength
            if (shadowStrengthCurve != null)
            {
                directionalLight.shadowStrength = shadowStrengthCurve.Evaluate(time);
            }
            else
            {
                directionalLight.shadowStrength = IsDaytime ? 0.85f : 0.45f;
            }

            // 4. Ambient Sky Light
            if (ambientColorGradient != null)
            {
                RenderSettings.ambientLight = ambientColorGradient.Evaluate(time);
            }
        }

        public void SetTimeOfDay(float normalizedTime)
        {
            currentTimeOfDay = Mathf.Clamp01(normalizedTime);
            ApplyLighting(currentTimeOfDay);
        }

        public string GetFormattedTime()
        {
            float totalHours = currentTimeOfDay * 24f + 6f; // offset so 0.0 is 6:00 AM
            if (totalHours >= 24f) totalHours -= 24f;

            int hours = Mathf.FloorToInt(totalHours);
            int minutes = Mathf.FloorToInt((totalHours - hours) * 60f);
            string period = hours >= 12 ? "PM" : "AM";
            int displayHours = hours % 12;
            if (displayHours == 0) displayHours = 12;

            return $"{displayHours:D2}:{minutes:D2} {period}";
        }

        private void InitializeDefaultCurves()
        {
            if (lightIntensityCurve == null || lightIntensityCurve.length == 0)
            {
                lightIntensityCurve = new AnimationCurve();
                lightIntensityCurve.AddKey(new Keyframe(0.00f, 0.40f)); // 06:00 Dawn
                lightIntensityCurve.AddKey(new Keyframe(0.15f, 1.10f)); // 09:30 Morning
                lightIntensityCurve.AddKey(new Keyframe(0.25f, 1.30f)); // 12:00 Noon Peak
                lightIntensityCurve.AddKey(new Keyframe(0.40f, 1.15f)); // 15:30 Afternoon
                lightIntensityCurve.AddKey(new Keyframe(0.50f, 0.50f)); // 18:00 Sunset
                lightIntensityCurve.AddKey(new Keyframe(0.58f, 0.20f)); // 20:00 Dusk
                lightIntensityCurve.AddKey(new Keyframe(0.75f, 0.15f)); // 00:00 Night / Moonlight
                lightIntensityCurve.AddKey(new Keyframe(0.92f, 0.18f)); // 04:00 Pre-dawn
                lightIntensityCurve.AddKey(new Keyframe(1.00f, 0.40f)); // Cycle loop
            }

            if (shadowStrengthCurve == null || shadowStrengthCurve.length == 0)
            {
                shadowStrengthCurve = new AnimationCurve();
                shadowStrengthCurve.AddKey(new Keyframe(0.00f, 0.50f));
                shadowStrengthCurve.AddKey(new Keyframe(0.25f, 0.85f));
                shadowStrengthCurve.AddKey(new Keyframe(0.50f, 0.50f));
                shadowStrengthCurve.AddKey(new Keyframe(0.75f, 0.35f));
                shadowStrengthCurve.AddKey(new Keyframe(1.00f, 0.50f));
            }

            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            {
                sunColorGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1.0f, 0.65f, 0.35f), 0.00f), // Warm sunrise
                    new GradientColorKey(new Color(1.0f, 0.95f, 0.85f), 0.15f), // Morning gold
                    new GradientColorKey(new Color(1.0f, 1.0f, 0.98f),  0.25f), // High noon white
                    new GradientColorKey(new Color(1.0f, 0.92f, 0.80f), 0.40f), // Afternoon
                    new GradientColorKey(new Color(1.0f, 0.45f, 0.20f), 0.50f), // Golden sunset
                    new GradientColorKey(new Color(0.45f, 0.35f, 0.60f), 0.58f), // Purple dusk
                    new GradientColorKey(new Color(0.22f, 0.32f, 0.55f), 0.75f), // Cool blue moonlight
                    new GradientColorKey(new Color(0.25f, 0.30f, 0.50f), 0.90f), // Pre-dawn deep blue
                    new GradientColorKey(new Color(1.0f, 0.65f, 0.35f), 1.00f)  // Back to sunrise
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                };
                sunColorGradient.SetKeys(colorKeys, alphaKeys);
            }

            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.45f, 0.40f, 0.38f), 0.00f), // Sunrise ambient
                    new GradientColorKey(new Color(0.65f, 0.68f, 0.70f), 0.25f), // Day ambient
                    new GradientColorKey(new Color(0.50f, 0.35f, 0.30f), 0.50f), // Sunset ambient
                    new GradientColorKey(new Color(0.12f, 0.16f, 0.25f), 0.75f), // Night ambient
                    new GradientColorKey(new Color(0.45f, 0.40f, 0.38f), 1.00f)
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                };
                ambientColorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }
    }
}
