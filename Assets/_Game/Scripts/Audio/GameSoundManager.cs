using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EmpireBuilder.World;

namespace EmpireBuilder.Audio
{
    /// <summary>
    /// Master 3D Sound System for Capital Kingdom.
    /// Manages:
    /// - 3D Environmental & District Ambience (Day, Night City, City Traffic, Industry, Stadium).
    /// - Dynamic Day/Night Ambience crossfades synced with DayNightCycleManager.
    /// - 3D Positional Sound Effects (Buy, Collect, Vehicle, Horn, Taxi Dispatch, Stadium Cheer, Dining, Alerts).
    /// - 3D Emitter positioning at real city landmarks (Factories, Stadium, Road Network).
    /// - Empty serialized AudioClip variables for easy drag-and-drop assignment in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSoundManager : MonoBehaviour
    {
        public static GameSoundManager Instance { get; private set; }

        [Header("=== 3D AMBIENT & BACKGROUND LOOPS (ASSIGN IN INSPECTOR) ===")]
        [Tooltip("Daytime ambient sound effect (birds, day city atmosphere, light breeze)")]
        [SerializeField] private AudioClip daySoundEffect = null;

        [Tooltip("Night city ambient sound effect (crickets, night wind, nocturnal city atmosphere)")]
        [SerializeField] private AudioClip nightCitySoundEffect = null;

        [Tooltip("City traffic ambient sound effect (distant cars, traffic murmur, engines, city hum)")]
        [SerializeField] private AudioClip cityTrafficSoundEffect = null;

        [Tooltip("Industry ambient sound effect (machinery, factory hum, train tracks, industrial equipment)")]
        [SerializeField] private AudioClip industrySoundEffect = null;

        [Tooltip("Stadium ambient sound effect (crowd chatter, chants, sports arena background)")]
        [SerializeField] private AudioClip stadiumSoundEffect = null;

        [Header("=== 3D GAMEPLAY & ACTION SOUND EFFECTS (ASSIGN IN INSPECTOR) ===")]
        [Tooltip("Click sound effect (UI button clicks, modal open/close, tabs)")]
        [SerializeField] private AudioClip clickSoundEffect = null;

        [Tooltip("Buy sound effect (purchasing flat, building, restaurant, stadium, taxi fleet)")]
        [SerializeField] private AudioClip buySoundEffect = null;

        [Tooltip("Collect sound effect (collecting rent, stadium match fee, taxi fare payout)")]
        [SerializeField] private AudioClip collectSoundEffect = null;

        [Tooltip("Vehicle sound effect (taxi engine drive loop or vehicle movement)")]
        [SerializeField] private AudioClip vehicleSoundEffect = null;

        [Tooltip("Vehicle engine acceleration sound effect (acceleration throttle / motor rev)")]
        [SerializeField] private AudioClip vehicleAccelerationSoundEffect = null;

        [Tooltip("Train sound effect (3D railway locomotion, tracks clatter, train engine loop)")]
        [SerializeField] private AudioClip trainSoundEffect = null;

        [Tooltip("Vehicle horn sound effect (taxi honking, traffic alert)")]
        [SerializeField] private AudioClip vehicleHornSoundEffect = null;

        [Tooltip("Taxi dispatch / spawn sound effect")]
        [SerializeField] private AudioClip taxiSpawnSoundEffect = null;

        [Tooltip("Stadium match cheer sound effect (goal celebration, match accepted)")]
        [SerializeField] private AudioClip stadiumMatchCheerSoundEffect = null;

        [Tooltip("Stadium match request incoming alert sound effect")]
        [SerializeField] private AudioClip stadiumMatchRequestAlertSoundEffect = null;

        [Tooltip("Eating food sound effect (restaurant dining, snack)")]
        [SerializeField] private AudioClip eatSoundEffect = null;

        [Tooltip("Drinking beverage sound effect (cafe coffee/juice sip)")]
        [SerializeField] private AudioClip drinkSoundEffect = null;

        [Tooltip("Low health warning sound effect (heartbeat pulse, hunger alarm)")]
        [SerializeField] private AudioClip lowHealthWarningSoundEffect = null;

        [Tooltip("Flat tenant rented out / lease signed sound effect")]
        [SerializeField] private AudioClip tenantMoveInSoundEffect = null;

        [Tooltip("Error / denied / insufficient funds sound effect")]
        [SerializeField] private AudioClip errorDeniedSoundEffect = null;

        [Header("=== VOLUME & AUDIO BALANCE ===")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)]
        [SerializeField] private float ambienceVolume = 0.65f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)]
        [SerializeField] private float uiVolume = 0.80f;

        [Header("=== 3D SPATIAL AUDIO CONFIGURATION ===")]
        [Tooltip("Spatial Blend: 0.0 = 2D Stereo, 1.0 = Full 3D World Spatialized")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1.0f;

        [Tooltip("Minimum distance before 3D sound starts attenuating (meters)")]
        [SerializeField] private float minDistance3D = 8f;

        [Tooltip("Maximum distance at which 3D sound can still be heard (meters)")]
        [SerializeField] private float maxDistance3D = 140f;

        [Tooltip("Attenuation rolloff mode for 3D sounds")]
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Tooltip("Day/Night ambient crossfade speed (seconds)")]
        [SerializeField] private float dayNightCrossfadeSpeed = 1.5f;

        [Header("=== 3D EMITTER REFERENCES (CONFIGURED IN SCENE) ===")]
        [SerializeField] private Transform industryEmitter;
        [SerializeField] private Transform cityTrafficEmitter;
        [SerializeField] private Transform stadiumEmitter;
        [SerializeField] private Transform dayNightAmbientEmitter;

        [Header("=== DEDICATED AUDIO SOURCES ===")]
        [SerializeField] private AudioSource dayAmbienceSource;
        [SerializeField] private AudioSource nightAmbienceSource;
        [SerializeField] private AudioSource cityTrafficSource;
        [SerializeField] private AudioSource industrySource;
        [SerializeField] private AudioSource stadiumSource;
        [SerializeField] private AudioSource uiAudioSource;

        [Header("=== 3D SFX POOL ===")]
        [SerializeField] private List<AudioSource> sfxPool = new List<AudioSource>();
        private const int SfxPoolSize = 10;
        private int currentSfxIndex = 0;

        // Public Properties for AudioClips (allows external queries)
        public AudioClip DaySoundEffect { get => daySoundEffect; set => daySoundEffect = value; }
        public AudioClip NightCitySoundEffect { get => nightCitySoundEffect; set => nightCitySoundEffect = value; }
        public AudioClip CityTrafficSoundEffect { get => cityTrafficSoundEffect; set => cityTrafficSoundEffect = value; }
        public AudioClip IndustrySoundEffect { get => industrySoundEffect; set => industrySoundEffect = value; }
        public AudioClip StadiumSoundEffect { get => stadiumSoundEffect; set => stadiumSoundEffect = value; }
        public AudioClip ClickSoundEffect { get => clickSoundEffect; set => clickSoundEffect = value; }
        public AudioClip BuySoundEffect { get => buySoundEffect; set => buySoundEffect = value; }
        public AudioClip CollectSoundEffect { get => collectSoundEffect; set => collectSoundEffect = value; }
        public AudioClip VehicleSoundEffect { get => vehicleSoundEffect; set => vehicleSoundEffect = value; }
        public AudioClip VehicleAccelerationSoundEffect { get => vehicleAccelerationSoundEffect; set => vehicleAccelerationSoundEffect = value; }
        public AudioClip TrainSoundEffect { get => trainSoundEffect; set => trainSoundEffect = value; }
        public AudioClip VehicleHornSoundEffect { get => vehicleHornSoundEffect; set => vehicleHornSoundEffect = value; }
        public AudioClip TaxiSpawnSoundEffect { get => taxiSpawnSoundEffect; set => taxiSpawnSoundEffect = value; }
        public AudioClip StadiumMatchCheerSoundEffect { get => stadiumMatchCheerSoundEffect; set => stadiumMatchCheerSoundEffect = value; }
        public AudioClip StadiumMatchRequestAlertSoundEffect { get => stadiumMatchRequestAlertSoundEffect; set => stadiumMatchRequestAlertSoundEffect = value; }
        public AudioClip EatSoundEffect { get => eatSoundEffect; set => eatSoundEffect = value; }
        public AudioClip DrinkSoundEffect { get => drinkSoundEffect; set => drinkSoundEffect = value; }
        public AudioClip LowHealthWarningSoundEffect { get => lowHealthWarningSoundEffect; set => lowHealthWarningSoundEffect = value; }
        public AudioClip TenantMoveInSoundEffect { get => tenantMoveInSoundEffect; set => tenantMoveInSoundEffect = value; }
        public AudioClip ErrorDeniedSoundEffect { get => errorDeniedSoundEffect; set => errorDeniedSoundEffect = value; }

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                ApplyVolumes();
            }
        }

        public float AmbienceVolume
        {
            get => ambienceVolume;
            set
            {
                ambienceVolume = Mathf.Clamp01(value);
                ApplyVolumes();
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                ApplyVolumes();
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

            EnsureAudioHierarchy();
            AutoLocateWorldEmitters();
        }

        private void Start()
        {
            ApplyVolumes();
            InitializeAmbientLoops();
        }

        private void Update()
        {
            UpdateDayNightAmbience();
            UpdateAmbientSources();
            FollowCameraWithDayNightEmitter();
        }

        /// <summary>
        /// Guarantees that all child emitter GameObjects and 3D AudioSources are fully constructed.
        /// </summary>
        public void EnsureAudioHierarchy()
        {
            // 1. Day / Night Ambience Emitter
            if (dayNightAmbientEmitter == null)
            {
                var tr = transform.Find("DayNightAmbience_3D");
                dayNightAmbientEmitter = tr != null ? tr : CreateChildEmitter("DayNightAmbience_3D", Vector3.zero);
            }
            if (dayAmbienceSource == null && dayNightAmbientEmitter != null)
            {
                dayAmbienceSource = GetOrAddSource(dayNightAmbientEmitter.gameObject, true);
            }
            if (nightAmbienceSource == null && dayNightAmbientEmitter != null)
            {
                var nightChild = dayNightAmbientEmitter.Find("NightSource");
                if (nightChild == null)
                {
                    GameObject no = new GameObject("NightSource");
                    no.transform.SetParent(dayNightAmbientEmitter, false);
                    nightChild = no.transform;
                }
                nightAmbienceSource = GetOrAddSource(nightChild.gameObject, true);
            }

            // 2. City Traffic Ambience Emitter
            if (cityTrafficEmitter == null)
            {
                var tr = transform.Find("CityTrafficAmbience_3D");
                cityTrafficEmitter = tr != null ? tr : CreateChildEmitter("CityTrafficAmbience_3D", new Vector3(0f, 0.5f, 0f));
            }
            if (cityTrafficSource == null && cityTrafficEmitter != null)
            {
                cityTrafficSource = GetOrAddSource(cityTrafficEmitter.gameObject, true);
            }

            // 3. Industry Ambience Emitter
            if (industryEmitter == null)
            {
                var tr = transform.Find("IndustryAmbience_3D");
                industryEmitter = tr != null ? tr : CreateChildEmitter("IndustryAmbience_3D", new Vector3(-35f, 0.5f, 15f));
            }
            if (industrySource == null && industryEmitter != null)
            {
                industrySource = GetOrAddSource(industryEmitter.gameObject, true);
            }

            // 4. Stadium Ambience Emitter
            if (stadiumEmitter == null)
            {
                var tr = transform.Find("StadiumAmbience_3D");
                stadiumEmitter = tr != null ? tr : CreateChildEmitter("StadiumAmbience_3D", new Vector3(45f, 1f, 35f));
            }
            if (stadiumSource == null && stadiumEmitter != null)
            {
                stadiumSource = GetOrAddSource(stadiumEmitter.gameObject, true);
            }

            // 5. UI Audio Source
            if (uiAudioSource == null)
            {
                var tr = transform.Find("UI_Audio");
                Transform uiTr = tr != null ? tr : CreateChildEmitter("UI_Audio", Vector3.zero);
                uiAudioSource = GetOrAddSource(uiTr.gameObject, false);
                uiAudioSource.spatialBlend = 0f; // UI is stereo 2D
            }

            // 6. 3D SFX Pool
            if (sfxPool == null) sfxPool = new List<AudioSource>();
            sfxPool.RemoveAll(s => s == null);

            Transform poolRoot = transform.Find("SFX_Pool");
            if (poolRoot == null)
            {
                poolRoot = CreateChildEmitter("SFX_Pool", Vector3.zero);
            }

            while (sfxPool.Count < SfxPoolSize)
            {
                int index = sfxPool.Count + 1;
                GameObject sfxObj = new GameObject($"SFX_Source_{index}");
                sfxObj.transform.SetParent(poolRoot, false);
                AudioSource s = GetOrAddSource(sfxObj, false);
                sfxPool.Add(s);
            }
        }

        private Transform CreateChildEmitter(string name, Vector3 localPos)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.transform.localPosition = localPos;
            return obj.transform;
        }

        private AudioSource GetOrAddSource(GameObject target, bool isLoop)
        {
            AudioSource src = target.GetComponent<AudioSource>();
            if (src == null)
            {
                src = target.AddComponent<AudioSource>();
            }
            src.playOnAwake = false;
            src.loop = isLoop;
            src.spatialBlend = spatialBlend;
            src.minDistance = minDistance3D;
            src.maxDistance = maxDistance3D;
            src.rolloffMode = rolloffMode;
            src.dopplerLevel = 0f;
            return src;
        }

        /// <summary>
        /// Automatically locates city landmarks (stadium, factories, road network) and positions the 3D emitters.
        /// </summary>
        public void AutoLocateWorldEmitters()
        {
            // 1. Locate Stadium
            if (stadiumEmitter != null)
            {
                var allBuildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in allBuildings)
                {
                    if (b != null && b.PropertyType == PropertyType.Stadium)
                    {
                        stadiumEmitter.position = b.transform.position;
                        break;
                    }
                }
            }

            // 2. Locate Industry (Train tracks, factories)
            if (industryEmitter != null)
            {
                GameObject factory = GameObject.Find("Building_Factory");
                if (factory == null) factory = GameObject.Find("Building_Factory (1)");
                if (factory == null) factory = GameObject.Find("Train (1)");
                if (factory == null) factory = GameObject.Find("Train");

                if (factory != null)
                {
                    industryEmitter.position = factory.transform.position;
                }
            }

            // 3. Locate City Traffic Center
            if (cityTrafficEmitter != null)
            {
                GameObject roadNet = GameObject.Find("RoadNetwork");
                if (roadNet != null)
                {
                    cityTrafficEmitter.position = roadNet.transform.position;
                }
            }
        }

        private void InitializeAmbientLoops()
        {
            if (dayAmbienceSource != null && daySoundEffect != null)
            {
                dayAmbienceSource.clip = daySoundEffect;
                dayAmbienceSource.Play();
            }

            if (nightAmbienceSource != null && nightCitySoundEffect != null)
            {
                nightAmbienceSource.clip = nightCitySoundEffect;
                nightAmbienceSource.Play();
            }

            if (cityTrafficSource != null && cityTrafficSoundEffect != null)
            {
                cityTrafficSource.clip = cityTrafficSoundEffect;
                cityTrafficSource.Play();
            }

            if (industrySource != null && industrySoundEffect != null)
            {
                industrySource.clip = industrySoundEffect;
                industrySource.Play();
            }

            if (stadiumSource != null && stadiumSoundEffect != null)
            {
                stadiumSource.clip = stadiumSoundEffect;
                stadiumSource.Play();
            }
        }

        private void FollowCameraWithDayNightEmitter()
        {
            if (dayNightAmbientEmitter != null && Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                dayNightAmbientEmitter.position = new Vector3(camPos.x, 0f, camPos.z);
            }
        }

        private void UpdateDayNightAmbience()
        {
            bool isDay = true;
            if (DayNightCycleManager.Instance != null)
            {
                isDay = DayNightCycleManager.Instance.IsDaytime;
            }

            float targetDayVol = isDay ? (ambienceVolume * masterVolume) : 0f;
            float targetNightVol = !isDay ? (ambienceVolume * masterVolume) : 0f;

            if (dayAmbienceSource != null)
            {
                dayAmbienceSource.volume = Mathf.MoveTowards(dayAmbienceSource.volume, targetDayVol, Time.deltaTime * dayNightCrossfadeSpeed);
            }

            if (nightAmbienceSource != null)
            {
                nightAmbienceSource.volume = Mathf.MoveTowards(nightAmbienceSource.volume, targetNightVol, Time.deltaTime * dayNightCrossfadeSpeed);
            }
        }

        /// <summary>
        /// Dynamically starts or stops ambient clips when assigned or changed during play mode.
        /// </summary>
        private void UpdateAmbientSources()
        {
            SyncAmbientSource(dayAmbienceSource, daySoundEffect);
            SyncAmbientSource(nightAmbienceSource, nightCitySoundEffect);
            SyncAmbientSource(cityTrafficSource, cityTrafficSoundEffect);
            SyncAmbientSource(industrySource, industrySoundEffect);
            SyncAmbientSource(stadiumSource, stadiumSoundEffect);
        }

        private void SyncAmbientSource(AudioSource src, AudioClip clip)
        {
            if (src == null) return;

            if (clip != null)
            {
                if (src.clip != clip)
                {
                    src.clip = clip;
                    src.Play();
                }
                else if (!src.isPlaying)
                {
                    src.Play();
                }
            }
            else
            {
                if (src.isPlaying)
                {
                    src.Stop();
                }
                src.clip = null;
            }
        }

        private void ApplyVolumes()
        {
            if (cityTrafficSource != null) cityTrafficSource.volume = ambienceVolume * masterVolume;
            if (industrySource != null) industrySource.volume = ambienceVolume * masterVolume;
            if (stadiumSource != null) stadiumSource.volume = ambienceVolume * masterVolume;
            if (uiAudioSource != null) uiAudioSource.volume = uiVolume * masterVolume;

            foreach (var s in sfxPool)
            {
                if (s != null) s.volume = sfxVolume * masterVolume;
            }
        }

        // =========================================================================
        // PUBLIC SFX PLAYBACK METHODS (NULL-SAFE & 3D SPATIALIZED)
        // =========================================================================

        public void PlayClick(Vector3? worldPos = null)
        {
            if (clickSoundEffect == null) return;
            if (worldPos.HasValue)
            {
                PlaySoundAtPosition(clickSoundEffect, worldPos.Value, uiVolume);
            }
            else
            {
                PlayUIOneShot(clickSoundEffect, uiVolume);
            }
        }

        public void PlayBuy(Vector3? worldPos = null)
        {
            if (buySoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(buySoundEffect, pos, sfxVolume);
        }

        public void PlayCollect(Vector3? worldPos = null)
        {
            if (collectSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(collectSoundEffect, pos, sfxVolume);
        }

        public void PlayVehicleSound(Vector3? worldPos = null)
        {
            AudioClip clip = vehicleAccelerationSoundEffect != null ? vehicleAccelerationSoundEffect : vehicleSoundEffect;
            if (clip == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(clip, pos, sfxVolume * 1.0f);
        }

        public void PlayTrainSound(Vector3? worldPos = null, float volumeMult = 1.0f)
        {
            if (trainSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(trainSoundEffect, pos, sfxVolume * Mathf.Clamp01(volumeMult));
        }

        public void PlayVehicleHorn(Vector3? worldPos = null)
        {
            if (vehicleHornSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(vehicleHornSoundEffect, pos, sfxVolume);
        }

        public void PlayTaxiSpawn(Vector3? worldPos = null)
        {
            if (taxiSpawnSoundEffect != null)
            {
                Vector3 pos = worldPos ?? GetDefaultListenerPosition();
                PlaySoundAtPosition(taxiSpawnSoundEffect, pos, sfxVolume);
            }
            else if (vehicleHornSoundEffect != null)
            {
                PlayVehicleHorn(worldPos);
            }
        }

        public void PlayStadiumMatchCheer(Vector3? worldPos = null)
        {
            if (stadiumMatchCheerSoundEffect == null) return;
            Vector3 pos = worldPos ?? (stadiumEmitter != null ? stadiumEmitter.position : GetDefaultListenerPosition());
            PlaySoundAtPosition(stadiumMatchCheerSoundEffect, pos, sfxVolume * 1.15f);
        }

        public void PlayStadiumMatchRequestAlert(Vector3? worldPos = null)
        {
            if (stadiumMatchRequestAlertSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(stadiumMatchRequestAlertSoundEffect, pos, sfxVolume);
        }

        public void PlayNotification(Vector3? worldPos = null)
        {
            PlayStadiumMatchRequestAlert(worldPos);
        }

        public void PlaySuccess(Vector3? worldPos = null)
        {
            PlayCollect(worldPos);
        }

        public void PlayEat(Vector3? worldPos = null)
        {
            if (eatSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(eatSoundEffect, pos, sfxVolume);
        }

        public void PlayDrink(Vector3? worldPos = null)
        {
            if (drinkSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(drinkSoundEffect, pos, sfxVolume);
        }

        public void PlayLowHealthWarning()
        {
            if (lowHealthWarningSoundEffect == null) return;
            PlayUIOneShot(lowHealthWarningSoundEffect, uiVolume * 1.1f);
        }

        public void PlayTenantMoveIn(Vector3? worldPos = null)
        {
            if (tenantMoveInSoundEffect == null) return;
            Vector3 pos = worldPos ?? GetDefaultListenerPosition();
            PlaySoundAtPosition(tenantMoveInSoundEffect, pos, sfxVolume);
        }

        public void PlayError(Vector3? worldPos = null)
        {
            if (errorDeniedSoundEffect == null) return;
            if (worldPos.HasValue)
            {
                PlaySoundAtPosition(errorDeniedSoundEffect, worldPos.Value, uiVolume);
            }
            else
            {
                PlayUIOneShot(errorDeniedSoundEffect, uiVolume);
            }
        }

        /// <summary>
        /// Plays a sound effect at a specific 3D world position using a pooled AudioSource.
        /// </summary>
        public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSFXSource();
            if (source == null) return;

            source.transform.position = position;
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volumeMultiplier;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance3D;
            source.maxDistance = maxDistance3D;
            source.rolloffMode = rolloffMode;
            source.dopplerLevel = 0f;
            source.Play();
        }

        private void PlayUIOneShot(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;
            if (uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(clip, uiVolume * masterVolume * volumeMultiplier);
            }
            else
            {
                PlaySoundAtPosition(clip, GetDefaultListenerPosition(), volumeMultiplier);
            }
        }

        private AudioSource GetNextSFXSource()
        {
            if (sfxPool == null || sfxPool.Count == 0) return null;

            for (int i = 0; i < sfxPool.Count; i++)
            {
                int index = (currentSfxIndex + i) % sfxPool.Count;
                if (sfxPool[index] != null && !sfxPool[index].isPlaying)
                {
                    currentSfxIndex = (index + 1) % sfxPool.Count;
                    return sfxPool[index];
                }
            }

            // If all sources are busy, steal the oldest one
            AudioSource src = sfxPool[currentSfxIndex];
            currentSfxIndex = (currentSfxIndex + 1) % sfxPool.Count;
            return src;
        }

        private Vector3 GetDefaultListenerPosition()
        {
            if (Camera.main != null)
            {
                return Camera.main.transform.position;
            }
            return transform.position;
        }
    }
}
