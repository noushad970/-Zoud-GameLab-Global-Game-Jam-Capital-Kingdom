using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Dedicated autonomous pedestrian AI designed specifically for the Grand Central Park.
    /// Traverses the park's sequentially linked 44 waypoints in exact numerical sequence:
    /// Waypoint -> Waypoint (1) -> Waypoint (2) -> ... -> Waypoint (43) -> loops back to Waypoint.
    /// Features:
    /// - Smooth locomotion and turning towards sequential waypoint targets.
    /// - Natural lateral path offsetting so visitors stroll organically rather than in single-file.
    /// - Occasional pauses at waypoints to admire flowers, trees, and scenic park areas.
    /// - Self-healing Animator configuration with Walk and Idle states.
    /// - Tagged and layered as Player for full physics and vehicle safety compatibility.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class ParkPedestrianController : MonoBehaviour
    {
        [Header("Waypoint Route")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private int currentWaypointIndex = 0;
        [SerializeField] private float arrivalDistance = 0.65f;

        [Header("Locomotion Attributes")]
        [SerializeField] private float walkSpeed = 1.5f;
        [SerializeField] private float turnSpeed = 280f;
        [SerializeField] private float lateralOffset = 0f;
        [SerializeField] private float gravity = 15f;

        [Header("Park Leisure Behavior")]
        [SerializeField] [Range(0f, 1f)] private float pauseChanceAtWaypoint = 0.16f;
        [SerializeField] private float minPauseDuration = 2.0f;
        [SerializeField] private float maxPauseDuration = 4.5f;

        [Header("Runtime State")]
        [SerializeField] private bool isPausing = false;
        [SerializeField] private float pauseTimer = 0f;
        [SerializeField] private Vector3 currentDestination;

        // Components
        private CharacterController characterController;
        private Animator animator;
        private int walkStateHash;
        private int idleStateHash;
        private int isWalkingParamHash;
        private bool hasIsWalkingParam = false;
        private bool? lastAnimWalkState = null;
        private float verticalVelocity = 0f;

        public int CurrentWaypointIndex => currentWaypointIndex;
        public bool IsPausing => isPausing;

        private void Awake()
        {
            SetupTagAndLayer();
            SetupCharacterController();
            SetupAnimator();

            // Randomize parameters for natural variety
            walkSpeed = Random.Range(1.3f, 1.85f);
            lateralOffset = Random.Range(-0.35f, 0.35f);
        }

        private void Start()
        {
            SetupTagAndLayer();
            UpdateAnimationState(!isPausing);
        }

        public void Initialize(List<Transform> routeWaypoints, int startIndex, RuntimeAnimatorController defaultAnimController = null)
        {
            waypoints = routeWaypoints;
            if (waypoints != null && waypoints.Count > 0)
            {
                currentWaypointIndex = Mathf.Clamp(startIndex, 0, waypoints.Count - 1);
            }

            if (defaultAnimController != null)
            {
                if (animator == null) animator = GetComponentInChildren<Animator>();
                if (animator != null && animator.runtimeAnimatorController == null)
                {
                    animator.runtimeAnimatorController = defaultAnimController;
                    animator.Rebind();
                }
            }

            SetupTagAndLayer();
            SetupCharacterController();
            SetupAnimator();

            UpdateDestination();
        }

        private void SetupTagAndLayer()
        {
            gameObject.tag = "Player";
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                gameObject.layer = playerLayer;
            }

            // Apply recursively to children
            foreach (Transform child in transform)
            {
                child.gameObject.tag = "Player";
                if (playerLayer != -1) child.gameObject.layer = playerLayer;
            }
        }

        private void SetupCharacterController()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
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

            // Auto-assign Animator Controller if missing
            if (animator.runtimeAnimatorController == null)
            {
                bool isFemale = ParkAIManager.IsFemaleCharacter(transform);
                if (CityPedestrianManager.Instance != null)
                {
                    animator.runtimeAnimatorController = isFemale
                        ? CityPedestrianManager.Instance.FemaleController
                        : CityPedestrianManager.Instance.MaleController;
                }

#if UNITY_EDITOR
                if (animator.runtimeAnimatorController == null)
                {
                    string ctrlPath = isFemale
                        ? "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Female Anim Controller.controller"
                        : "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Male Anim Controller.controller";

                    var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrlPath);
                    if (ctrl != null)
                    {
                        animator.runtimeAnimatorController = ctrl;
                        animator.Rebind();
                    }
                }
#endif
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            walkStateHash = Animator.StringToHash("Walk");
            idleStateHash = Animator.StringToHash("Idle");

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
                    walkStateHash = possibleWalks[i];
                    break;
                }
            }

            int[] possibleIdles =
            {
                Animator.StringToHash("Idle"),
                Animator.StringToHash("idle"),
                Animator.StringToHash("m_idle_A"),
                Animator.StringToHash("f_idle_A")
            };

            for (int i = 0; i < possibleIdles.Length; i++)
            {
                if (animator.HasState(0, possibleIdles[i]))
                {
                    idleStateHash = possibleIdles[i];
                    break;
                }
            }

            // Check for isWalking / IsWalking boolean parameter
            isWalkingParamHash = Animator.StringToHash("isWalking");
            hasIsWalkingParam = false;
            foreach (var p in animator.parameters)
            {
                if (p.name == "isWalking" || p.name == "IsWalking")
                {
                    isWalkingParamHash = p.nameHash;
                    hasIsWalkingParam = true;
                    break;
                }
            }
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Count == 0) return;

            // Handle leisure waypoint pause
            if (isPausing)
            {
                pauseTimer -= Time.deltaTime;
                if (pauseTimer <= 0f)
                {
                    isPausing = false;
                    AdvanceToNextWaypoint();
                }
                else
                {
                    ApplyGravity();
                    UpdateAnimationState(false);
                    return;
                }
            }

            Transform targetWP = waypoints[currentWaypointIndex];
            if (targetWP == null)
            {
                AdvanceToNextWaypoint();
                return;
            }

            // Calculate destination with slight lateral offset
            Vector3 dest = GetDestinationWithOffset(targetWP);
            currentDestination = dest;

            Vector3 diff = dest - transform.position;
            diff.y = 0f;
            float distToTarget = diff.magnitude;

            if (distToTarget <= arrivalDistance)
            {
                // Reached target waypoint
                if (Random.value < pauseChanceAtWaypoint)
                {
                    // Leisurely stop and enjoy the park atmosphere
                    isPausing = true;
                    pauseTimer = Random.Range(minPauseDuration, maxPauseDuration);
                    UpdateAnimationState(false);
                    return;
                }

                AdvanceToNextWaypoint();
                return;
            }

            // Rotate smoothly towards target strictly on horizontal yaw axis (zero pitch, zero roll)
            if (diff.sqrMagnitude > 0.001f)
            {
                float targetYaw = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
                float currentYaw = transform.eulerAngles.y;
                float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, turnSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }

            // Move forward
            Vector3 moveDirection = transform.forward * walkSpeed;
            ApplyGravity();
            moveDirection.y = verticalVelocity;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(moveDirection * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }
            else
            {
                transform.position += moveDirection * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            }

            UpdateAnimationState(true);
        }

        private Vector3 GetDestinationWithOffset(Transform targetWP)
        {
            Vector3 pos = targetWP.position;
            Vector3 right = targetWP.right;
            return pos + right * lateralOffset;
        }

        private void AdvanceToNextWaypoint()
        {
            if (waypoints == null || waypoints.Count == 0) return;

            // Move strictly sequentially: 0 -> 1 -> 2 -> ... -> 43 -> 0
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            UpdateDestination();
        }

        private void UpdateDestination()
        {
            if (waypoints == null || waypoints.Count == 0) return;
            Transform targetWP = waypoints[currentWaypointIndex];
            if (targetWP != null)
            {
                currentDestination = GetDestinationWithOffset(targetWP);
            }
        }

        private void ApplyGravity()
        {
            if (characterController != null && characterController.isGrounded)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }
        }

        private void UpdateAnimationState(bool isWalking)
        {
            if (animator == null) return;
            if (lastAnimWalkState.HasValue && lastAnimWalkState.Value == isWalking) return;

            lastAnimWalkState = isWalking;

            if (hasIsWalkingParam)
            {
                animator.SetBool(isWalkingParamHash, isWalking);
            }

            int targetHash = isWalking ? walkStateHash : idleStateHash;
            if (animator.HasState(0, targetHash))
            {
                animator.CrossFade(targetHash, 0.15f);
            }
        }

        public void SetWaypointIndex(int index)
        {
            if (waypoints != null && waypoints.Count > 0)
            {
                currentWaypointIndex = Mathf.Clamp(index, 0, waypoints.Count - 1);
                UpdateDestination();
            }
        }
    }
}

