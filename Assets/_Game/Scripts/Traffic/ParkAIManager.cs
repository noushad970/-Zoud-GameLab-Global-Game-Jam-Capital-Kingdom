using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.UI;
using EmpireBuilder.World;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Master coordinator for the Grand Central Park AI pedestrian system and entry fee revenue economics.
    /// Features:
    /// - Runs in both Edit Mode ([ExecuteAlways]) and Play Mode to guarantee characters never T-pose or tilt sideways.
    /// - Automatically gathers and sorts all 44 park waypoints into exact sequential circuit order:
    ///   Waypoint -> Waypoint (1) -> Waypoint (2) -> ... -> Waypoint (43) -> Waypoint.
    /// - Discovers all 16 Park AI Characters, initializes their ParkPedestrianControllers, and
    ///   evenly distributes them around the park circuit with upright orientation.
    /// - Assigns Female and Male Animator Controllers based on character model/armature analysis.
    /// - Generates periodic visitor entry fees when the park is purchased by the player.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ParkAIManager : MonoBehaviour
    {
        public static ParkAIManager Instance { get; private set; }

        [Header("Park Waypoints & Route Root")]
        [Tooltip("The parent GameObject containing all 44 sequential Waypoints and 16 Park AI Characters (usually 'ParkWaypoints')")]
        [SerializeField] private Transform parkWaypointsRoot;

        [Header("Park Property Reference")]
        [SerializeField] private PropertyBuilding parkProperty;

        [Header("Sequential Waypoints (Sorted Circuit)")]
        [SerializeField] private List<Transform> sequentialWaypoints = new List<Transform>();

        [Header("Park Characters")]
        [SerializeField] private List<ParkPedestrianController> parkCharacters = new List<ParkPedestrianController>();

        [Header("Visitor Entry Fee Economics")]
        [Tooltip("Minimum time (in seconds) between random visitor group arrivals")]
        [SerializeField] private float minVisitorInterval = 14f;
        [Tooltip("Maximum time (in seconds) between random visitor group arrivals")]
        [SerializeField] private float maxVisitorInterval = 28f;
        [SerializeField] private double minVisitorFee = 90.0;
        [SerializeField] private double maxVisitorFee = 360.0;

        [Header("Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController femaleAnimController;
        [SerializeField] private RuntimeAnimatorController maleAnimController;

        [Header("Runtime State")]
        [SerializeField] private float visitorTimer = 18f;
        [SerializeField] private double totalSessionFeesGenerated = 0.0;

        public List<Transform> SequentialWaypoints => sequentialWaypoints;
        public List<ParkPedestrianController> ParkCharacters => parkCharacters;
        public PropertyBuilding ParkProperty => parkProperty;

        private Coroutine visitorRevenueCoroutine;
        private float editModeTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (parkWaypointsRoot == null)
            {
                parkWaypointsRoot = transform;
            }

            LoadDefaultControllers();
            BuildWaypointsAndCharacters();
            FindParkProperty();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
            LoadDefaultControllers();
            BuildWaypointsAndCharacters();
            FindParkProperty();
        }

        private void Start()
        {
            if (parkProperty == null)
            {
                FindParkProperty();
            }

            if (Application.isPlaying)
            {
                visitorTimer = Random.Range(minVisitorInterval, maxVisitorInterval);
                visitorRevenueCoroutine = StartCoroutine(VisitorEntryFeeLoop());
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                editModeTimer += Time.deltaTime;
                if (editModeTimer > 2.0f)
                {
                    editModeTimer = 0f;
                    EnsureCharactersSanity();
                }
            }
        }

        private void OnDestroy()
        {
            if (visitorRevenueCoroutine != null)
            {
                StopCoroutine(visitorRevenueCoroutine);
                visitorRevenueCoroutine = null;
            }
            if (Instance == this) Instance = null;
        }

        public void LoadDefaultControllers()
        {
#if UNITY_EDITOR
            if (femaleAnimController == null)
            {
                femaleAnimController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Female Anim Controller.controller"
                );
            }
            if (maleAnimController == null)
            {
                maleAnimController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/animation/male/Male Anim Controller.controller"
                );
            }
#endif
            if (femaleAnimController == null && CityPedestrianManager.Instance != null)
            {
                femaleAnimController = CityPedestrianManager.Instance.FemaleController;
            }
            if (maleAnimController == null && CityPedestrianManager.Instance != null)
            {
                maleAnimController = CityPedestrianManager.Instance.MaleController;
            }
        }

        public void FindParkProperty()
        {
            PropertyBuilding[] all = Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in all)
            {
                if (b != null && b.PropertyType == PropertyType.Park)
                {
                    parkProperty = b;
                    break;
                }
            }
        }

        /// <summary>
        /// Reliably identifies whether a character rig is female or male based on bone armatures and meshes.
        /// </summary>
        public static bool IsFemaleCharacter(Transform t)
        {
            if (t == null) return false;
            if (t.name.IndexOf("female", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (t.Find("Female_Armature") != null || t.Find("Female_Hats") != null || t.Find("Female_Hairstyles") != null) return true;

            foreach (Transform c in t)
            {
                if (c.name.IndexOf("female", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            foreach (var smr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name.IndexOf("female", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (smr.sharedMesh != null && smr.sharedMesh.name.IndexOf("female", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        [ContextMenu("Rebuild Park Waypoints and Characters")]
        public void BuildWaypointsAndCharacters()
        {
            if (parkWaypointsRoot == null) parkWaypointsRoot = transform;

            LoadDefaultControllers();

            // 1. Gather all waypoints with their sequence indices
            var waypointIndexMap = new List<KeyValuePair<int, Transform>>();
            Regex wpRegex = new Regex(@"^Waypoint(?:\s*\((\d+)\))?$", RegexOptions.IgnoreCase);

            foreach (Transform child in parkWaypointsRoot)
            {
                string cName = child.name.Trim();
                Match match = wpRegex.Match(cName);
                if (match.Success)
                {
                    int index = 0;
                    if (match.Groups[1].Success && int.TryParse(match.Groups[1].Value, out int parsed))
                    {
                        index = parsed;
                    }
                    waypointIndexMap.Add(new KeyValuePair<int, Transform>(index, child));
                }
            }

            // Sort strictly in numerical sequence: 0, 1, 2, ...
            waypointIndexMap.Sort((a, b) => a.Key.CompareTo(b.Key));

            sequentialWaypoints.Clear();
            foreach (var kvp in waypointIndexMap)
            {
                sequentialWaypoints.Add(kvp.Value);
            }

            // 2. Discover Park Characters
            parkCharacters.Clear();
            var characterTransforms = new List<Transform>();

            foreach (Transform child in parkWaypointsRoot)
            {
                if (child.name.Contains("Character") || child.name.Contains("Park AI"))
                {
                    characterTransforms.Add(child);
                }
            }

            if (sequentialWaypoints.Count == 0)
            {
                return;
            }

            // Distribute characters along the waypoint circuit
            int totalChars = characterTransforms.Count;
            for (int i = 0; i < totalChars; i++)
            {
                Transform charTr = characterTransforms[i];

                // Ensure Player tag and layer
                charTr.gameObject.tag = "Player";
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer != -1) charTr.gameObject.layer = playerLayer;

                // Configure CharacterController
                var cc = charTr.GetComponent<CharacterController>();
                if (cc == null) cc = charTr.gameObject.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.radius = 0.28f;
                cc.height = 1.7f;
                cc.stepOffset = 0.35f;

                // Configure ParkPedestrianController
                var controller = charTr.GetComponent<ParkPedestrianController>();
                if (controller == null)
                {
                    controller = charTr.gameObject.AddComponent<ParkPedestrianController>();
                }

                // Remove any rogue AIPedestrianController that might have been attached by mistake
                var rogueCityCtrl = charTr.GetComponent<AIPedestrianController>();
                if (rogueCityCtrl != null)
                {
                    if (Application.isPlaying) Destroy(rogueCityCtrl);
                    else DestroyImmediate(rogueCityCtrl);
                }

                // Pick spaced starting waypoint index
                int startIndex = (i * sequentialWaypoints.Count) / Mathf.Max(1, totalChars);
                startIndex = Mathf.Clamp(startIndex, 0, sequentialWaypoints.Count - 1);

                Transform startWp = sequentialWaypoints[startIndex];

                // Enforce strictly upright rotation (zero pitch, zero roll)
                float initialYaw = startWp != null ? startWp.eulerAngles.y : charTr.eulerAngles.y;
                charTr.rotation = Quaternion.Euler(0f, initialYaw, 0f);

                if (startWp != null)
                {
                    // Place character grounded at starting waypoint
                    charTr.position = startWp.position + new Vector3(0f, 0.05f, 0f);
                }

                // Detect gender & assign proper Animator Controller
                bool isFemale = IsFemaleCharacter(charTr);
                RuntimeAnimatorController targetController = isFemale ? femaleAnimController : maleAnimController;

                var anim = charTr.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    if (targetController != null && anim.runtimeAnimatorController == null)
                    {
                        anim.runtimeAnimatorController = targetController;
                        anim.Rebind();
                    }
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                controller.Initialize(sequentialWaypoints, startIndex, targetController);
                parkCharacters.Add(controller);
            }
        }

        private void EnsureCharactersSanity()
        {
            if (femaleAnimController == null || maleAnimController == null)
            {
                LoadDefaultControllers();
            }

            foreach (var ctrl in parkCharacters)
            {
                if (ctrl == null) continue;
                Transform t = ctrl.transform;

                // Force upright rotation
                if (Mathf.Abs(t.eulerAngles.x) > 0.01f || Mathf.Abs(t.eulerAngles.z) > 0.01f)
                {
                    t.rotation = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
                }

                // Check Animator
                var anim = t.GetComponentInChildren<Animator>();
                if (anim != null && anim.runtimeAnimatorController == null)
                {
                    bool isFemale = IsFemaleCharacter(t);
                    anim.runtimeAnimatorController = isFemale ? femaleAnimController : maleAnimController;
                    anim.Rebind();
                }
            }
        }

        private IEnumerator VisitorEntryFeeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                if (parkProperty == null)
                {
                    FindParkProperty();
                    continue;
                }

                // Only generate admission entry fees if the park is purchased by the player!
                if (!parkProperty.IsOwned)
                {
                    continue;
                }

                visitorTimer -= 1.0f;
                if (visitorTimer <= 0f)
                {
                    visitorTimer = Random.Range(minVisitorInterval, maxVisitorInterval);
                    GenerateVisitorEntryFee();
                }
            }
        }

        private void GenerateVisitorEntryFee()
        {
            if (parkProperty == null || !parkProperty.IsOwned) return;

            double feeAmount = System.Math.Round(Random.Range((float)minVisitorFee, (float)maxVisitorFee) / 10.0) * 10.0;
            totalSessionFeesGenerated += feeAmount;

            parkProperty.AddParkEntryFee(feeAmount);

            CityNoticeBarUI.Instance?.ShowNotice(
                $"🎟️ Park Visitors: +${feeAmount:N0} entry fees accumulated at Grand Central Park!",
                NoticeType.RentReady,
                5.5f
            );

            // Play subtle collect chime
            GameSoundManager.Instance?.PlayCollect(parkProperty.transform.position);

            Debug.Log($"[ParkAIManager] New park visitors arrived! Added +${feeAmount:N0} entry fee (Total pending: ${parkProperty.PendingParkRevenue:N0}).");
        }
    }
}
