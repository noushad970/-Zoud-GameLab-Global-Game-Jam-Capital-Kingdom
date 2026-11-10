using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Master coordinator for city pedestrians.
    /// Manages strictly the dedicated AI human characters in the scene,
    /// loads and enforces the manual character waypoints ('AI Waypoints for character'),
    /// and ensures characters wander exclusively along these waypoints without spawning procedural characters.
    /// </summary>
    public class CityPedestrianManager : MonoBehaviour
    {
        public static CityPedestrianManager Instance { get; private set; }

        [Header("Dedicated Pedestrian Waypoints")]
        [Tooltip("Root GameObject containing character waypoints ('AI Waypoints for character').")]
        [SerializeField] private GameObject pedestrianWaypointsRoot;

        [Tooltip("The manual waypoint transforms for AI characters.")]
        [SerializeField] private List<Transform> characterWaypoints = new List<Transform>();

        [Tooltip("The manual PedestrianWaypointNode components for AI characters.")]
        [SerializeField] private List<PedestrianWaypointNode> characterWaypointNodes = new List<PedestrianWaypointNode>();

        [Header("Static AI Characters in City")]
        [Tooltip("The dedicated 9 AI characters in the scene. Only these characters will be used in the city.")]
        [SerializeField] private List<AIPedestrianController> dedicatedCharacters = new List<AIPedestrianController>();

        [Header("Population Configuration")]
        [Tooltip("Disabled: Only the user-created static characters in the scene are used.")]
        [SerializeField] private bool autoSpawnCitizens = false;
        [SerializeField] private int targetPopulation = 9;

        [Header("Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController maleAnimatorController;
        [SerializeField] private RuntimeAnimatorController femaleAnimatorController;

        public RuntimeAnimatorController MaleController => maleAnimatorController;
        public RuntimeAnimatorController FemaleController => femaleAnimatorController;

        [Header("Runtime Telemetry")]
        [SerializeField] private List<AIPedestrianController> activeCitizens = new List<AIPedestrianController>();
        [SerializeField] private List<Vector3> waypointPositions = new List<Vector3>();

        public IReadOnlyList<AIPedestrianController> ActiveCitizens => activeCitizens;
        public IReadOnlyList<Transform> CharacterWaypoints => characterWaypoints;
        public IReadOnlyList<PedestrianWaypointNode> CharacterWaypointNodes => characterWaypointNodes;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureManager()
        {
            if (Instance == null)
            {
                var existing = Object.FindAnyObjectByType<CityPedestrianManager>();
                if (existing == null)
                {
                    GameObject managerObj = new GameObject("CityPedestrianManager");
                    managerObj.AddComponent<CityPedestrianManager>();
                }
            }
        }

        public static void EnsureExistsAndInitialize()
        {
            if (Instance == null)
            {
                var existing = Object.FindAnyObjectByType<CityPedestrianManager>();
                if (existing == null)
                {
                    GameObject managerObj = new GameObject("CityPedestrianManager");
                    managerObj.AddComponent<CityPedestrianManager>();
                }
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

#if UNITY_EDITOR
            if (maleAnimatorController == null)
            {
                maleAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Male Anim Controller.controller");
            }
            if (femaleAnimatorController == null)
            {
                femaleAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Female Anim Controller.controller");
            }
#endif

            LoadCharacterWaypoints();
        }

        private void Start()
        {
            InitializePedestrians();
        }

        /// <summary>
        /// Loads the dedicated pedestrian waypoints created under 'AI Waypoints for character'.
        /// </summary>
        public void LoadCharacterWaypoints()
        {
            characterWaypoints.Clear();
            characterWaypointNodes.Clear();
            waypointPositions.Clear();

            // 1. If pedestrianWaypointsRoot is assigned, load its children
            if (pedestrianWaypointsRoot == null)
            {
                pedestrianWaypointsRoot = GameObject.Find("AI Waypoints for character");
            }

            if (pedestrianWaypointsRoot != null)
            {
                Transform[] allTfs = pedestrianWaypointsRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTfs)
                {
                    if (t != pedestrianWaypointsRoot.transform && !characterWaypoints.Contains(t))
                    {
                        characterWaypoints.Add(t);
                        waypointPositions.Add(t.position);

                        var node = t.GetComponent<PedestrianWaypointNode>();
                        if (node == null)
                        {
                            node = t.gameObject.AddComponent<PedestrianWaypointNode>();
                        }
                        if (!characterWaypointNodes.Contains(node))
                        {
                            characterWaypointNodes.Add(node);
                        }
                    }
                }
            }

            // Fallback: search scene roots for any object named "AI Waypoints for character"
            if (characterWaypoints.Count == 0)
            {
                GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root.name.Contains("AI Waypoints"))
                    {
                        Transform[] allTfs = root.GetComponentsInChildren<Transform>(true);
                        foreach (var t in allTfs)
                        {
                            if (t != root.transform && !characterWaypoints.Contains(t))
                            {
                                characterWaypoints.Add(t);
                                waypointPositions.Add(t.position);

                                var node = t.GetComponent<PedestrianWaypointNode>();
                                if (node == null)
                                {
                                    node = t.gameObject.AddComponent<PedestrianWaypointNode>();
                                }
                                if (!characterWaypointNodes.Contains(node))
                                {
                                    characterWaypointNodes.Add(node);
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log($"[CityPedestrianManager] Loaded {characterWaypoints.Count} dedicated character waypoints ({characterWaypointNodes.Count} PedestrianWaypointNodes).");
        }

        /// <summary>
        /// Initializes strictly the designated scene AI characters and assigns them to the manual character waypoints.
        /// Destroys/removes any procedural or rogue character objects.
        /// </summary>
        public void InitializePedestrians()
        {
            activeCitizens.Clear();

            // 1. Load the dedicated character waypoints first
            LoadCharacterWaypoints();

            // 2. Remove any rogue/procedural spawned characters
            GameObject oldSpawns = GameObject.Find("Active_City_Pedestrians");
            if (oldSpawns != null) Destroy(oldSpawns);

            // 3. Collect the dedicated characters
            List<AIPedestrianController> targetCharacters = new List<AIPedestrianController>();

            if (dedicatedCharacters != null && dedicatedCharacters.Count > 0)
            {
                foreach (var c in dedicatedCharacters)
                {
                    if (c != null && !targetCharacters.Contains(c))
                    {
                        targetCharacters.Add(c);
                    }
                }
            }

            // Discover the 9 user characters by name if list is not fully populated
            if (targetCharacters.Count < 9)
            {
                string[] maleNames = new string[]
                {
                    "AI Character Male",
                    "AI Character Male (1)",
                    "AI Character Male (2)",
                    "AI Character Male (3)",
                    "AI Character Male (4)"
                };

                string[] femaleNames = new string[]
                {
                    "AI Character Female",
                    "AI Character Female (1)",
                    "AI Character Female (2)",
                    "AI Character Female (3)"
                };

                foreach (var n in maleNames)
                {
                    GameObject obj = GameObject.Find(n);
                    if (obj != null)
                    {
                        var ctrl = obj.GetComponent<AIPedestrianController>();
                        if (ctrl == null) ctrl = obj.AddComponent<AIPedestrianController>();
                        if (!targetCharacters.Contains(ctrl)) targetCharacters.Add(ctrl);
                    }
                }

                foreach (var n in femaleNames)
                {
                    GameObject obj = GameObject.Find(n);
                    if (obj != null)
                    {
                        var ctrl = obj.GetComponent<AIPedestrianController>();
                        if (ctrl == null) ctrl = obj.AddComponent<AIPedestrianController>();
                        if (!targetCharacters.Contains(ctrl)) targetCharacters.Add(ctrl);
                    }
                }
            }

            // Sync dedicatedCharacters list with discovered characters for Inspector
            dedicatedCharacters.Clear();
            dedicatedCharacters.AddRange(targetCharacters);

            // 4. Remove AIPedestrianController from any OTHER character in the scene
            AIPedestrianController[] allControllers = Object.FindObjectsByType<AIPedestrianController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allControllers)
            {
                if (c != null && !targetCharacters.Contains(c))
                {
                    // This is an unwanted extra AI character - disable & remove AI component
                    c.enabled = false;
                    Destroy(c);
                }
            }

            // 5. Initialize each of the dedicated characters
            for (int i = 0; i < targetCharacters.Count; i++)
            {
                var citizen = targetCharacters[i];
                if (citizen == null) continue;

                // Ensure Player tag and layer
                citizen.SetPlayerTagAndLayer();

                // Ensure CharacterController setup
                EnsureCharacterPhysics(citizen.gameObject);

                // Ensure Animator setup
                EnsureCharacterAnimator(citizen.gameObject, citizen.Gender == PedestrianGender.Female);

                // Assign initial waypoint node navigation
                if (characterWaypointNodes.Count > 0)
                {
                    PedestrianWaypointNode initialNode = FindClosestCharacterWaypointNode(citizen.transform.position);
                    if (initialNode != null)
                    {
                        citizen.SetCurrentWaypointNode(initialNode);
                    }
                }

                // Assign initial wander path using the dedicated waypoints
                if (characterWaypoints.Count > 0)
                {
                    List<Vector3> wanderPath = GenerateCharacterWanderPath(citizen.transform.position, 4);
                    citizen.SetPath(wanderPath, false);
                }

                activeCitizens.Add(citizen);
            }

            Debug.Log($"[CityPedestrianManager] Successfully initialized {activeCitizens.Count} dedicated AI characters on manual character waypoints.");
        }

        private void EnsureCharacterPhysics(GameObject go)
        {
            var cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();

            cc.center = new Vector3(0f, 0.85f, 0f);
            cc.radius = 0.28f;
            cc.height = 1.7f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            // Remove standalone CapsuleCollider if present to prevent PhysX self-collision
            var standaloneCol = go.GetComponent<CapsuleCollider>();
            if (standaloneCol != null) Destroy(standaloneCol);
        }

        public void EnsureCharacterAnimator(GameObject go, bool isFemale)
        {
            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null) return;

            if (anim.runtimeAnimatorController == null)
            {
                anim.runtimeAnimatorController = isFemale ? femaleAnimatorController : maleAnimatorController;
            }

            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        /// <summary>
        /// Generates a wander path strictly using the dedicated character waypoints.
        /// </summary>
        public List<Vector3> GenerateCharacterWanderPath(Vector3 fromPos, int steps = 4)
        {
            List<Vector3> path = new List<Vector3>();
            if (characterWaypoints == null || characterWaypoints.Count == 0)
            {
                LoadCharacterWaypoints();
            }

            if (characterWaypoints == null || characterWaypoints.Count == 0)
            {
                return path;
            }

            // Find closest waypoint to current position
            Transform currentWp = FindClosestCharacterWaypoint(fromPos);
            if (currentWp == null) return path;

            path.Add(currentWp.position);
            Transform prevWp = null;

            for (int s = 0; s < steps; s++)
            {
                Transform nextWp = GetNextCharacterWaypoint(currentWp, prevWp);
                if (nextWp != null && nextWp != currentWp)
                {
                    path.Add(nextWp.position);
                    prevWp = currentWp;
                    currentWp = nextWp;
                }
                else
                {
                    break;
                }
            }

            return path;
        }

        public PedestrianWaypointNode FindClosestCharacterWaypointNode(Vector3 pos)
        {
            if (characterWaypointNodes == null || characterWaypointNodes.Count == 0)
            {
                LoadCharacterWaypoints();
            }

            PedestrianWaypointNode closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < characterWaypointNodes.Count; i++)
            {
                var node = characterWaypointNodes[i];
                if (node == null) continue;

                float d = Vector3.Distance(pos, node.Position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = node;
                }
            }

            return closest;
        }

        public Transform FindClosestCharacterWaypoint(Vector3 pos)
        {
            Transform closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < characterWaypoints.Count; i++)
            {
                var wp = characterWaypoints[i];
                if (wp == null) continue;

                float d = Vector3.Distance(pos, wp.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = wp;
                }
            }

            return closest;
        }

        private Transform GetNextCharacterWaypoint(Transform current, Transform previous)
        {
            if (characterWaypoints.Count <= 1) return current;

            List<Transform> candidates = new List<Transform>();
            Vector3 currentPos = current.position;

            // Pick neighboring waypoints within a reasonable street distance (25m to 210m)
            for (int i = 0; i < characterWaypoints.Count; i++)
            {
                var wp = characterWaypoints[i];
                if (wp == null || wp == current || wp == previous) continue;

                float dist = Vector3.Distance(currentPos, wp.position);
                if (dist >= 25f && dist <= 210f)
                {
                    candidates.Add(wp);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            // Fallback: any other waypoint that is not current
            for (int i = 0; i < characterWaypoints.Count; i++)
            {
                var wp = characterWaypoints[i];
                if (wp != null && wp != current)
                {
                    candidates.Add(wp);
                }
            }

            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : current;
        }

        /// <summary>
        /// Returns a steering repulsion vector if the position is encroaching into vehicle road networks.
        /// </summary>
        public Vector3 GetRoadAvoidanceVector(Vector3 pos, float avoidanceDist)
        {
            if (CityTrafficManager.Instance == null || CityTrafficManager.Instance.AllWaypoints == null) return Vector3.zero;

            Vector3 repulsion = Vector3.zero;
            var carWaypoints = CityTrafficManager.Instance.AllWaypoints;
            for (int i = 0; i < carWaypoints.Count; i++)
            {
                var wp = carWaypoints[i];
                if (wp == null) continue;

                Vector3 diff = pos - wp.Position;
                diff.y = 0f;
                float d = diff.magnitude;
                if (d < avoidanceDist && d > 0.05f)
                {
                    repulsion += (diff.normalized) * (avoidanceDist - d);
                }
            }

            return repulsion.normalized;
        }

        /// <summary>
        /// Finds a nearby pedestrian to chat / socialize with during idle or gathering moments.
        /// </summary>
        public AIPedestrianController FindNearbyGatheringPartner(AIPedestrianController self, float radius = 3.2f)
        {
            if (activeCitizens == null || activeCitizens.Count <= 1) return null;

            Vector3 myPos = self.transform.position;
            for (int i = 0; i < activeCitizens.Count; i++)
            {
                var other = activeCitizens[i];
                if (other == null || other == self) continue;

                float dist = Vector3.Distance(myPos, other.transform.position);
                if (dist <= radius)
                {
                    return other;
                }
            }

            return null;
        }
    }
}
