using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    public enum AICarState
    {
        Cruising,
        Accelerating,
        Following,
        Braking,
        Stopped,
        Turning,
        YieldingIntersection,
        PassingOncoming,
        BypassingLateral,
        Reversing
    }

    /// <summary>
    /// Autonomous AI Car Controller.
    /// Drives along roadside road networks, chooses random turns at intersections,
    /// senses other cars ahead, and smoothly accelerates, decelerates, brakes, or stops.
    /// </summary>
    public class AICarController : MonoBehaviour
    {
        [Header("Speed & Driving Settings")]
        [Tooltip("Target cruising speed in units/second.")]
        [SerializeField] private float cruisingSpeed = 11.5f;

        [Tooltip("Acceleration rate in units/second^2.")]
        [SerializeField] private float accelerationRate = 6.5f;

        [Tooltip("Braking deceleration rate in units/second^2.")]
        [SerializeField] private float brakingRate = 12.0f;

        [Tooltip("Emergency stop deceleration rate in units/second^2.")]
        [SerializeField] private float emergencyBrakeRate = 26.0f;

        [Tooltip("Steering angular speed in degrees/second.")]
        [SerializeField] private float steeringSpeed = 160.0f;

        [Tooltip("Arrival distance threshold to reach a waypoint.")]
        [SerializeField] private float waypointArrivalDistance = 2.0f;

        [Header("Obstacle & Car Detection")]
        [Tooltip("Maximum forward distance to detect other cars.")]
        [SerializeField] private float maxDetectionDistance = 14.0f;

        [Tooltip("Distance at which the car starts decelerating / matching speed.")]
        [SerializeField] private float cautionDistance = 9.0f;

        [Tooltip("Distance at which the car must come to a full stop.")]
        [SerializeField] private float fullStopDistance = 3.6f;

        [Tooltip("Radius of the forward sensor sphere.")]
        [SerializeField] private float sensorRadius = 0.75f;

        [Tooltip("Height above ground of the forward sensor origin.")]
        [SerializeField] private float sensorHeight = 0.45f;

        [Tooltip("Forward offset from car pivot to front bumper.")]
        [SerializeField] private float frontBumperOffset = 1.2f;

        [Tooltip("Layer mask for obstacle detection.")]
        [SerializeField] private LayerMask detectionLayerMask = ~0;

        [System.Serializable]
        public class WheelInfo
        {
            public Transform transform;
            public Vector3 initialLocalPos;
            public Quaternion initialLocalRot;
            public float currentAngle;
            public bool isFrontWheel;
        }

        [Header("Waypoints & Route")]
        [SerializeField] private TrafficWaypointNode currentWaypoint;
        [SerializeField] private TrafficWaypointNode previousWaypoint;

        [Header("Assigned Road Network")]
        [Tooltip("Explicit road network GameObject this car should follow. If assigned in Inspector, car strictly follows this network. If null, assigned automatically by CityTrafficManager.")]
        [SerializeField] private GameObject assignedRoadNetwork;
        [SerializeField] private string assignedNetworkName = "";
        [SerializeField] private List<TrafficWaypointNode> assignedRoadWaypoints = new List<TrafficWaypointNode>();

        public GameObject AssignedRoadNetwork => assignedRoadNetwork;
        public string AssignedNetworkName => assignedNetworkName;
        public IReadOnlyList<TrafficWaypointNode> AssignedRoadWaypoints => assignedRoadWaypoints;

        public void SetAssignedRoadNetwork(GameObject roadNet, string netName, List<TrafficWaypointNode> waypoints)
        {
            assignedRoadNetwork = roadNet;
            assignedNetworkName = netName;
            assignedRoadWaypoints = waypoints != null ? new List<TrafficWaypointNode>(waypoints) : new List<TrafficWaypointNode>();
        }

        [Header("Visuals & Wheels")]
        [SerializeField] private float wheelRadius = 0.35f;
        [SerializeField] private List<WheelInfo> wheels = new List<WheelInfo>();
        private float currentSteerAngle = 0f;

        [Header("Telemetry (Read-Only)")]
        [SerializeField] private AICarState currentState = AICarState.Cruising;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float targetSpeed = 0f;
        [SerializeField] private float distanceToCarAhead = 999f;
        [SerializeField] private bool isBraking = false;
        [SerializeField] private AICarController carAhead = null;

        // Static non-alloc buffers to prevent GC allocation stutter on mobile/WebGL
        private static readonly RaycastHit[] staticHitsBuffer = new RaycastHit[24];
        private static readonly Collider[] staticOverlapBuffer = new Collider[16];

        [Header("Player & Pedestrian Detection")]
        [Tooltip("Maximum forward distance to detect a player/pedestrian.")]
        [SerializeField] private float playerDetectionDistance = 8.5f;
        [SerializeField] private bool isStoppedForPlayer = false;
        [SerializeField] private GameObject playerAhead = null;
        [SerializeField] private float distanceToPlayerAhead = 999f;

        [Header("Collision & Roadside Evasion")]
        [Tooltip("Lateral offset in meters to shift towards the road right side when evading oncoming traffic.")]
        [SerializeField] private float rightSideEvasionOffset = 2.2f;

        [Tooltip("Speed of shifting laterally towards the roadside or back to lane center.")]
        [SerializeField] private float evasionShiftSpeed = 4.0f;

        [Tooltip("Reduced cruising speed when passing tight oncoming traffic.")]
        [SerializeField] private float passingSpeed = 6.0f;

        [SerializeField] private float currentEvasionOffset = 0f;
        [SerializeField] private float targetEvasionOffset = 0f;
        [SerializeField] private bool isOncomingDetected = false;

        [Header("Traffic Jam & Clearance Maneuvers")]
        [Tooltip("Seconds stopped behind/facing a car before executing an unjam maneuver.")]
        [SerializeField] private float unjamThreshold = 1.8f;

        [Tooltip("Lateral bypass offset in meters to steer around a stopped car (left or right).")]
        [SerializeField] private float maxLateralBypassOffset = 2.5f;

        [Tooltip("Reverse driving speed in m/s.")]
        [SerializeField] private float reverseSpeed = 3.8f;

        [Tooltip("Duration in seconds to reverse when backing up.")]
        [SerializeField] private float reverseDuration = 2.2f;

        [Tooltip("Lateral shift speed when clearing traffic.")]
        [SerializeField] private float clearanceShiftSpeed = 5.2f;

        [Header("3D Engine Audio")]
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioClip engineAudioClip = null;
        [Range(0f, 1f)]
        [SerializeField] private float engineBaseVolume = 0.88f;
        [SerializeField] private float minPitch = 0.82f;
        [SerializeField] private float maxPitch = 1.35f;

        [SerializeField] private bool isReversing = false;
        [SerializeField] private float blockedByCarTimer = 0f;
        private float playerWaitTimer = 0f;
        private float currentUnjamThreshold = 1.8f;
        private float reverseTimer = 0f;
        private float reverseSteerDirection = 1f;

        private float stuckTimer = 0f;
        private float streetLevelY = 0f;
        private TrafficWaypointNode reservedIntersection = null;
        private float intersectionWaitTimer = 0f;

        public AICarState CurrentState => currentState;
        [Header("Rented Taxi Status")]
        [SerializeField] private bool isRentedTaxi = false;

        public float CurrentSpeed => currentSpeed;
        public bool IsBraking => isBraking;
        public bool IsOncomingDetected => isOncomingDetected;
        public bool IsReversing => isReversing;
        public bool IsRentedTaxi => isRentedTaxi;

        public void SetAsRentedTaxi(bool rented)
        {
            isRentedTaxi = rented;
        }

        private void Awake()
        {
            // Taxis in Taxi Stations are reserved for player taxi features and should not have AI traffic behaviour unless rented
            if (!isRentedTaxi && CityTrafficManager.IsInTaxiStation(transform))
            {
                enabled = false;
                Destroy(this);
                return;
            }

            streetLevelY = transform.position.y;

            // Randomize unjam threshold per car so they don't act in identical lockstep
            currentUnjamThreshold = Random.Range(1.3f, 2.4f);

            // Add subtle random variance to speed so cars don't move like clones
            cruisingSpeed = Random.Range(9.5f, 13.0f);

            // Initialize wheels and record resting local transforms
            InitializeWheels();

            // Ensure car has a trigger / collider for other cars to sense
            EnsureCollider();

            // Ensure car lights (dual forward headlights & glowing taillights) are attached and operational
            if (GetComponent<AICarLightController>() == null)
            {
                gameObject.AddComponent<AICarLightController>();
            }
        }

        private void Start()
        {
            targetSpeed = cruisingSpeed;
            SetupEngineAudio();
        }

        private void Update()
        {
            UpdateSensors();
            UpdateIntersectionYielding();
            UpdateSpeed();
            UpdateSteeringAndMovement();
            UpdateWheels();
            UpdateEngineAudio();
            UpdateWatchdog();
        }

        private void SetupEngineAudio()
        {
            if (engineAudioSource == null) engineAudioSource = GetComponent<AudioSource>();
            if (engineAudioSource == null) engineAudioSource = gameObject.AddComponent<AudioSource>();

            engineAudioSource.spatialBlend = 1.0f; // Full 3D World Spatialized
            engineAudioSource.minDistance = 6.0f;
            engineAudioSource.maxDistance = 110.0f;
            engineAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            engineAudioSource.loop = true;
            engineAudioSource.playOnAwake = false;

            if (engineAudioClip == null && EmpireBuilder.Audio.GameSoundManager.Instance != null)
            {
                engineAudioClip = EmpireBuilder.Audio.GameSoundManager.Instance.VehicleAccelerationSoundEffect ??
                                  EmpireBuilder.Audio.GameSoundManager.Instance.VehicleSoundEffect;
            }

            if (engineAudioClip != null)
            {
                engineAudioSource.clip = engineAudioClip;
                engineAudioSource.volume = engineBaseVolume;
                if (!engineAudioSource.isPlaying) engineAudioSource.Play();
            }
        }

        private void UpdateEngineAudio()
        {
            if (engineAudioSource == null) return;

            if (engineAudioClip == null && EmpireBuilder.Audio.GameSoundManager.Instance != null)
            {
                engineAudioClip = EmpireBuilder.Audio.GameSoundManager.Instance.VehicleAccelerationSoundEffect ??
                                  EmpireBuilder.Audio.GameSoundManager.Instance.VehicleSoundEffect;
            }

            if (engineAudioClip != null && engineAudioSource.clip != engineAudioClip)
            {
                engineAudioSource.clip = engineAudioClip;
                if (!engineAudioSource.isPlaying) engineAudioSource.Play();
            }

            if (engineAudioSource.isPlaying)
            {
                float speedRatio = Mathf.Clamp01(Mathf.Abs(currentSpeed) / Mathf.Max(1.0f, cruisingSpeed));
                bool isAccelerating = (currentState == AICarState.Accelerating || (currentSpeed < targetSpeed && targetSpeed > 0.5f));

                // Boost volume and pitch dynamically on acceleration so user clearly hears the engine rev
                float targetVol = isAccelerating ? engineBaseVolume : Mathf.Lerp(engineBaseVolume * 0.65f, engineBaseVolume, speedRatio);
                float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
                if (isAccelerating) targetPitch += 0.08f;

                engineAudioSource.volume = Mathf.MoveTowards(engineAudioSource.volume, targetVol, 1.2f * Time.deltaTime);
                engineAudioSource.pitch = Mathf.MoveTowards(engineAudioSource.pitch, targetPitch, 0.8f * Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            if (reservedIntersection != null)
            {
                reservedIntersection.ReleaseIntersection(this);
                reservedIntersection = null;
            }
        }

        #region Waypoint Management
        public void SetInitialWaypoint(TrafficWaypointNode waypoint)
        {
            currentWaypoint = waypoint;
            if (currentWaypoint != null)
            {
                // Align facing direction towards waypoint
                Vector3 toWp = (currentWaypoint.Position - transform.position);
                toWp.y = 0f;
                if (toWp.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(toWp.normalized, Vector3.up);
                }
            }
        }

        private void AdvanceWaypoint()
        {
            if (currentWaypoint == null) return;

            // Release any previously reserved intersection
            if (reservedIntersection != null && reservedIntersection != currentWaypoint)
            {
                reservedIntersection.ReleaseIntersection(this);
                reservedIntersection = null;
            }

            previousWaypoint = currentWaypoint;
            TrafficWaypointNode next = currentWaypoint != null ? currentWaypoint.GetRandomNextNode(previousWaypoint) : null;
            if (next == null && CityTrafficManager.Instance != null)
            {
                // Seamless route loop: find next nearest continuing waypoint on this car's assigned road network
                List<TrafficWaypointNode> candidates = (assignedRoadWaypoints != null && assignedRoadWaypoints.Count > 0)
                    ? assignedRoadWaypoints
                    : (CityTrafficManager.Instance.AllWaypoints as List<TrafficWaypointNode>);

                next = CityTrafficManager.Instance.FindAlternativeWaypoint(transform.position, transform.forward, previousWaypoint, null, 45f, candidates);
                if (next == null && candidates != null && candidates.Count > 0)
                {
                    next = CityTrafficManager.Instance.FindBestInitialWaypoint(transform, candidates);
                }
            }
            currentWaypoint = next;

            if (currentWaypoint != null && currentWaypoint.isIntersection)
            {
                reservedIntersection = currentWaypoint;
            }
        }
        #endregion

        #region Sensing & Obstacle Detection
        private void UpdateSensors()
        {
            if (isReversing)
            {
                // While reversing, forward sensors yield control to rear maneuver
                return;
            }

            Vector3 origin = transform.position + Vector3.up * sensorHeight + transform.forward * frontBumperOffset;
            Vector3 direction = transform.forward;

            int hitCount = Physics.SphereCastNonAlloc(origin, sensorRadius, direction, staticHitsBuffer, maxDetectionDistance, detectionLayerMask, QueryTriggerInteraction.Collide);

            float closestDistance = float.MaxValue;
            AICarController detectedCar = null;

            int playerLayer = LayerMask.NameToLayer("Player");
            float closestPlayerDist = float.MaxValue;
            GameObject detectedPlayer = null;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = staticHitsBuffer[i];
                if (hit.collider == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                // Check for Player (by tag "Player", layer "Player", or AIPedestrianController component)
                bool isPlayer = hit.collider.CompareTag("Player") ||
                                (playerLayer != -1 && hit.collider.gameObject.layer == playerLayer) ||
                                hit.collider.GetComponentInParent<AIPedestrianController>() != null;

                if (isPlayer)
                {
                    // Only yield if player is actually inside the driving lane path width
                    Vector3 toHit = (hit.point - transform.position);
                    toHit.y = 0f;
                    float lateralDist = Mathf.Abs(Vector3.Dot(toHit, transform.right));
                    if (lateralDist > 1.35f) continue; // On sidewalk/curb, NOT in driving lane!

                    float dist = hit.distance;
                    if (dist < closestPlayerDist)
                    {
                        closestPlayerDist = dist;
                        detectedPlayer = hit.collider.gameObject;
                    }
                }
                else
                {
                    AICarController other = hit.collider.GetComponentInParent<AICarController>();
                    if (other != null)
                    {
                        // Check if other car is generally in front
                        float dist = hit.distance;
                        if (dist < closestDistance)
                        {
                            closestDistance = dist;
                            detectedCar = other;
                        }
                    }
                }
            }

            // Close proximity check at bumper for standing/crossing player
            if (detectedPlayer == null)
            {
                int colCount = Physics.OverlapSphereNonAlloc(origin, sensorRadius + 0.4f, staticOverlapBuffer, detectionLayerMask, QueryTriggerInteraction.Collide);
                for (int c = 0; c < colCount; c++)
                {
                    var col = staticOverlapBuffer[c];
                    if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;
                    if (col.CompareTag("Player") || (playerLayer != -1 && col.gameObject.layer == playerLayer) || col.GetComponentInParent<AIPedestrianController>() != null)
                    {
                        Vector3 toCol = (col.transform.position - transform.position);
                        toCol.y = 0f;
                        float lat = Mathf.Abs(Vector3.Dot(toCol, transform.right));
                        if (lat > 1.35f) continue; // Outside car path

                        detectedPlayer = col.gameObject;
                        closestPlayerDist = Vector3.Distance(origin, col.transform.position);
                        break;
                    }
                }
            }

            playerAhead = (closestPlayerDist <= playerDetectionDistance) ? detectedPlayer : null;
            distanceToPlayerAhead = closestPlayerDist;

            // If player is in front of the vehicle, yield with patience timer
            if (playerAhead != null)
            {
                playerWaitTimer += Time.deltaTime;
                if (playerWaitTimer > 3.5f)
                {
                    // If blocked by player for > 3.5 seconds, gently creep past
                    if (EmpireBuilder.Audio.GameSoundManager.Instance != null && playerWaitTimer > 4.0f)
                    {
                        EmpireBuilder.Audio.GameSoundManager.Instance.PlayVehicleHorn(transform.position);
                    }
                    targetSpeed = 1.8f;
                    isBraking = false;
                    currentState = AICarState.Cruising;
                    isStoppedForPlayer = false;
                }
                else
                {
                    isStoppedForPlayer = true;
                    targetSpeed = 0f;
                    isBraking = true;
                    currentState = AICarState.Stopped;
                    blockedByCarTimer = 0f;
                    targetEvasionOffset = 0f;
                    carAhead = null;
                    return;
                }
            }
            else
            {
                playerWaitTimer = 0f;
                isStoppedForPlayer = false;
            }

            carAhead = detectedCar;
            distanceToCarAhead = closestDistance;

            if (carAhead != null)
            {
                // Accumulate blocked timer if car is stopped or heavily slowed by the vehicle ahead
                if (currentSpeed < 1.0f && distanceToCarAhead <= cautionDistance)
                {
                    blockedByCarTimer += Time.deltaTime;
                    if (blockedByCarTimer >= currentUnjamThreshold)
                    {
                        ExecuteTrafficClearanceManeuver();
                    }
                }
                else
                {
                    blockedByCarTimer = Mathf.Max(0f, blockedByCarTimer - Time.deltaTime);
                }

                // Check if car ahead is ONCOMING (facing towards us)
                float facingDot = Vector3.Dot(transform.forward, carAhead.transform.forward);
                bool isOncoming = (facingDot < -0.2f);
                isOncomingDetected = isOncoming;

                if (isOncoming)
                {
                    // Deterministic right-of-way resolution based on HashCode: breaks symmetry and avoids head-on freezing
                    bool hasRightOfWay = this.GetHashCode() > carAhead.GetHashCode();

                    // Both cars shift firmly to their respective right sides (standard driving convention)
                    targetEvasionOffset = rightSideEvasionOffset * 1.35f;

                    if (hasRightOfWay)
                    {
                        // Higher priority vehicle continues forward at safe passing speed
                        targetSpeed = Mathf.Max(4.5f, passingSpeed);
                        isBraking = false;
                        currentState = AICarState.PassingOncoming;
                    }
                    else
                    {
                        // Yielding vehicle shifts slightly further right and slows down to let oncoming car clear
                        targetEvasionOffset = rightSideEvasionOffset * 1.6f;
                        if (distanceToCarAhead <= fullStopDistance * 0.85f)
                        {
                            targetSpeed = 0f;
                            isBraking = true;
                            currentState = AICarState.Stopped;
                        }
                        else
                        {
                            targetSpeed = 2.5f;
                            isBraking = (currentSpeed > targetSpeed);
                            currentState = AICarState.PassingOncoming;
                        }
                    }
                }
                else
                {
                    // Following behind a car in the same lane
                    if (carAhead.CurrentSpeed < 1.0f && distanceToCarAhead <= cautionDistance)
                    {
                        // Check if we can overtake on the left or bypass on the right
                        bool leftClear = CheckLateralClearance(-transform.right, maxLateralBypassOffset);
                        bool rightClear = CheckLateralClearance(transform.right, maxLateralBypassOffset);

                        if (leftClear)
                        {
                            targetEvasionOffset = -maxLateralBypassOffset;
                        }
                        else if (rightClear)
                        {
                            targetEvasionOffset = maxLateralBypassOffset;
                        }
                        else
                        {
                            targetEvasionOffset = rightSideEvasionOffset * 0.5f;
                        }

                        // If side offset has given clearance, allow overtaking creep speed
                        if (Mathf.Abs(currentEvasionOffset) > 1.4f && distanceToCarAhead > fullStopDistance * 0.65f)
                        {
                            targetSpeed = 4.5f;
                            isBraking = false;
                            currentState = AICarState.BypassingLateral;
                        }
                        else if (distanceToCarAhead <= fullStopDistance)
                        {
                            targetSpeed = 0f;
                            isBraking = true;
                            currentState = AICarState.Stopped;
                        }
                        else
                        {
                            targetSpeed = 2.0f;
                            isBraking = (currentSpeed > targetSpeed);
                            currentState = AICarState.Braking;
                        }
                    }
                    else
                    {
                        targetEvasionOffset = 0f;
                        if (distanceToCarAhead <= fullStopDistance)
                        {
                            targetSpeed = 0f;
                            isBraking = true;
                            currentState = AICarState.Stopped;
                        }
                        else if (distanceToCarAhead <= cautionDistance)
                        {
                            float t = (distanceToCarAhead - fullStopDistance) / Mathf.Max(0.01f, (cautionDistance - fullStopDistance));
                            float matchedSpeed = Mathf.Min(cruisingSpeed, carAhead.CurrentSpeed);
                            targetSpeed = Mathf.Lerp(0.5f, matchedSpeed, t);
                            isBraking = (currentSpeed > targetSpeed);
                            currentState = isBraking ? AICarState.Braking : AICarState.Following;
                        }
                        else
                        {
                            targetSpeed = cruisingSpeed * 0.85f;
                            isBraking = false;
                            currentState = AICarState.Cruising;
                        }
                    }
                }
            }
            else
            {
                // Road is clear ahead
                blockedByCarTimer = 0f;
                isOncomingDetected = false;
                targetEvasionOffset = 0f;
                targetSpeed = cruisingSpeed;
                isBraking = false;
                currentState = (currentSpeed < cruisingSpeed * 0.95f) ? AICarState.Accelerating : AICarState.Cruising;
            }

            // Smoothly shift laterally towards road right side or back to center
            float shiftRate = (currentState == AICarState.BypassingLateral) ? clearanceShiftSpeed : evasionShiftSpeed;
            currentEvasionOffset = Mathf.MoveTowards(currentEvasionOffset, targetEvasionOffset, shiftRate * Time.deltaTime);
        }

        private void UpdateIntersectionYielding()
        {
            if (isReversing) return;
            if (currentWaypoint == null || !currentWaypoint.isIntersection)
            {
                intersectionWaitTimer = 0f;
                return;
            }

            float distToIntersection = Vector3.Distance(transform.position, currentWaypoint.Position);
            if (distToIntersection <= currentWaypoint.intersectionRadius + 2.0f)
            {
                bool canEnter = currentWaypoint.TryReserveIntersection(this);
                if (!canEnter)
                {
                    intersectionWaitTimer += Time.deltaTime;
                    if (intersectionWaitTimer > 2.0f)
                    {
                        // Yield timeout expired: cautiously cross to prevent city-wide gridlock
                        targetSpeed = 3.5f;
                        isBraking = false;
                        currentState = AICarState.Turning;
                    }
                    else
                    {
                        targetSpeed = 0f;
                        isBraking = true;
                        currentState = AICarState.YieldingIntersection;
                    }
                }
                else
                {
                    intersectionWaitTimer = 0f;
                }
            }
            else
            {
                intersectionWaitTimer = 0f;
            }
        }
        #endregion

        #region Speed & Movement
        private void UpdateSpeed()
        {
            if (isStoppedForPlayer)
            {
              //  currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * 3.0f * Time.deltaTime);
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, emergencyBrakeRate * Time.deltaTime);
                return;
            }

            if (isReversing)
            {
                // Smoothly accelerate backward in reverse gear
                currentSpeed = Mathf.MoveTowards(currentSpeed, -reverseSpeed, emergencyBrakeRate * Time.deltaTime);
                return;
            }

            // Respect waypoint specific speed limit if set
            if (currentWaypoint != null && currentWaypoint.speedLimit > 0f)
            {
                targetSpeed = Mathf.Min(targetSpeed, currentWaypoint.speedLimit);
            }

            // Cornering slowdown
            if (currentWaypoint != null)
            {
                Vector3 toWp = (currentWaypoint.Position - transform.position);
                toWp.y = 0f;
                if (toWp.sqrMagnitude > 0.01f)
                {
                    float angle = Vector3.Angle(transform.forward, toWp.normalized);
                    if (angle > 30f)
                    {
                        float cornerSpeed = Mathf.Lerp(cruisingSpeed, 4.5f, (angle - 30f) / 60f);
                        targetSpeed = Mathf.Min(targetSpeed, cornerSpeed);
                        if (currentState != AICarState.Stopped && currentState != AICarState.YieldingIntersection && currentState != AICarState.BypassingLateral)
                        {
                            currentState = AICarState.Turning;
                        }
                    }
                }
            }

            // Apply acceleration or braking
            if (isBraking || currentSpeed > targetSpeed)
            {
                float rate = (targetSpeed <= 0.1f && distanceToCarAhead <= fullStopDistance + 0.5f) ? emergencyBrakeRate : brakingRate;
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);
            }
        }

        private void UpdateSteeringAndMovement()
        {
            if (isReversing)
            {
                // Move backward in -transform.forward
                Vector3 reverseMovement = -transform.forward * (Mathf.Abs(currentSpeed) * Time.deltaTime);
                transform.position += reverseMovement;

                // Rotate body in reverse steer direction to angle out of the deadlock
                transform.Rotate(Vector3.up, reverseSteerDirection * 34f * Time.deltaTime);

                Vector3 clampedPos = transform.position;
                clampedPos.y = streetLevelY;
                transform.position = clampedPos;

                // Check behind for other cars/obstacles
                Vector3 rearOrigin = transform.position + Vector3.up * sensorHeight - transform.forward * frontBumperOffset;
                if (Physics.SphereCast(rearOrigin, sensorRadius, -transform.forward, out RaycastHit rearHit, 2.2f, detectionLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (rearHit.collider != null && rearHit.collider.GetComponentInParent<AICarController>() != null)
                    {
                        FinishReverseAndChangeDirection();
                        return;
                    }
                }

                reverseTimer -= Time.deltaTime;
                if (reverseTimer <= 0f)
                {
                    FinishReverseAndChangeDirection();
                }
                return;
            }

            if (currentWaypoint == null) return;

            Vector3 baseTargetPos = currentWaypoint.Position;
            baseTargetPos.y = streetLevelY;

            Vector3 toBaseTarget = (baseTargetPos - transform.position);
            toBaseTarget.y = 0f;

            // Calculate the road right normal vector: Vector3.Cross(Vector3.up, toBaseTarget.normalized)
            // This accurately shifts the steering target towards the right side of the road
            Vector3 roadRight = (toBaseTarget.sqrMagnitude > 0.01f)
                ? Vector3.Cross(Vector3.up, toBaseTarget.normalized)
                : transform.right;

            Vector3 adjustedTarget = baseTargetPos + roadRight * currentEvasionOffset;

            Vector3 toAdjusted = (adjustedTarget - transform.position);
            toAdjusted.y = 0f;
            float distance = toAdjusted.magnitude;

            if (distance > 0.05f && !isStoppedForPlayer)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toAdjusted.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, steeringSpeed * Time.deltaTime);
            }

            // Move forward
            if (currentSpeed > 0.001f)
            {
                transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                Vector3 clampedPos = transform.position;
                clampedPos.y = streetLevelY;
                transform.position = clampedPos;
            }

            // Waypoint arrival check against base lane waypoint and adjusted target
            float distToBaseWaypoint = toBaseTarget.magnitude;
            float distToAdjustedTarget = toAdjusted.magnitude;

            // Check if car reached waypoint, reached adjusted target, or passed the waypoint plane
            bool hasPassedWaypoint = Vector3.Dot(toBaseTarget, transform.forward) < 0f && distToBaseWaypoint < 8.0f;
            bool arrivedAtAdjusted = distToAdjustedTarget <= waypointArrivalDistance;
            bool arrivedAtBase = distToBaseWaypoint <= Mathf.Max(3.2f, Mathf.Abs(currentEvasionOffset) + 0.8f);

            if (arrivedAtAdjusted || arrivedAtBase || hasPassedWaypoint)
            {
                // Smoothly pull evasion offset back towards lane center when reaching a waypoint
                currentEvasionOffset = Mathf.MoveTowards(currentEvasionOffset, 0f, 2.5f);
                targetEvasionOffset = 0f;
                AdvanceWaypoint();
            }
        }

        private void UpdateWheels()
        {
            if (wheels == null || wheels.Count == 0) return;
            if (wheelRadius <= 0.05f) wheelRadius = 0.35f;

            // Calculate rotation delta based on speed and time (supports reverse when currentSpeed < 0)
            float rotationDelta = (currentSpeed * Time.deltaTime / wheelRadius) * Mathf.Rad2Deg;

            // Calculate steering angle for front wheels when turning or reversing
            float targetSteerAngle = 0f;
            if (isReversing)
            {
                targetSteerAngle = -reverseSteerDirection * 25f;
            }
            else if (currentWaypoint != null && Mathf.Abs(currentSpeed) > 0.1f)
            {
                Vector3 toWp = (currentWaypoint.Position - transform.position);
                toWp.y = 0f;
                if (toWp.sqrMagnitude > 0.01f)
                {
                    float signedAngle = Vector3.SignedAngle(transform.forward, toWp.normalized, Vector3.up);
                    targetSteerAngle = Mathf.Clamp(signedAngle, -28f, 28f);
                }
            }
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle, steeringSpeed * 1.5f * Time.deltaTime);

            for (int i = 0; i < wheels.Count; i++)
            {
                var w = wheels[i];
                if (w.transform == null) continue;

                // Advance wheel roll angle
                w.currentAngle = (w.currentAngle + rotationDelta) % 360f;

                // CRITICAL: Lock local position to initialLocalPos every frame!
                // This guarantees tires can NEVER split or drift from the vehicle!
                w.transform.localPosition = w.initialLocalPos;

                // Clean rotation around local X axis
                Quaternion rollRot = Quaternion.Euler(w.currentAngle, 0f, 0f);

                if (w.isFrontWheel)
                {
                    Quaternion steerRot = Quaternion.Euler(0f, currentSteerAngle, 0f);
                    w.transform.localRotation = w.initialLocalRot * steerRot * rollRot;
                }
                else
                {
                    w.transform.localRotation = w.initialLocalRot * rollRot;
                }
            }
        }

        private void UpdateWatchdog()
        {
            // If car has been motionless for too long, execute progressive unjam maneuvers to clear gridlock
            if (Mathf.Abs(currentSpeed) < 0.25f && !isReversing)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= 1.5f && stuckTimer < 3.0f)
                {
                    ExecuteTrafficClearanceManeuver();
                }
                else if (stuckTimer >= 3.0f && stuckTimer < 4.8f)
                {
                    // Proactive bypass: advance waypoint, shift laterally, and accelerate forward
                    AdvanceWaypoint();
                    targetEvasionOffset = (Random.value > 0.5f ? maxLateralBypassOffset : -maxLateralBypassOffset);
                    currentSpeed = cruisingSpeed * 0.55f;
                    isBraking = false;
                    currentState = AICarState.Cruising;
                }
                else if (stuckTimer >= 4.8f)
                {
                    // Full recovery: snap back cleanly to nearest road waypoint in lane
                    RecoverToNearestLaneWaypoint();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = Mathf.Max(0f, stuckTimer - Time.deltaTime);
            }
        }

        private void RecoverToNearestLaneWaypoint()
        {
            TrafficWaypointNode recoveryNode = null;
            if (assignedRoadWaypoints != null && assignedRoadWaypoints.Count > 0)
            {
                float minDist = float.MaxValue;
                foreach (var wp in assignedRoadWaypoints)
                {
                    if (wp == null) continue;
                    float d = Vector3.Distance(transform.position, wp.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        recoveryNode = wp;
                    }
                }
            }

            if (recoveryNode == null && CityTrafficManager.Instance != null && CityTrafficManager.Instance.AllWaypoints != null)
            {
                float minDist = float.MaxValue;
                foreach (var wp in CityTrafficManager.Instance.AllWaypoints)
                {
                    if (wp == null) continue;
                    float d = Vector3.Distance(transform.position, wp.Position);
                    if (d < minDist)
                    {
                        minDist = d;
                        recoveryNode = wp;
                    }
                }
            }

            if (recoveryNode != null)
            {
                Vector3 targetPos = recoveryNode.Position;
                targetPos.y = streetLevelY;
                transform.position = targetPos;

                TrafficWaypointNode nextNode = recoveryNode.GetRandomNextNode();
                if (nextNode != null)
                {
                    Vector3 fwd = (nextNode.Position - targetPos);
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                    }
                    currentWaypoint = nextNode;
                }
                else
                {
                    currentWaypoint = recoveryNode;
                }

                currentEvasionOffset = 0f;
                targetEvasionOffset = 0f;
                currentSpeed = cruisingSpeed * 0.5f;
                isReversing = false;
                carAhead = null;
                playerAhead = null;
                isBraking = false;
                currentState = AICarState.Cruising;
            }
        }
        #endregion

        #region Traffic Clearance & Unjam Maneuvers
        /// <summary>
        /// Initiates a traffic clearance maneuver when blocked by another vehicle:
        /// 1. Tries to steer left or right to bypass around the vehicle.
        /// 2. If lateral clearance is blocked or deadlock persists, drives backward (reverses) with evasive steering.
        /// 3. Changes direction onto an alternate road branch or nearby waypoint so traffic clears immediately.
        /// </summary>
        public void ExecuteTrafficClearanceManeuver()
        {
            if (isReversing || isStoppedForPlayer || playerAhead != null) return;

            // Check if left or right is open to bypass
            bool rightClear = CheckLateralClearance(transform.right, maxLateralBypassOffset);
            bool leftClear = CheckLateralClearance(-transform.right, maxLateralBypassOffset);

            // If we have lateral room and distance is not completely zero, try shifting left or right first
            if (currentState != AICarState.BypassingLateral && distanceToCarAhead > fullStopDistance * 0.8f && (rightClear || leftClear))
            {
                targetEvasionOffset = rightClear ? maxLateralBypassOffset : -maxLateralBypassOffset;
                currentState = AICarState.BypassingLateral;
                targetSpeed = 4.5f;
                isBraking = false;
                blockedByCarTimer = 0f;
                return;
            }

            // Otherwise engage Reverse maneuver
            StartReverseManeuver();
        }

        private void StartReverseManeuver()
        {
            if (isReversing) return;

            isReversing = true;
            currentState = AICarState.Reversing;
            reverseDuration = Random.Range(1.8f, 2.8f);
            reverseTimer = reverseDuration;
            targetSpeed = -reverseSpeed;

            // Decide reverse steering direction:
            // Angle the tail towards the lane and the nose towards the open side of the road
            if (Mathf.Abs(currentEvasionOffset) > 0.4f)
            {
                reverseSteerDirection = (currentEvasionOffset > 0f) ? -1f : 1f;
            }
            else
            {
                bool rightOpen = CheckLateralClearance(transform.right, 2.0f);
                reverseSteerDirection = rightOpen ? 1f : -1f;
            }
        }

        private void FinishReverseAndChangeDirection()
        {
            isReversing = false;
            reverseTimer = 0f;
            blockedByCarTimer = 0f;
            currentUnjamThreshold = Random.Range(1.4f, 2.4f);
            carAhead = null;
            isBraking = false;

            // Release any intersection lock
            if (reservedIntersection != null)
            {
                reservedIntersection.ReleaseIntersection(this);
                reservedIntersection = null;
            }

            // Pick an alternative direction:
            bool foundBranch = false;

            // 1. Try to find a diverging branch from currentWaypoint
            if (currentWaypoint != null && currentWaypoint.nextNodes != null && currentWaypoint.nextNodes.Count > 1)
            {
                TrafficWaypointNode bestNode = null;
                float bestScore = -999f;
                foreach (var node in currentWaypoint.nextNodes)
                {
                    if (node == null) continue;
                    Vector3 toNode = (node.Position - transform.position).normalized;
                    float forwardDot = Vector3.Dot(transform.forward, toNode);
                    // Prefer branches that turn away from straight forward (forwardDot < 0.6)
                    float score = 1.0f - forwardDot;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestNode = node;
                    }
                }
                if (bestNode != null)
                {
                    currentWaypoint = bestNode;
                    foundBranch = true;
                }
            }

            // 2. Query CityTrafficManager for an alternative road waypoint nearby on assigned road network
            if (!foundBranch && CityTrafficManager.Instance != null)
            {
                List<TrafficWaypointNode> candidates = (assignedRoadWaypoints != null && assignedRoadWaypoints.Count > 0)
                    ? assignedRoadWaypoints
                    : (CityTrafficManager.Instance.AllWaypoints as List<TrafficWaypointNode>);

                TrafficWaypointNode altNode = CityTrafficManager.Instance.FindAlternativeWaypoint(
                    transform.position,
                    transform.forward,
                    currentWaypoint,
                    previousWaypoint,
                    35f,
                    candidates
                );
                if (altNode != null)
                {
                    previousWaypoint = currentWaypoint;
                    currentWaypoint = altNode;
                    foundBranch = true;
                }
            }

            // 3. Fallback: If on a single-lane road without branches, turn around towards previous waypoint
            if (!foundBranch)
            {
                if (previousWaypoint != null && Vector3.Distance(transform.position, previousWaypoint.Position) > 3.0f)
                {
                    TrafficWaypointNode temp = currentWaypoint;
                    currentWaypoint = previousWaypoint;
                    previousWaypoint = temp;
                }
                else
                {
                    AdvanceWaypoint();
                }
            }

            // Align rotation smoothly towards the newly selected direction
            if (currentWaypoint != null)
            {
                Vector3 toWp = (currentWaypoint.Position - transform.position);
                toWp.y = 0f;
                if (toWp.sqrMagnitude > 0.05f)
                {
                    transform.rotation = Quaternion.LookRotation(toWp.normalized, Vector3.up);
                }
            }

            // Reset lateral offsets so the car drives cleanly in the new direction
            currentEvasionOffset = 0f;
            targetEvasionOffset = 0f;
            targetSpeed = cruisingSpeed;
            currentState = AICarState.Accelerating;
        }

        private bool CheckLateralClearance(Vector3 lateralDir, float distance)
        {
            Vector3 origin = transform.position + Vector3.up * sensorHeight;
            // Lateral ray to check if side is blocked by wall/car/obstacle
            if (Physics.SphereCast(origin, 0.6f, lateralDir, out RaycastHit hit, distance, detectionLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            // Forward ray from the offset position to verify clearance forward in that lane
            Vector3 offsetOrigin = origin + lateralDir * distance;
            if (Physics.SphereCast(offsetOrigin, 0.6f, transform.forward, out RaycastHit fwdHit, cautionDistance * 0.7f, detectionLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (fwdHit.collider != null && fwdHit.transform != transform && !fwdHit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region Helpers
        private void InitializeWheels()
        {
            wheels.Clear();
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child == transform) continue;
                string n = child.name.ToLower();
                if (n.Contains("wheel"))
                {
                    // Avoid nested duplicate transforms under a wheel
                    if (child.parent != transform && child.parent.name.ToLower().Contains("wheel"))
                    {
                        continue;
                    }

                    WheelInfo w = new WheelInfo
                    {
                        transform = child,
                        initialLocalPos = child.localPosition,
                        initialLocalRot = child.localRotation,
                        currentAngle = 0f,
                        isFrontWheel = (child.localPosition.z > 0f) || n.Contains("front")
                    };
                    wheels.Add(w);
                }
            }
        }

        private void EnsureCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.75f, 0f);
                box.size = new Vector3(2.0f, 1.5f, 4.2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * sensorHeight + transform.forward * frontBumperOffset;
            Gizmos.color = isBraking ? Color.red : Color.green;
            Gizmos.DrawWireSphere(origin, sensorRadius);
            Gizmos.DrawRay(origin, transform.forward * maxDetectionDistance);

            if (currentWaypoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentWaypoint.Position);
            }
        }
        #endregion
    }
}
