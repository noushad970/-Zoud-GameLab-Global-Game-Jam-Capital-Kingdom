using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    public enum PedestrianGender
    {
        Male,
        Female
    }

    public enum PedestrianState
    {
        Walking,
        Idle,
        Gathering,
        AvoidingObstacle,
        YieldingToCar
    }

    /// <summary>
    /// Autonomous pedestrian AI character that walks along roadsides and sidewalks,
    /// avoids obstacles and moving vehicles, never walks in the middle of roads,
    /// pauses for idle moments, and gathers realistically at cafes, markets, schools, and stations.
    /// Tagged and layered as "Player" for vehicle detection and pedestrian safety.
    /// </summary>
    public class AIPedestrianController : MonoBehaviour
    {
        [Header("Character Identity")]
        [SerializeField] private PedestrianGender gender = PedestrianGender.Male;
        [SerializeField] private string characterName = "Citizen";

        [Header("Locomotion Settings")]
        [SerializeField] private float walkSpeed = 1.6f;
        [SerializeField] private float turnSpeed = 240f;
        [SerializeField] private float arrivalThreshold = 0.85f;

        [Header("Idle & Social Timing")]
        [SerializeField] private float idleChance = 0f;
        [SerializeField] private float minWalkDuration = 14f;
        [SerializeField] private float maxWalkDuration = 28f;
        [SerializeField] private float minIdleDuration = 0f;
        [SerializeField] private float maxIdleDuration = 0f;
        [SerializeField] private float hotspotGatherDuration = 0f;

        [Header("Obstacle & Road Avoidance")]
        [SerializeField] private float obstacleDetectionDistance = 2.2f;
        [SerializeField] private float personalSpaceRadius = 0.45f;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        private float steeringRaycastTimer = 0f;
        private Vector3 cachedSteeringResult = Vector3.forward;
        [SerializeField] private float roadAvoidanceDistance = 2.4f;

        [Header("Current Runtime State")]
        [SerializeField] private PedestrianState currentState = PedestrianState.Walking;
        [SerializeField] private Vector3 currentTargetPosition;
        [SerializeField] private bool isGatheringAtHotspot = false;
        [SerializeField] private string currentHotspotName = "";

        [Header("Waypoint Node Navigation")]
        [Tooltip("The current waypoint node this character is walking towards. Link nextNodes on the waypoint in Inspector to define walking routes.")]
        [SerializeField] private PedestrianWaypointNode currentWaypointNode;
        [SerializeField] private PedestrianWaypointNode previousWaypointNode;

        public PedestrianWaypointNode CurrentWaypointNode => currentWaypointNode;
        public PedestrianWaypointNode PreviousWaypointNode => previousWaypointNode;

        public void SetCurrentWaypointNode(PedestrianWaypointNode node)
        {
            currentWaypointNode = node;
            if (node != null)
            {
                currentTargetPosition = node.Position;
                if (node.customSpeed > 0f)
                {
                    walkSpeed = node.customSpeed;
                }
            }
        }

        // Animation components & state
        private Animator animator;
        private int activeWalkHash;
        private int activeIdleHash;
        private int isWalkingParamHash;
        private bool hasIsWalkingParam = false;
        private bool? lastWalkingState = null;

        // Navigation
        private List<Vector3> currentPath = new List<Vector3>();
        private int pathIndex = 0;
        private float stateTimer = 0f;
        private float walkCycleTimer = 0f;
        private AIPedestrianController socialPartner = null;
        private CharacterController characterController;
        private CapsuleCollider capsuleCollider;
        private Vector3 lastStuckCheckPosition;
        private float stuckCheckTimer = 0f;
        private int stuckCounter = 0;

        public PedestrianGender Gender => gender;
        public PedestrianState CurrentState => currentState;
        public bool IsGathering => isGatheringAtHotspot;
        public string CurrentHotspotName => currentHotspotName;

        public void Initialize(PedestrianGender g, string name = "Citizen", float speed = 1.6f)
        {
            gender = g;
            characterName = name;
            walkSpeed = speed;
            SetPlayerTagAndLayer();
            SetupAnimator();
        }

        private void Awake()
        {
            SetPlayerTagAndLayer();
            SetupPhysicsComponents();
            SetupAnimator();

            // Randomize walking speed slightly for natural crowd cadence
            walkSpeed = Random.Range(1.4f, 2.1f);
            walkCycleTimer = Random.Range(minWalkDuration * 0.5f, maxWalkDuration);
        }

        private void Start()
        {
            SetPlayerTagAndLayer();

            if (currentWaypointNode == null && CityPedestrianManager.Instance != null)
            {
                var closest = CityPedestrianManager.Instance.FindClosestCharacterWaypointNode(transform.position);
                if (closest != null)
                {
                    SetCurrentWaypointNode(closest);
                }
            }

            if (currentWaypointNode == null && currentPath.Count == 0 && CityPedestrianManager.Instance != null)
            {
                RequestNewSidewalkPath();
            }

            // Sync initial animation state
            lastWalkingState = null;
            UpdateAnimation(currentState == PedestrianState.Walking);
        }

        /// <summary>
        /// Enforces Player tag and Player layer recursively across this character and all child parts.
        /// </summary>
        public void SetPlayerTagAndLayer()
        {
            gameObject.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                gameObject.layer = playerLayer;
            }

            // Also set recursively on all children
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                allChildren[i].gameObject.tag = "Player";
                if (playerLayer != -1)
                {
                    allChildren[i].gameObject.layer = playerLayer;
                }
            }
        }

        private void SetupPhysicsComponents()
        {
            // CharacterController has its own built-in capsule collider.
            // If a separate CapsuleCollider is on the same GameObject, PhysX self-collision blocks CharacterController.Move().
            capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            if (capsuleCollider != null)
            {
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.center = new Vector3(0f, 0.85f, 0f);
                capsuleCollider.radius = 0.28f;
                capsuleCollider.height = 1.7f;
                Destroy(capsuleCollider);
                capsuleCollider = null;
            }

            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.center = new Vector3(0f, 0.85f, 0f);
                characterController.radius = 0.28f;
                characterController.height = 1.7f;
                characterController.stepOffset = 0.35f;
                characterController.slopeLimit = 45f;
            }
            characterController.center = new Vector3(0f, 0.85f, 0f);
            characterController.radius = 0.28f;
            characterController.height = 1.7f;
            characterController.stepOffset = 0.35f;
            characterController.slopeLimit = 45f;
            characterController.minMoveDistance = 0f;
        }

        private void SetupAnimator()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) return;

            // Auto-assign AnimatorController if missing from the model
            RuntimeAnimatorController targetController = animator.runtimeAnimatorController;

            if (targetController == null && CityPedestrianManager.Instance != null)
            {
                targetController = gender == PedestrianGender.Female
                    ? CityPedestrianManager.Instance.FemaleController
                    : CityPedestrianManager.Instance.MaleController;

                animator.runtimeAnimatorController = targetController;
            }

#if UNITY_EDITOR
            if (targetController == null)
            {
                string ctrlPath = gender == PedestrianGender.Female
                    ? "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Female Anim Controller.controller"
                    : "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Male Anim Controller.controller";

                targetController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                animator.runtimeAnimatorController = targetController;
            }
#endif

            if (targetController != null && animator.runtimeAnimatorController != targetController)
            {
                animator.runtimeAnimatorController = targetController;
                animator.Rebind();
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Discover active walk state name/hash
            activeWalkHash = Animator.StringToHash("Walk");

            int[] possibleWalks =
            {
                Animator.StringToHash("Walk"),
                Animator.StringToHash("walk"),
                Animator.StringToHash("m_walk"),
                Animator.StringToHash("f_walk")
            };

            for (int i = 0; i < possibleWalks.Length; i++)
            {
                if (animator.HasState(0, possibleWalks[i]))
                {
                    activeWalkHash = possibleWalks[i];
                    break;
                }
            }

            // Check if isWalking parameter exists in controller
            hasIsWalkingParam = false;

            if (animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.name.Equals("isWalking", System.StringComparison.OrdinalIgnoreCase) ||
                        p.name.Equals("walk", System.StringComparison.OrdinalIgnoreCase))
                    {
                        isWalkingParamHash = p.nameHash;
                        hasIsWalkingParam = true;
                        break;
                    }
                }
            }

            // Discover active idle state name/hash
            // Fallback state names
            activeWalkHash = Animator.StringToHash("Walk");
            activeIdleHash = Animator.StringToHash("Idle");
            int[] possibleIdles = new int[]
            {
                Animator.StringToHash("Idle"),
                Animator.StringToHash("idle"),
                Animator.StringToHash("m_idle_A"),
                Animator.StringToHash("f_idle_A"),
                Animator.StringToHash("m_idle"),
                Animator.StringToHash("f_idle")
            };
            for (int i = 0; i < possibleIdles.Length; i++)
            {
                if (animator.HasState(0, possibleIdles[i]))
                {
                    activeIdleHash = possibleIdles[i];
                    break;
                }
            }

            // Discover isWalking parameter
            isWalkingParamHash = Animator.StringToHash("isWalking");
            hasIsWalkingParam = false;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == isWalkingParamHash || p.name.ToLower() == "iswalking" || p.name.ToLower() == "walk")
                {
                    isWalkingParamHash = p.nameHash;
                    hasIsWalkingParam = true;
                    break;
                }
            }

            lastWalkingState = null;
            UpdateAnimation(currentState == PedestrianState.Walking);
        }

        private void Update()
        {
            // Unstoppable Citizens: continuously maintain Walking locomotion
            currentState = PedestrianState.Walking;
            UpdateWalking();
        }

        private void UpdateWalking()
        {
            UpdateAnimation(true);

            // Unstoppable citizens: walkCycleTimer cycles without idle pauses
            walkCycleTimer -= Time.deltaTime;
            if (walkCycleTimer <= 0f)
            {
                walkCycleTimer = Random.Range(minWalkDuration, maxWalkDuration);
            }

            // Rapid stuck detection & continuous walking recovery watchdog
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= 1.2f)
            {
                stuckCheckTimer = 0f;
                float distMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);
                lastStuckCheckPosition = transform.position;

                if (distMoved < 0.22f)
                {
                    stuckCounter++;
                    if (stuckCounter >= 1)
                    {
                        stuckCounter = 0;
                        if (currentWaypointNode != null)
                        {
                            var nextNode = currentWaypointNode.GetRandomNextNode(previousWaypointNode);
                            if (nextNode == null && CityPedestrianManager.Instance != null)
                            {
                                nextNode = CityPedestrianManager.Instance.FindNextAvailableWaypointNode(currentWaypointNode, previousWaypointNode);
                            }

                            if (nextNode != null)
                            {
                                previousWaypointNode = currentWaypointNode;
                                currentWaypointNode = nextNode;
                                currentTargetPosition = nextNode.Position;
                            }
                            else if (previousWaypointNode != null)
                            {
                                var swap = currentWaypointNode;
                                currentWaypointNode = previousWaypointNode;
                                previousWaypointNode = swap;
                                currentTargetPosition = currentWaypointNode.Position;
                            }
                            else if (CityPedestrianManager.Instance != null)
                            {
                                currentWaypointNode = CityPedestrianManager.Instance.FindClosestCharacterWaypointNode(transform.position);
                                if (currentWaypointNode != null) currentTargetPosition = currentWaypointNode.Position;
                            }
                        }
                        else if (CityPedestrianManager.Instance != null)
                        {
                            currentWaypointNode = CityPedestrianManager.Instance.FindClosestCharacterWaypointNode(transform.position);
                            if (currentWaypointNode != null) currentTargetPosition = currentWaypointNode.Position;
                        }

                        // Immediate physics unstick nudge
                        Vector3 nudge = (transform.forward + transform.right * Random.Range(-0.8f, 0.8f)).normalized * 0.45f;
                        if (characterController != null && characterController.enabled)
                        {
                            characterController.Move(nudge);
                        }
                    }
                }
                else
                {
                    stuckCounter = 0;
                }
            }

            // --- Waypoint Node Navigation ---
            if (currentWaypointNode != null)
            {
                currentTargetPosition = currentWaypointNode.Position;
                Vector3 toNodeTarget = currentTargetPosition - transform.position;
                toNodeTarget.y = 0f;
                float nodeDist = toNodeTarget.magnitude;

                bool passedWaypoint = Vector3.Dot(toNodeTarget, transform.forward) < 0f && nodeDist < 1.4f;

                if (nodeDist <= arrivalThreshold || passedWaypoint)
                {
                    // Pick next connected waypoint node (unstoppable: no pauses)
                    PedestrianWaypointNode next = currentWaypointNode.GetRandomNextNode(previousWaypointNode);
                    if (next == null && CityPedestrianManager.Instance != null)
                    {
                        next = CityPedestrianManager.Instance.FindNextAvailableWaypointNode(currentWaypointNode, previousWaypointNode);
                    }

                    if (next != null)
                    {
                        previousWaypointNode = currentWaypointNode;
                        currentWaypointNode = next;
                        currentTargetPosition = currentWaypointNode.Position;
                        if (currentWaypointNode.customSpeed > 0f)
                        {
                            walkSpeed = currentWaypointNode.customSpeed;
                        }
                    }
                    else if (previousWaypointNode != null)
                    {
                        // Reverse route back to previous node
                        var swap = currentWaypointNode;
                        currentWaypointNode = previousWaypointNode;
                        previousWaypointNode = swap;
                        currentTargetPosition = currentWaypointNode.Position;
                    }
                    else if (CityPedestrianManager.Instance != null)
                    {
                        // Look up another closest node if orphaned
                        currentWaypointNode = CityPedestrianManager.Instance.FindClosestCharacterWaypointNode(transform.position);
                        if (currentWaypointNode != null)
                        {
                            currentTargetPosition = currentWaypointNode.Position;
                        }
                    }

                    toNodeTarget = currentTargetPosition - transform.position;
                    toNodeTarget.y = 0f;
                }

                Vector3 moveDirNode = toNodeTarget.sqrMagnitude > 0.001f ? toNodeTarget.normalized : transform.forward;
                Vector3 steerDirNode = CalculateSteering(moveDirNode);

                if (steerDirNode == Vector3.zero)
                {
                    steerDirNode = (transform.forward + transform.right * 0.5f).normalized;
                }

                if (steerDirNode.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(steerDirNode, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                }

                Vector3 velocityNode = steerDirNode * walkSpeed;
                velocityNode.y = (characterController != null && characterController.isGrounded) ? -2.0f : -9.81f;

                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(velocityNode * Time.deltaTime);
                }
                else
                {
                    transform.position += velocityNode * Time.deltaTime;
                }
                return;
            }

            // --- Fallback Vector Path Navigation ---
            if (currentPath == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                RequestNewSidewalkPath();
                return;
            }

            currentTargetPosition = currentPath[pathIndex];
            Vector3 toTarget = currentTargetPosition - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance <= arrivalThreshold)
            {
                pathIndex++;
                if (pathIndex >= currentPath.Count)
                {
                    RequestNewSidewalkPath();
                    return;
                }
                currentTargetPosition = currentPath[pathIndex];
                toTarget = currentTargetPosition - transform.position;
                toTarget.y = 0f;
            }

            Vector3 moveDir = toTarget.normalized;
            Vector3 steerDir = CalculateSteering(moveDir);

            if (steerDir == Vector3.zero)
            {
                steerDir = (transform.forward + transform.right * 0.5f).normalized;
            }

            // Smooth rotation
            if (steerDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(steerDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }

            // Move forward
            // Move forward with grounded gravity
            Vector3 velocity = steerDir * walkSpeed;
            velocity.y = -9.81f * Time.deltaTime; // Gravity
            velocity.y = (characterController != null && characterController.isGrounded) ? -2.0f : -9.81f;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(velocity * Time.deltaTime);
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }

        private Vector3 CalculateSteering(Vector3 desiredMoveDir)
        {
            // Rate-limit physics raycasting across all pedestrians to eliminate CPU spikes on mobile
            steeringRaycastTimer -= Time.deltaTime;
            if (steeringRaycastTimer > 0f)
            {
                return Vector3.Slerp(cachedSteeringResult, desiredMoveDir, 0.35f);
            }
            steeringRaycastTimer = Random.Range(0.09f, 0.15f);

            Vector3 steering = desiredMoveDir;

            // 1. Raycast / Spherecast forward for obstacles (cars, pedestrians, props)
            Vector3 rayOrigin = transform.position
                    + Vector3.up * 0.75f
                    + transform.forward * 0.35f;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            RaycastHit hit;
            bool centerBlocked = Physics.SphereCast(
                rayOrigin,
                0.26f,
                forward,
                out hit,
                obstacleDetectionDistance,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            if (centerBlocked &&
                (hit.collider == null ||
                 hit.transform == transform ||
                 hit.transform.IsChildOf(transform)))
            {
                centerBlocked = false;
            }

            RaycastHit leftHit;
            bool leftBlocked = Physics.Raycast(
                rayOrigin,
                (forward - right * 0.5f).normalized,
                out leftHit,
                obstacleDetectionDistance * 0.85f,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            if (leftBlocked &&
                (leftHit.transform == transform ||
                 leftHit.transform.IsChildOf(transform)))
            {
                leftBlocked = false;
            }

            RaycastHit rightHit;
            bool rightBlocked = Physics.Raycast(
                rayOrigin,
                (forward + right * 0.5f).normalized,
                out rightHit,
                obstacleDetectionDistance * 0.85f,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            if (rightBlocked &&
                (rightHit.transform == transform ||
                 rightHit.transform.IsChildOf(transform)))
            {
                rightBlocked = false;
            }

            if (centerBlocked && hit.collider != null)
            {
                // Steer decisively left or right to bypass dynamic or static obstacles smoothly without stopping
                if (!rightBlocked)
                {
                    steering += right * 2.0f;
                }
                else if (!leftBlocked)
                {
                    steering -= right * 2.0f;
                }
                else
                {
                    // Hemmed in: slide dynamically along side tangent
                    steering += (Random.value < 0.5f ? right : -right) * 1.8f;
                }
            }
            else
            {
                if (leftBlocked) steering += right * 0.7f;
                if (rightBlocked) steering -= right * 0.7f;
            }

            // 2. Road Centerline Avoidance: Strictly prevent walking in the middle of the road
            if (CityPedestrianManager.Instance != null)
            {
                Vector3 roadRepulsion = CityPedestrianManager.Instance.GetRoadAvoidanceVector(transform.position, roadAvoidanceDistance);
                if (roadRepulsion != Vector3.zero)
                {
                    steering += roadRepulsion * 2.8f;
                }
            }

            steering.y = 0f;
            if (steering.sqrMagnitude < 0.001f)
            {
                steering = (forward + right * 0.5f).normalized;
            }
            cachedSteeringResult = steering.normalized;
            return cachedSteeringResult;
        }

        private void EnterIdleState(float duration)
        {
            // Unstoppable citizens: continuously maintain walking
            currentState = PedestrianState.Walking;
            UpdateAnimation(true);
        }

        private void UpdateIdle()
        {
            currentState = PedestrianState.Walking;
            UpdateWalking();
        }

        private void EnterGatheringState()
        {
            // Unstoppable citizens: continuously maintain walking
            currentState = PedestrianState.Walking;
            RequestNewSidewalkPath();
        }

        private void UpdateGathering()
        {
            currentState = PedestrianState.Walking;
            UpdateWalking();
        }

        private void UpdateAvoidingObstacle()
        {
            currentState = PedestrianState.Walking;
            UpdateWalking();
        }

        private void UpdateYieldingToCar()
        {
            currentState = PedestrianState.Walking;
            UpdateWalking();
        }

        public void SetPath(List<Vector3> path, bool isHotspot = false, string hotspotName = "")
        {
            if (path == null || path.Count == 0) return;

            currentPath = new List<Vector3>(path);
            pathIndex = 0;
            isGatheringAtHotspot = isHotspot;
            currentHotspotName = hotspotName;
            currentState = PedestrianState.Walking;
            UpdateAnimation(true);
        }

        private void RequestNewSidewalkPath()
        {
            if (CityPedestrianManager.Instance != null)
            {
                var newPath = CityPedestrianManager.Instance.GenerateCharacterWanderPath(transform.position, 4);
                SetPath(newPath, false);
            }
        }

        private void RequestPathToRandomHotspot()
        {
            if (CityPedestrianManager.Instance != null)
            {
                var newPath = CityPedestrianManager.Instance.GenerateCharacterWanderPath(transform.position, 4);
                SetPath(newPath, false);
            }
        }

        public void UpdateAnimation(bool isWalking)
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null) return;
            }

            // Prevent redundant updates if state hasn't changed
            if (lastWalkingState.HasValue && lastWalkingState.Value == isWalking)
            {
                return;
            }
            lastWalkingState = isWalking;

            if (hasIsWalkingParam)
            {
                animator.SetBool(isWalkingParamHash, isWalking);
            }

            if (isWalking)
            {
                animator.CrossFadeInFixedTime(activeWalkHash, 0.2f);
                animator.speed = Mathf.Clamp(walkSpeed / 1.6f, 0.85f, 1.25f);
            }
            else
            {
                animator.CrossFadeInFixedTime(activeIdleHash, 0.25f);
                animator.speed = 1.0f;
            }
        }
    }
}
