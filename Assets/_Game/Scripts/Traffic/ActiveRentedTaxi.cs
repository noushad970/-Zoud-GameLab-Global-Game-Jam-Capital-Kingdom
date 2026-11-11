using System.Collections;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Simulation;
using EmpireBuilder.UI;
using EmpireBuilder.World;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Attached to a dynamically spawned rented taxi vehicle driving on the city streets.
    /// Features:
    /// - Distinctive 2D green overhead shape marker so the player immediately identifies their rented taxi.
    /// - Periodic passenger fare generation credited directly to the player's account.
    /// - Autonomous return-to-station navigation and despawning once the daily shift/period ends.
    /// - Fully scene and inspector configurable.
    /// </summary>
    [DisallowMultipleComponent]
    public class ActiveRentedTaxi : MonoBehaviour
    {
        [Header("Origin Station & Rental Info")]
        [SerializeField] private PropertyBuilding originStation;
        [SerializeField] private int taxiNumber = 1;
        [SerializeField] private bool isActiveOnRoad = false;
        [SerializeField] private bool isReturningToStation = false;

        [Header("Rental Shift Duration")]
        [Tooltip("Real-time seconds of the taxi's active rental shift before returning to station")]
        [SerializeField] private float rentalShiftDurationSeconds = 80f;
        [SerializeField] private float remainingShiftTime = 80f;

        [Header("Fare Economics")]
        [SerializeField] private float minFareInterval = 12f;
        [SerializeField] private float maxFareInterval = 22f;
        [SerializeField] private double minFareAmount = 35.0;
        [SerializeField] private double maxFareAmount = 110.0;
        [SerializeField] private double totalFaresEarned = 0.0;
        [SerializeField] private int tripsCompleted = 0;

        [Header("2D Green Shape Overhead Marker")]
        [Tooltip("The 2D green shape sprite displayed directly above the taxi roof")]
        [SerializeField] private Sprite greenShapeSprite;
        [SerializeField] private Vector3 markerLocalOffset = new Vector3(0f, 2.65f, 0f);
        [SerializeField] private Vector3 markerScale = new Vector3(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color markerColor = new Color(0.12f, 1f, 0.32f, 1f);
        [SerializeField] private bool billboardToCamera = true;
        [SerializeField] private bool animateFloatingBob = true;

        private GameObject markerObject;
        private SpriteRenderer markerRenderer;
        private Transform exitPointTransform;
        private Vector3 targetStationPosition;
        private AICarController aiCar;
        private Coroutine fareCoroutine;
        private Coroutine shiftCoroutine;
        private Camera cachedCamera;

        public int TaxiNumber => taxiNumber;
        public bool IsActiveOnRoad => isActiveOnRoad;
        public bool IsReturningToStation => isReturningToStation;
        public double TotalFaresEarned => totalFaresEarned;
        public int TripsCompleted => tripsCompleted;
        public float RemainingShiftTime => remainingShiftTime;

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted += HandleDayCompleted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted -= HandleDayCompleted;
            }

            if (fareCoroutine != null)
            {
                StopCoroutine(fareCoroutine);
                fareCoroutine = null;
            }

            if (shiftCoroutine != null)
            {
                StopCoroutine(shiftCoroutine);
                shiftCoroutine = null;
            }

            if (originStation != null)
            {
                originStation.NotifyTaxiDespawned(this);
            }
        }

        private void LateUpdate()
        {
            // Billboard the 2D green shape marker to face the active camera
            if (markerObject != null && billboardToCamera)
            {
                if (cachedCamera == null) cachedCamera = Camera.main;
                if (cachedCamera != null)
                {
                    Transform camT = cachedCamera.transform;
                    markerObject.transform.rotation = Quaternion.LookRotation(camT.forward, camT.up);
                }

                if (animateFloatingBob)
                {
                    float bobOffset = Mathf.Sin(Time.time * 3.5f) * 0.08f;
                    markerObject.transform.localPosition = markerLocalOffset + new Vector3(0f, bobOffset, 0f);
                }
            }

            // Return-to-station arrival check
            if (isReturningToStation && isActiveOnRoad)
            {
                float dist = Vector3.Distance(transform.position, targetStationPosition);
                if (dist <= 4.5f)
                {
                    CompleteShiftAndDespawn();
                }
            }
        }

        /// <summary>
        /// Initializes and dispatches the newly spawned rented taxi vehicle.
        /// </summary>
        public void SetupSpawnedTaxi(
            PropertyBuilding station,
            int number,
            Transform exitPoint,
            TrafficWaypointNode startWaypoint,
            GameObject roadNet,
            float shiftDuration = 80f,
            Sprite customSprite = null)
        {
            originStation = station;
            taxiNumber = number;
            exitPointTransform = exitPoint;
            rentalShiftDurationSeconds = shiftDuration > 10f ? shiftDuration : 80f;
            remainingShiftTime = rentalShiftDurationSeconds;

            if (exitPoint != null)
            {
                targetStationPosition = exitPoint.position;
                transform.position = exitPoint.position;
                transform.rotation = exitPoint.rotation;
            }
            else if (station != null)
            {
                targetStationPosition = station.transform.position;
                transform.position = station.transform.position;
                transform.rotation = station.transform.rotation;
            }

            if (customSprite != null)
            {
                greenShapeSprite = customSprite;
            }

            CreateGreenOverheadMarker();

            // Configure AI controller
            aiCar = GetComponent<AICarController>();
            if (aiCar == null)
            {
                aiCar = gameObject.AddComponent<AICarController>();
            }

            aiCar.SetAsRentedTaxi(true);
            aiCar.enabled = true;

            if (startWaypoint != null)
            {
                aiCar.SetInitialWaypoint(startWaypoint);
            }

            isActiveOnRoad = true;
            isReturningToStation = false;

            if (EmpireBuilder.Audio.GameSoundManager.Instance != null &&
                (EmpireBuilder.Audio.GameSoundManager.Instance.VehicleSoundEffect != null || EmpireBuilder.Audio.GameSoundManager.Instance.VehicleAccelerationSoundEffect != null))
            {
                AudioSource taxiAudio = GetComponent<AudioSource>();
                if (taxiAudio == null) taxiAudio = gameObject.AddComponent<AudioSource>();
                taxiAudio.clip = EmpireBuilder.Audio.GameSoundManager.Instance.VehicleAccelerationSoundEffect ??
                                 EmpireBuilder.Audio.GameSoundManager.Instance.VehicleSoundEffect;
                taxiAudio.loop = true;
                taxiAudio.spatialBlend = 1.0f;
                taxiAudio.minDistance = 8f;
                taxiAudio.maxDistance = 110f;
                taxiAudio.rolloffMode = AudioRolloffMode.Logarithmic;
                taxiAudio.volume = 0.95f; // Clearly audible engine acceleration sound
                taxiAudio.Play();
            }

            if (fareCoroutine != null) StopCoroutine(fareCoroutine);
            fareCoroutine = StartCoroutine(FareEarningLoop());

            if (shiftCoroutine != null) StopCoroutine(shiftCoroutine);
            shiftCoroutine = StartCoroutine(ShiftCountdownRoutine());

            Debug.Log($"[ActiveRentedTaxi] Spawned Rented Taxi #{taxiNumber} with Green Overhead Marker deployed from '{station.PropertyName}'!");
        }

        /// <summary>
        /// Creates a 2D green shape object directly above the taxi roof.
        /// </summary>
        private void CreateGreenOverheadMarker()
        {
            if (markerObject != null) return;

            markerObject = new GameObject("MyTaxiMarker");
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = markerLocalOffset;
            markerObject.transform.localScale = markerScale;

            markerRenderer = markerObject.AddComponent<SpriteRenderer>();

            // Attempt to load green button/shape sprite from assets if not assigned
            if (greenShapeSprite == null)
            {
#if UNITY_EDITOR
                greenShapeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Green.png"
                );
                if (greenShapeSprite == null)
                {
                    greenShapeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                        "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Square/Green.png"
                    );
                }
#endif
            }

            if (greenShapeSprite == null)
            {
                greenShapeSprite = GenerateFallbackGreenShapeSprite();
            }

            markerRenderer.sprite = greenShapeSprite;
            markerRenderer.color = markerColor;
            markerRenderer.sortingOrder = 500; // Ensure visible over world models
        }

        private Sprite GenerateFallbackGreenShapeSprite()
        {
            // Procedurally generates a crisp green diamond/badge 64x64 sprite
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            Color green = markerColor;
            Color border = new Color(1f, 1f, 1f, 0.95f);

            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - half);
                    int dy = Mathf.Abs(y - half);
                    int dist = dx + dy;

                    if (dist < half - 2)
                    {
                        tex.SetPixel(x, y, green);
                    }
                    else if (dist <= half)
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private IEnumerator ShiftCountdownRoutine()
        {
            while (remainingShiftTime > 0f)
            {
                yield return new WaitForSeconds(1f);
                remainingShiftTime -= 1f;
            }

            // Shift finished: return to station
            InitiateReturnToStation();
        }

        private IEnumerator FareEarningLoop()
        {
            while (isActiveOnRoad && !isReturningToStation)
            {
                float delay = Random.Range(minFareInterval, maxFareInterval);
                yield return new WaitForSeconds(delay);

                if (!isActiveOnRoad || isReturningToStation) break;

                // Pick random passenger fare
                double fare = System.Math.Round(Random.Range((float)minFareAmount, (float)maxFareAmount), 0);
                totalFaresEarned += fare;
                tripsCompleted++;

                int currentDay = 1;
                if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
                {
                    currentDay = GameManager.Instance.Simulation.CurrentDay;
                }

                // Credit cash to player
                if (GameManager.Instance != null && GameManager.Instance.Economy != null)
                {
                    GameManager.Instance.Economy.ApplyTransaction(
                        fare,
                        TransactionCategory.Revenue,
                        $"Rented Taxi #{taxiNumber} Passenger Fare",
                        currentDay
                    );
                }
                else
                {
                    float savedCash = PlayerPrefs.GetFloat("SavedPlayerCash", 10000f);
                    savedCash += (float)fare;
                    PlayerPrefs.SetFloat("SavedPlayerCash", savedCash);
                    PlayerPrefs.Save();
                }

                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();

                // Display celebratory toast
                PropertyDetailModalUI.Instance?.ShowToast($"🚖 Rented Taxi #{taxiNumber} picked up passenger! Fare: +${fare:N0}");

                EmpireBuilder.Audio.GameSoundManager.Instance?.PlayCollect(transform.position);
            }
        }

        private void HandleDayCompleted(DailyReport report)
        {
            InitiateReturnToStation();
        }

        /// <summary>
        /// Orders the rented taxi to finish its shift and drive back to the taxi station.
        /// </summary>
        public void InitiateReturnToStation()
        {
            if (isReturningToStation) return;
            isReturningToStation = true;

            if (fareCoroutine != null)
            {
                StopCoroutine(fareCoroutine);
                fareCoroutine = null;
            }

            PropertyDetailModalUI.Instance?.ShowToast($"🚖 Rented Taxi #{taxiNumber}'s shift finished. Returning to station...");
            Debug.Log($"[ActiveRentedTaxi] Taxi #{taxiNumber} daily rental period finished. Returning to station.");

            // Guide vehicle back to station
            if (aiCar != null)
            {
                TrafficWaypointNode nearestStationNode = FindNearestWaypointTo(targetStationPosition);
                if (nearestStationNode != null)
                {
                    aiCar.SetInitialWaypoint(nearestStationNode);
                }
            }

            // Fallback safety timer: if it doesn't reach station in 20 seconds, despawn automatically
            StartCoroutine(SafetyDespawnRoutine(20f));
        }

        private IEnumerator SafetyDespawnRoutine(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            CompleteShiftAndDespawn();
        }

        private void CompleteShiftAndDespawn()
        {
            if (!isActiveOnRoad) return;
            isActiveOnRoad = false;

            PropertyDetailModalUI.Instance?.ShowToast(
                $"🚖 Taxi #{taxiNumber} returned to station! Total Shift Fares: +${totalFaresEarned:N0} ({tripsCompleted} trips)"
            );

            EmpireBuilder.Audio.GameSoundManager.Instance?.PlayVehicleHorn(transform.position);

            Debug.Log($"[ActiveRentedTaxi] Taxi #{taxiNumber} returned to station. Despawning spawned vehicle.");

            if (originStation != null)
            {
                originStation.NotifyTaxiDespawned(this);
            }

            Destroy(gameObject);
        }

        private TrafficWaypointNode FindNearestWaypointTo(Vector3 pos)
        {
            var nodes = UnityEngine.Object.FindObjectsByType<TrafficWaypointNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            TrafficWaypointNode closest = null;
            float minDist = float.MaxValue;
            foreach (var n in nodes)
            {
                if (n == null) continue;
                float d = Vector3.Distance(pos, n.Position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = n;
                }
            }
            return closest;
        }
    }
}
