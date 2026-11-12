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
        /// Loads the dedicated pedestrian waypoints created under 'AI Waypoints for character',
        /// and auto-links them into a connected sidewalk network.
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

            // Ensure waypoint nodes are connected into a continuous navigable graph
            AutoLinkWaypointNetwork();

            Debug.Log($"[CityPedestrianManager] Loaded {characterWaypoints.Count} dedicated character waypoints ({characterWaypointNodes.Count} PedestrianWaypointNodes).");
        }

        /// <summary>
        /// Automatically links all pedestrian waypoint nodes into a connected graph.
        /// Connects each node to its 2 to 3 nearest neighbor nodes within realistic sidewalk distance.
        /// </summary>
        public void AutoLinkWaypointNetwork()
        {
            if (characterWaypointNodes == null || characterWaypointNodes.Count < 2) return;

            for (int i = 0; i < characterWaypointNodes.Count; i++)
            {
                var nodeA = characterWaypointNodes[i];
                if (nodeA == null) continue;

                // If nextNodes is already manually configured, keep it
                if (nodeA.nextNodes != null && nodeA.nextNodes.Count > 0)
                {
                    // Clean nulls
                    nodeA.nextNodes.RemoveAll(n => n == null);
                    if (nodeA.nextNodes.Count > 0) continue;
                }

                // Find 2-3 closest neighbors within 60 meters
                List<KeyValuePair<PedestrianWaypointNode, float>> neighbors = new List<KeyValuePair<PedestrianWaypointNode, float>>();
                for (int j = 0; j < characterWaypointNodes.Count; j++)
                {
                    if (i == j) continue;
                    var nodeB = characterWaypointNodes[j];
                    if (nodeB == null) continue;

                    float dist = Vector3.Distance(nodeA.Position, nodeB.Position);
                    if (dist < 60f)
                    {
                        neighbors.Add(new KeyValuePair<PedestrianWaypointNode, float>(nodeB, dist));
                    }
                }

                neighbors.Sort((a, b) => a.Value.CompareTo(b.Value));
                int linkCount = Mathf.Min(3, neighbors.Count);
                for (int k = 0; k < linkCount; k++)
                {
                    nodeA.AddNextNode(neighbors[k].Key);
                    neighbors[k].Key.AddNextNode(nodeA); // Bidirectional
                }
            }
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

            // 3. Collect ALL AI characters in the scene
            List<AIPedestrianController> targetCharacters = new List<AIPedestrianController>();

            // Collect any explicitly assigned dedicated characters
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

            // Find all existing AIPedestrianControllers in the scene (excluding park-dedicated AI)
            AIPedestrianController[] existingControllers = Object.FindObjectsByType<AIPedestrianController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in existingControllers)
            {
                if (c == null) continue;
                if (c.name.Contains("Park") || (c.transform.parent != null && c.transform.parent.name.Contains("Park")) || c.GetComponent<ParkPedestrianController>() != null)
                {
                    // Remove rogue AIPedestrianController from park character
                    Destroy(c);
                    continue;
                }
                if (!targetCharacters.Contains(c))
                {
                    targetCharacters.Add(c);
                }
            }

            // Also discover all GameObject roots/children starting with "AI Character" (strictly city pedestrians, not park)
            var allSceneObjects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allSceneObjects)
            {
                if (t == null) continue;
                string n = t.name;
                if (n.Contains("Park") || (t.parent != null && t.parent.name.Contains("Park")) || t.GetComponent<ParkPedestrianController>() != null)
                {
                    continue;
                }

                if (n.StartsWith("AI Character") && (n.Contains("Male") || n.Contains("Female")))
                {
                    var ctrl = t.GetComponent<AIPedestrianController>();
                    if (ctrl == null) ctrl = t.gameObject.AddComponent<AIPedestrianController>();
                    if (!targetCharacters.Contains(ctrl))
                    {
                        targetCharacters.Add(ctrl);
                    }
                }
            }

            // Sync dedicatedCharacters list with discovered characters for Inspector
            dedicatedCharacters.Clear();
            dedicatedCharacters.AddRange(targetCharacters);

            // 4. Ensure all discovered AI characters are active and enabled
            for (int i = 0; i < targetCharacters.Count; i++)
            {
                var citizen = targetCharacters[i];
                if (citizen == null) continue;
                citizen.enabled = true;
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

        public PedestrianWaypointNode FindNextAvailableWaypointNode(PedestrianWaypointNode current, PedestrianWaypointNode previous)
        {
            if (characterWaypointNodes == null || characterWaypointNodes.Count == 0)
            {
                LoadCharacterWaypoints();
            }

            if (characterWaypointNodes == null || characterWaypointNodes.Count == 0) return null;

            // 1. If current node has branches, pick from them
            if (current != null && current.nextNodes != null && current.nextNodes.Count > 0)
            {
                var next = current.GetRandomNextNode(previous);
                if (next != null) return next;
            }

            // 2. Search for closest forward/neighboring waypoint
            Vector3 pos = current != null ? current.Position : Vector3.zero;
            PedestrianWaypointNode closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < characterWaypointNodes.Count; i++)
            {
                var node = characterWaypointNodes[i];
                if (node == null || node == current) continue;
                if (node == previous && characterWaypointNodes.Count > 2) continue;

                float d = Vector3.Distance(pos, node.Position);
                if (d < minDist && d > 1.2f)
                {
                    minDist = d;
                    closest = node;
                }
            }

            return closest ?? previous ?? current;
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

            // Pick neighboring waypoints within a realistic street distance (2.5m to 55m)
            for (int i = 0; i < characterWaypoints.Count; i++)
            {
                var wp = characterWaypoints[i];
                if (wp == null || wp == current) continue;
                if (wp == previous && characterWaypoints.Count > 2) continue;

                float dist = Vector3.Distance(currentPos, wp.position);
                if (dist >= 2.5f && dist <= 55f)
                {
                    candidates.Add(wp);
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            // Fallback: nearest other waypoint that is not current
            Transform nearest = null;
            float minD = float.MaxValue;
            for (int i = 0; i < characterWaypoints.Count; i++)
            {
                var wp = characterWaypoints[i];
                if (wp == null || wp == current) continue;
                float d = Vector3.Distance(currentPos, wp.position);
                if (d < minD && d > 0.8f)
                {
                    minD = d;
                    nearest = wp;
                }
            }

            return nearest ?? previous ?? current;
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
