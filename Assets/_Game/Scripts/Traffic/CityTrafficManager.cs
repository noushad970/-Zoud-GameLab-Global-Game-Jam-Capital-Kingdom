using System.Collections.Generic;
using UnityEngine;
using EmpireBuilder.World;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Master coordinator for city traffic.
    /// Manages multiple independent manual road networks across city sectors/quadrants,
    /// discovers all cars in the scene, attaches AICarController components,
    /// sets up colliders for detection, distributes vehicles across the road networks,
    /// and launches the traffic simulation.
    /// </summary>
    public class CityTrafficManager : MonoBehaviour
    {
        public static CityTrafficManager Instance { get; private set; }

        public enum CarDistributionMode
        {
            BalancedQuadrantDistribution, // Distributes cars naturally across each quadrant/network so all 4 have active traffic
            SectorProximityOnly,          // Cars strictly follow the road network in their own sector/quadrant
            RoundRobin                    // Cycles strictly 1, 2, 3, 4 across all cars
        }

        [System.Serializable]
        public class RoadNetworkGroup
        {
            public string networkName;
            public GameObject networkRoot;
            public List<TrafficWaypointNode> waypoints = new List<TrafficWaypointNode>();
            public List<AICarController> assignedCars = new List<AICarController>();
            public Vector3 centerPosition;

            public RoadNetworkGroup() { }

            public RoadNetworkGroup(string name, GameObject root, List<TrafficWaypointNode> wps)
            {
                networkName = name;
                networkRoot = root;
                waypoints = wps ?? new List<TrafficWaypointNode>();
                assignedCars = new List<AICarController>();
                CalculateCenter();
            }

            public void CalculateCenter()
            {
                if (waypoints != null && waypoints.Count > 0)
                {
                    Vector3 sum = Vector3.zero;
                    int count = 0;
                    for (int i = 0; i < waypoints.Count; i++)
                    {
                        if (waypoints[i] != null)
                        {
                            sum += waypoints[i].Position;
                            count++;
                        }
                    }
                    centerPosition = count > 0 ? sum / count : (networkRoot != null ? networkRoot.transform.position : Vector3.zero);
                }
                else if (networkRoot != null)
                {
                    centerPosition = networkRoot.transform.position;
                }
            }
        }

        [Header("Road Networks (Multi-Network System)")]
        [Tooltip("List of manual road networks in the city. Can be configured in Inspector or automatically discovered at Start.")]
        [SerializeField] private List<GameObject> manualRoadNetworks = new List<GameObject>();

        [Tooltip("Fallback single road network reference for backwards compatibility.")]
        [SerializeField] private GameObject manualRoadNetwork;

        [Tooltip("How vehicles should be distributed among the road networks.")]
        [SerializeField] private CarDistributionMode distributionMode = CarDistributionMode.BalancedQuadrantDistribution;

        [Header("Traffic Configuration")]
        [Tooltip("Automatically setup and activate all cars on Start.")]
        [SerializeField] private bool autoInitializeCars = true;

        [Tooltip("Base cruising speed for spawned/detected cars.")]
        [SerializeField] private float defaultCarSpeed = 11.0f;

        [Tooltip("Sector root names that contain city blocks and roads.")]
        [SerializeField] private string[] sectorRootNames = new string[]
        {
            "Hotel Zone",
            "Demonstration (1)",
            "Demonstration (2)",
            "Demonstration (3)"
        };

        [Header("Discovered Road Network Groups")]
        [SerializeField] private List<RoadNetworkGroup> roadNetworkGroups = new List<RoadNetworkGroup>();

        [Header("Runtime Telemetry")]
        [SerializeField] private List<AICarController> registeredCars = new List<AICarController>();
        [SerializeField] private List<TrafficWaypointNode> allWaypoints = new List<TrafficWaypointNode>();
        [SerializeField] private int activeCarCount = 0;

        public IReadOnlyList<AICarController> RegisteredCars => registeredCars;
        public IReadOnlyList<TrafficWaypointNode> AllWaypoints => allWaypoints;
        public IReadOnlyList<RoadNetworkGroup> RoadNetworkGroups => roadNetworkGroups;
        public List<GameObject> ManualRoadNetworks => manualRoadNetworks;

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
        }

        private void Start()
        {
            if (autoInitializeCars)
            {
                InitializeAllTraffic();
            }
        }

        /// <summary>
        /// Discovers all road networks, distributes vehicles across them, sets up
        /// their AI controllers and starting waypoints, and launches the city simulation.
        /// </summary>
        public void InitializeAllTraffic()
        {
            registeredCars.Clear();
            allWaypoints.Clear();

            // 1. Load and partition all road networks
            LoadManualRoadNetworks();

            if (roadNetworkGroups.Count == 0 || allWaypoints.Count == 0)
            {
                Debug.LogWarning("[CityTrafficManager] No road networks or TrafficWaypointNodes found in the scene!");
                return;
            }

            Debug.Log($"[CityTrafficManager] Loaded {roadNetworkGroups.Count} distinct road networks with {allWaypoints.Count} total waypoints.");

            // 2. Discover all vehicle GameObjects in the scene
            List<Transform> discoveredCarTransforms = CollectAllCarsInScene();

            // 3. Assign each vehicle to a designated road network and starting waypoint
            AssignVehiclesToNetworks(discoveredCarTransforms);

            activeCarCount = registeredCars.Count;
            Debug.Log($"[CityTrafficManager] Traffic initialized with {activeCarCount} AI cars distributed across {roadNetworkGroups.Count} road networks.");

            // 4. Discover and initialize railway trains
            InitializeTrains();

            // 5. Discover and launch pedestrian AI system
            CityPedestrianManager.EnsureExistsAndInitialize();
        }

        /// <summary>
        /// Discovers all railway trains in the city and ensures they have active TrainPathFollower components.
        /// </summary>
        private void InitializeTrains()
        {
            string[] trainNames = new string[] { "Train", "Train (1)", "Train (2)", "Train (3)" };
            int trainCount = 0;

            foreach (string tName in trainNames)
            {
                GameObject trainObj = GameObject.Find(tName);
                if (trainObj != null)
                {
                    var follower = trainObj.GetComponent<TrainPathFollower>();
                    if (follower == null)
                    {
                        follower = trainObj.AddComponent<TrainPathFollower>();
                    }
                    trainCount++;
                }
            }

            // Also check under Buildings hierarchy if any are nested
            GameObject buildingsObj = GameObject.Find("Buildings");
            if (buildingsObj != null)
            {
                foreach (Transform child in buildingsObj.transform)
                {
                    if (child.name.StartsWith("Train") && !child.name.Contains("Carriage"))
                    {
                        var follower = child.GetComponent<TrainPathFollower>();
                        if (follower == null)
                        {
                            follower = child.gameObject.AddComponent<TrainPathFollower>();
                            trainCount++;
                        }
                    }
                }
            }

            Debug.Log($"[CityTrafficManager] Verified {trainCount} trains initialized with non-rotating push-pull railway logic.");
        }

        /// <summary>
        /// Finds an alternate waypoint to clear traffic deadlocks or loop back onto the continuing road circuit.
        /// If candidateNodes is provided, restricts search to candidateNodes (e.g. car's assigned road network).
        /// </summary>
        public TrafficWaypointNode FindAlternativeWaypoint(
            Vector3 currentPos,
            Vector3 forwardDir,
            TrafficWaypointNode excludeNode1,
            TrafficWaypointNode excludeNode2 = null,
            float maxDist = 35f,
            List<TrafficWaypointNode> candidateNodes = null)
        {
            var searchList = (candidateNodes != null && candidateNodes.Count > 0) ? candidateNodes : allWaypoints;
            TrafficWaypointNode best = null;
            float bestScore = -999f;

            for (int i = 0; i < searchList.Count; i++)
            {
                var node = searchList[i];
                if (node == null || node == excludeNode1 || node == excludeNode2) continue;

                Vector3 toNode = (node.Position - currentPos);
                toNode.y = 0f;
                float dist = toNode.magnitude;
                if (dist < 4.0f || dist > maxDist) continue;

                // Check angle relative to current forward direction
                float forwardDot = Vector3.Dot(forwardDir, toNode.normalized);

                // Score favors nodes that deviate from the blocked forward direction
                float score = (1.0f - forwardDot * 0.6f) * 10f - Mathf.Abs(dist - 14f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            return best;
        }

        #region Manual Road Network Discovery
        /// <summary>
        /// Discovers all manual road networks and groups their waypoints.
        /// </summary>
        public void LoadManualRoadNetworks()
        {
            roadNetworkGroups.Clear();
            allWaypoints.Clear();

            HashSet<GameObject> registeredRoots = new HashSet<GameObject>();

            // 1. Process explicit manualRoadNetworks assigned in Inspector
            if (manualRoadNetworks != null)
            {
                for (int i = 0; i < manualRoadNetworks.Count; i++)
                {
                    GameObject netObj = manualRoadNetworks[i];
                    if (netObj != null && !registeredRoots.Contains(netObj))
                    {
                        RegisterRoadNetworkObject(netObj, registeredRoots);
                    }
                }
            }

            // 2. Process legacy single manualRoadNetwork if assigned
            if (manualRoadNetwork != null && !registeredRoots.Contains(manualRoadNetwork))
            {
                RegisterRoadNetworkObject(manualRoadNetwork, registeredRoots);
            }

            // 3. Auto-discover all active GameObjects named "RoadNetwork" in the scene
            // (e.g. under Hotel Zone, Demonstration (1), Demonstration (2), Demonstration (3))
            GameObject[] allSceneObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allSceneObjects)
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t.name == "RoadNetwork" && t.gameObject.activeInHierarchy)
                    {
                        if (!registeredRoots.Contains(t.gameObject))
                        {
                            RegisterRoadNetworkObject(t.gameObject, registeredRoots);
                        }
                    }
                }
            }

            // 4. Fallback: If no groups were formed, group any remaining waypoints
            if (roadNetworkGroups.Count == 0)
            {
                var sceneWaypoints = Object.FindObjectsByType<TrafficWaypointNode>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (sceneWaypoints != null && sceneWaypoints.Length > 0)
                {
                    List<TrafficWaypointNode> wps = new List<TrafficWaypointNode>(sceneWaypoints);
                    RoadNetworkGroup fallbackGroup = new RoadNetworkGroup("Default Network", null, wps);
                    roadNetworkGroups.Add(fallbackGroup);
                    allWaypoints.AddRange(wps);
                }
            }

            // Sync manualRoadNetworks list with discovered groups for Inspector visibility
            if (manualRoadNetworks == null) manualRoadNetworks = new List<GameObject>();
            foreach (var g in roadNetworkGroups)
            {
                if (g.networkRoot != null && !manualRoadNetworks.Contains(g.networkRoot))
                {
                    manualRoadNetworks.Add(g.networkRoot);
                }
            }
        }

        private void RegisterRoadNetworkObject(GameObject netObj, HashSet<GameObject> registeredRoots)
        {
            if (netObj == null || registeredRoots.Contains(netObj)) return;

            TrafficWaypointNode[] nodes = netObj.GetComponentsInChildren<TrafficWaypointNode>(true);
            if (nodes == null || nodes.Length == 0) return;

            List<TrafficWaypointNode> validNodes = new List<TrafficWaypointNode>();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && !validNodes.Contains(nodes[i]))
                {
                    validNodes.Add(nodes[i]);
                    if (!allWaypoints.Contains(nodes[i]))
                    {
                        allWaypoints.Add(nodes[i]);
                    }
                }
            }

            if (validNodes.Count == 0) return;

            string groupName = netObj.name;
            if (netObj.transform.parent != null)
            {
                groupName = $"{netObj.transform.parent.name} - RoadNetwork";
            }

            RoadNetworkGroup group = new RoadNetworkGroup(groupName, netObj, validNodes);
            roadNetworkGroups.Add(group);
            registeredRoots.Add(netObj);
        }
        #endregion

        #region Car Discovery & Multi-Network Distribution
        private List<Transform> CollectAllCarsInScene()
        {
            List<Transform> carTransforms = new List<Transform>();
            HashSet<Transform> seen = new HashSet<Transform>();

            // Collect from sector roots
            foreach (string sectorName in sectorRootNames)
            {
                GameObject sectorObj = GameObject.Find(sectorName);
                if (sectorObj != null)
                {
                    ScanForCars(sectorObj.transform, carTransforms, seen);
                }
            }

            // Collect from Buildings root
            GameObject buildingsObj = GameObject.Find("Buildings");
            if (buildingsObj != null)
            {
                ScanForCars(buildingsObj.transform, carTransforms, seen);
            }

            // Collect from each RoadNetwork root (in case cars were parented under a network)
            foreach (var group in roadNetworkGroups)
            {
                if (group.networkRoot != null)
                {
                    ScanForCars(group.networkRoot.transform, carTransforms, seen);
                }
            }

            // Also scan active scene roots to catch any placed directly in the scene hierarchy
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                ScanForCars(root.transform, carTransforms, seen);
            }

            return carTransforms;
        }

        private void ScanForCars(Transform root, List<Transform> carTransforms, HashSet<Transform> seen)
        {
            if (root == null) return;
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allChildren)
            {
                if (t == null || seen.Contains(t)) continue;
                string lowerName = t.name.ToLower();

                // Clean up any rogue components on wheel/mesh subparts
                if (lowerName.Contains("wheel") || 
                    lowerName.Contains("spoiler") || 
                    lowerName.Contains("glass") || 
                    lowerName.Contains("light") ||
                    lowerName.Contains("door") ||
                    lowerName.Contains("section") ||
                    lowerName.Contains("title") ||
                    lowerName.Contains("button"))
                {
                    AICarController rogueAi = t.GetComponent<AICarController>();
                    if (rogueAi != null) Destroy(rogueAi);
                    continue;
                }

                // Exclude any unrented vehicles located in or under a Taxi Station (reserved for player taxi feature)
                if (IsInTaxiStation(t))
                {
                    AICarController rogueAi = t.GetComponent<AICarController>();
                    if (rogueAi != null && !rogueAi.IsRentedTaxi) Destroy(rogueAi);
                    continue;
                }

                // Check if this transform is a car root
                bool isCarName = lowerName.StartsWith("car_") ||
                                 lowerName.StartsWith("van") ||
                                 lowerName.StartsWith("futuristic_car") ||
                                 lowerName.StartsWith("taxi") ||
                                 lowerName.StartsWith("vehicle_taxi");

                bool hasAi = t.GetComponent<AICarController>() != null;

                if (isCarName || hasAi)
                {
                    // Ensure we only select the top-level car root, not a nested subpart
                    if (t.parent != null && t.parent.GetComponentInParent<AICarController>() != null)
                    {
                        continue;
                    }

                    seen.Add(t);
                    carTransforms.Add(t);
                }
            }
        }

        private void AssignVehiclesToNetworks(List<Transform> carTransforms)
        {
            if (carTransforms == null || carTransforms.Count == 0 || roadNetworkGroups.Count == 0) return;

            // Reset assigned cars in groups
            for (int i = 0; i < roadNetworkGroups.Count; i++)
            {
                roadNetworkGroups[i].assignedCars.Clear();
            }

            // Keep track of claimed starting waypoints per network to distribute cars along different waypoints
            Dictionary<RoadNetworkGroup, HashSet<TrafficWaypointNode>> usedStartWaypoints = new Dictionary<RoadNetworkGroup, HashSet<TrafficWaypointNode>>();
            for (int i = 0; i < roadNetworkGroups.Count; i++)
            {
                usedStartWaypoints[roadNetworkGroups[i]] = new HashSet<TrafficWaypointNode>();
            }

            int roundRobinIdx = 0;

            for (int i = 0; i < carTransforms.Count; i++)
            {
                Transform carT = carTransforms[i];
                if (carT == null || IsInTaxiStation(carT))
                {
                    if (carT != null)
                    {
                        AICarController rogueAi = carT.GetComponent<AICarController>();
                        if (rogueAi != null) Destroy(rogueAi);
                    }
                    continue;
                }

                // Ensure AICarController exists
                AICarController aiCar = carT.GetComponent<AICarController>();
                if (aiCar == null)
                {
                    aiCar = carT.gameObject.AddComponent<AICarController>();
                }

                // Ensure BoxCollider exists for obstacle detection
                BoxCollider col = carT.GetComponent<BoxCollider>();
                if (col == null)
                {
                    col = carT.gameObject.AddComponent<BoxCollider>();
                    col.center = new Vector3(0f, 0.75f, 0f);
                    col.size = new Vector3(2.2f, 1.5f, 4.4f);
                }

                // Determine target road network group
                RoadNetworkGroup targetGroup = null;

                // Priority 1: Check if vehicle has an explicit inspector assignment
                if (aiCar.AssignedRoadNetwork != null)
                {
                    targetGroup = roadNetworkGroups.Find(g => g.networkRoot == aiCar.AssignedRoadNetwork);
                }

                // Priority 2: Select group based on distribution mode
                if (targetGroup == null)
                {
                    switch (distributionMode)
                    {
                        case CarDistributionMode.RoundRobin:
                            targetGroup = roadNetworkGroups[roundRobinIdx % roadNetworkGroups.Count];
                            roundRobinIdx++;
                            break;

                        case CarDistributionMode.SectorProximityOnly:
                            targetGroup = FindClosestNetworkGroup(carT.position);
                            break;

                        case CarDistributionMode.BalancedQuadrantDistribution:
                        default:
                            // Check if car is child/ancestor of a sector that contains a network
                            targetGroup = FindNetworkGroupForSector(carT);
                            if (targetGroup == null)
                            {
                                targetGroup = FindClosestNetworkGroup(carT.position);
                            }
                            break;
                    }
                }

                if (targetGroup == null)
                {
                    targetGroup = roadNetworkGroups[0];
                }

                // Check starting waypoint in the assigned group
                var usedWps = usedStartWaypoints[targetGroup];
                TrafficWaypointNode startWp = FindBestInitialWaypoint(carT, targetGroup.waypoints, usedWps);
                if (startWp != null)
                {
                    usedWps.Add(startWp);
                }

                // Wire up the car with its designated network
                aiCar.SetAssignedRoadNetwork(targetGroup.networkRoot, targetGroup.networkName, targetGroup.waypoints);
                if (startWp != null)
                {
                    aiCar.SetInitialWaypoint(startWp);
                }

                targetGroup.assignedCars.Add(aiCar);

                if (!registeredCars.Contains(aiCar))
                {
                    registeredCars.Add(aiCar);
                }
            }

            // Post-distribution balance check:
            // If any road network has 0 cars, and another network has >= 3 cars,
            // reassign surplus cars so that every road network has active vehicles!
            if (distributionMode == CarDistributionMode.BalancedQuadrantDistribution && roadNetworkGroups.Count > 1)
            {
                RebalanceEmptyNetworks(usedStartWaypoints);
            }
        }

        private void RebalanceEmptyNetworks(Dictionary<RoadNetworkGroup, HashSet<TrafficWaypointNode>> usedStartWaypoints)
        {
            List<RoadNetworkGroup> emptyGroups = roadNetworkGroups.FindAll(g => g.assignedCars.Count == 0);
            if (emptyGroups.Count == 0) return;

            foreach (var emptyGroup in emptyGroups)
            {
                // Find group with highest number of cars (> 2)
                RoadNetworkGroup donorGroup = null;
                int maxCars = 2;
                foreach (var g in roadNetworkGroups)
                {
                    if (g.assignedCars.Count > maxCars)
                    {
                        maxCars = g.assignedCars.Count;
                        donorGroup = g;
                    }
                }

                if (donorGroup != null && donorGroup.assignedCars.Count > 2)
                {
                    // Transfer the last car from donor to empty group
                    AICarController carToTransfer = donorGroup.assignedCars[donorGroup.assignedCars.Count - 1];
                    donorGroup.assignedCars.RemoveAt(donorGroup.assignedCars.Count - 1);

                    var usedWps = usedStartWaypoints[emptyGroup];
                    TrafficWaypointNode newStartWp = FindBestInitialWaypoint(carToTransfer.transform, emptyGroup.waypoints, usedWps);
                    if (newStartWp != null) usedWps.Add(newStartWp);

                    carToTransfer.SetAssignedRoadNetwork(emptyGroup.networkRoot, emptyGroup.networkName, emptyGroup.waypoints);
                    if (newStartWp != null) carToTransfer.SetInitialWaypoint(newStartWp);

                    emptyGroup.assignedCars.Add(carToTransfer);
                }
            }
        }

        private RoadNetworkGroup FindNetworkGroupForSector(Transform carTransform)
        {
            Transform curr = carTransform;
            while (curr != null)
            {
                for (int i = 0; i < roadNetworkGroups.Count; i++)
                {
                    var group = roadNetworkGroups[i];
                    if (group.networkRoot != null)
                    {
                        if (group.networkRoot.transform.parent != null && group.networkRoot.transform.parent == curr)
                        {
                            return group;
                        }
                        if (group.networkRoot.transform == curr)
                        {
                            return group;
                        }
                    }
                }
                curr = curr.parent;
            }
            return null;
        }

        private RoadNetworkGroup FindClosestNetworkGroup(Vector3 position)
        {
            RoadNetworkGroup closest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < roadNetworkGroups.Count; i++)
            {
                var g = roadNetworkGroups[i];
                float dist = Vector3.Distance(position, g.centerPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = g;
                }
            }

            return closest ?? (roadNetworkGroups.Count > 0 ? roadNetworkGroups[0] : null);
        }

        /// <summary>
        /// Returns true if the transform is located in or under a Taxi Station.
        /// Stationed taxis are reserved for dedicated player taxi rental / station gameplay mechanics
        /// and are excluded from ambient city traffic AI.
        /// </summary>
        public static bool IsInTaxiStation(Transform t)
        {
            if (t == null) return false;
            Transform curr = t;
            while (curr != null)
            {
                string lower = curr.name.ToLower();
                if (lower.Contains("texi station") || 
                    lower.Contains("taxi station") || 
                    lower.Contains("texistation") || 
                    lower.Contains("taxistation") ||
                    lower.Contains("texi spawn") || 
                    lower.Contains("taxi spawn"))
                {
                    return true;
                }

                var prop = curr.GetComponent<PropertyBuilding>();
                if (prop != null && prop.PropertyType == PropertyType.TaxiStation)
                {
                    return true;
                }

                curr = curr.parent;
            }
            return false;
        }

        public TrafficWaypointNode FindBestInitialWaypoint(
            Transform carTransform,
            List<TrafficWaypointNode> waypoints,
            HashSet<TrafficWaypointNode> preferredExclusions = null)
        {
            if (waypoints == null || waypoints.Count == 0) return null;

            TrafficWaypointNode bestNode = null;
            float bestScore = float.MinValue;

            Vector3 carPos = carTransform.position;
            Vector3 carForward = carTransform.forward;
            carForward.y = 0f;
            carForward.Normalize();

            // Pass 1: Look for forward-aligned waypoint within 60m corridor
            for (int i = 0; i < waypoints.Count; i++)
            {
                var node = waypoints[i];
                if (node == null) continue;

                Vector3 nodePos = node.Position;
                Vector3 toNode = (nodePos - carPos);
                toNode.y = 0f;
                float distance = toNode.magnitude;

                if (distance > 60f) continue;

                float headingAlignment = (distance > 0.1f) ? Vector3.Dot(carForward, toNode.normalized) : 0f;

                float outgoingAlignment = 0f;
                if (node.nextNodes != null)
                {
                    for (int j = 0; j < node.nextNodes.Count; j++)
                    {
                        var next = node.nextNodes[j];
                        if (next != null)
                        {
                            Vector3 segDir = (next.Position - nodePos).normalized;
                            float align = Vector3.Dot(carForward, segDir);
                            if (align > outgoingAlignment) outgoingAlignment = align;
                        }
                    }
                }

                // If already claimed as a starting waypoint by another car, apply a penalty to space cars out
                float exclusionPenalty = (preferredExclusions != null && preferredExclusions.Contains(node)) ? 35f : 0f;

                float score = (headingAlignment * 40f) + (outgoingAlignment * 20f) - (distance * 1.5f) - exclusionPenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestNode = node;
                }
            }

            // Pass 2: Fallback to closest waypoint in the network if car is further away or not forward-aligned
            if (bestNode == null)
            {
                float closestDist = float.MaxValue;
                for (int i = 0; i < waypoints.Count; i++)
                {
                    var node = waypoints[i];
                    if (node == null) continue;
                    float d = Vector3.Distance(carPos, node.Position);
                    if (preferredExclusions != null && preferredExclusions.Contains(node))
                    {
                        d += 15f; // Slight penalty to prefer unoccupied starting waypoints
                    }
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestNode = node;
                    }
                }
            }

            return bestNode;
        }
        #endregion
    }
}
