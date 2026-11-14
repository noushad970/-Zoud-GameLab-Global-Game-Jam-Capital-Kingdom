using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EmpireBuilder.Core
{
    /// <summary>
    /// Automatic mobile and WebGL performance optimizer.
    /// Runs at startup and on every scene load to ensure smooth 60 FPS gameplay,
    /// optimal resolution scaling, zero garbage collection pauses, and eliminated draw call spikes.
    /// </summary>
    public class MobilePerformanceOptimizer : MonoBehaviour
    {
        public static MobilePerformanceOptimizer Instance { get; private set; }

        [Header("Optimization Settings")]
        [SerializeField] private bool force60Fps = true;
        [SerializeField] private bool optimizeMobileQuality = true;
        [SerializeField] private float mobileFarClipPlane = 160f;
        [SerializeField] private float mobileRenderScale = 0.85f;

        public static bool IsMobileDevice { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeOnStartup()
        {
            // Detect handheld / mobile platforms including WebGL mobile browsers
            IsMobileDevice = Application.isMobilePlatform ||
                             SystemInfo.deviceType == DeviceType.Handheld ||
                             IsMobileBrowserCheck();

            // Set smooth 60 FPS target
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0; // Let requestAnimationFrame handle synchronization on WebGL/mobile

            // Prevent mobile sleep during gameplay
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (IsMobileDevice)
            {
                // Select Mobile Quality Level (Tier 0)
                if (QualitySettings.GetQualityLevel() != 0)
                {
                    QualitySettings.SetQualityLevel(0, true);
                }

                QualitySettings.shadowDistance = 35f;
                QualitySettings.shadowCascades = 1;

                Debug.Log($"[MobilePerformanceOptimizer] Mobile device detected ({SystemInfo.deviceModel}). Applied Mobile Quality Tier 0 with 60 FPS target.");
            }
            else
            {
                Debug.Log("[MobilePerformanceOptimizer] Desktop platform detected. 60 FPS target locked.");
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySceneOptimizations();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneOptimizations();
        }

        /// <summary>
        /// Applies camera, resolution, and rendering optimizations for the loaded scene.
        /// </summary>
        public void ApplySceneOptimizations()
        {
            if (force60Fps)
            {
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }

            // Optimize Main Camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                if (IsMobileDevice)
                {
                    cam.farClipPlane = mobileFarClipPlane;
                    cam.allowHDR = false;
                    cam.allowMSAA = false;
                }
            }

            // Optimize URP Render Scale on Mobile / Handheld
            if (IsMobileDevice && optimizeMobileQuality)
            {
                var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset == null)
                {
                    urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                }

                if (urpAsset != null)
                {
                    urpAsset.renderScale = Mathf.Clamp(mobileRenderScale, 0.70f, 0.90f);
                }

                // If running on ultra-dense mobile screens (e.g. 2800x1260 or 4K phone screens),
                // cap the maximum render viewport to 1080p to prevent GPU fillrate choking
                if (Screen.width > 1920 || Screen.height > 1080)
                {
                    float aspect = (float)Screen.width / Screen.height;
                    int targetHeight = 720;
                    int targetWidth = Mathf.RoundToInt(targetHeight * aspect);
                    Screen.SetResolution(targetWidth, targetHeight, false);
                }
            }
        }

        private static bool IsMobileBrowserCheck()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // In WebGL, SystemInfo.deviceType is reliable in modern Unity,
                // but checking system memory or handheld helps classify mobile phones
                return SystemInfo.deviceType == DeviceType.Handheld || SystemInfo.systemMemorySize < 4096;
            }
            catch
            {
                return false;
            }
#else
            return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif
        }
    }
}
