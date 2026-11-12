using System.Collections.Generic;
using EmpireBuilder.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Manages touch and click detection for selecting city buildings.
    /// Distinguishes between camera swiping/panning and deliberate building tapping,
    /// applies the glow shader highlight, and opens the building detail UI.
    /// Also handles automatic discovery and setup of buildings throughout the city.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingInteractionManager : MonoBehaviour
    {
        public static BuildingInteractionManager Instance { get; private set; }

        [Header("Camera & Raycast Settings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask raycastMask = ~0; // Default to all layers
        [SerializeField] private float maxRayDistance = 600f;

        [Header("Tap Detection Sensitivity")]
        [Tooltip("Maximum pixel movement between pointer down and up to be registered as a tap instead of camera pan")]
        [SerializeField] private float tapMaxPixelDistance = 18f;

        [Header("Default Selection Material")]
        [Tooltip("The ghostMaterial shader used when selecting a building")]
        [SerializeField] private Material defaultSelectionMaterial;

        [Header("City Building Discovery")]
        [SerializeField] private bool autoRegisterCityBuildings = true;

        [Header("UI Reference (Optional, auto-found if null)")]
        [SerializeField] private PropertyDetailModalUI detailModalUI;

        private PropertyBuilding currentSelectedBuilding;
        private Vector2 pointerDownPosition;
        private bool isPointerTracking = false;
        private bool pointerStartedOverUI = false;

        public PropertyBuilding CurrentSelectedBuilding => currentSelectedBuilding;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure GameManager is initialized if scene is played directly
            if (EmpireBuilder.Core.GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                var gm = gmObj.AddComponent<EmpireBuilder.Core.GameManager>();
                gm.StartNewGame();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

#if UNITY_EDITOR
            if (defaultSelectionMaterial == null)
            {
                defaultSelectionMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Assets No/characterEffects/shaders/ghostEffect/ghostMaterial.mat"
                );
            }
#endif
            if (defaultSelectionMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default");
                if (s != null)
                {
                    defaultSelectionMaterial = new Material(s);
                    defaultSelectionMaterial.name = "BuildingSelectionGlow_Default";
                    defaultSelectionMaterial.SetColor("_BaseColor", new Color(0.2f, 0.75f, 1f, 0.35f));
                    defaultSelectionMaterial.SetColor("_Color", new Color(0.2f, 0.75f, 1f, 0.35f));
                    defaultSelectionMaterial.SetFloat("_Surface", 1);
                    defaultSelectionMaterial.SetFloat("_Blend", 0);
                    defaultSelectionMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    defaultSelectionMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    defaultSelectionMaterial.SetFloat("_ZWrite", 0);
                    defaultSelectionMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
            }

            if (detailModalUI == null)
            {
                detailModalUI = FindObjectOfType<PropertyDetailModalUI>(true);
            }
        }

        private void Start()
        {
            if (autoRegisterCityBuildings)
            {
                InitializeCityBuildings();
            }
        }

        private void Update()
        {
            HandleTouchInput();
            HandleMouseInput();
        }

        #region City Buildings Auto-Discovery & Initialization
        /// <summary>
        /// Checks whether a GameObject name represents a sector root container or utility hierarchy
        /// that must NEVER be treated as a building or receive PropertyBuilding.
        /// </summary>
        public static bool IsSectorOrContainer(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.Trim().ToLowerInvariant();
            return n == "buildings" ||
                   n == "hotel zone" ||
                   n.StartsWith("demonstration") ||
                   n.StartsWith("roadnetwork") ||
                   n.StartsWith("ai waypoints") ||
                   n.StartsWith("parkwaypoints") ||
                   n == "terrain" ||
                   n == "environment" ||
                   n == "gameplay" ||
                   n.Contains("canvas") ||
                   n.Contains("camera") ||
                   n.Contains("light") ||
                   n.Contains("volume") ||
                   n.Contains("manager") ||
                   n.Contains("eventsystem");
        }

        /// <summary>
        /// Scans all game objects in the city and attaches/configures PropertyBuilding
        /// and BoxColliders so that every house, apartment, restaurant, stadium, and taxi station
        /// is fully touchable and interactive.
        /// </summary>
        public void InitializeCityBuildings()
        {
            // First, strip any accidental PropertyBuilding or BoxCollider from sector roots
            string[] sectorNames = new string[]
            {
                "Buildings", "Hotel Zone", "Demonstration", "Demonstration (1)",
                "Demonstration (2)", "Demonstration (3)", "RoadNetwork", "AI Waypoints for character", "Terrain", "ParkWaypoints"
            };
            foreach (var sName in sectorNames)
            {
                GameObject sObj = GameObject.Find(sName);
                if (sObj != null)
                {
                    PropertyBuilding pb = sObj.GetComponent<PropertyBuilding>();
                    if (pb != null) Destroy(pb);
                    BoxCollider bc = sObj.GetComponent<BoxCollider>();
                    if (bc != null) Destroy(bc);
                }
            }

            // Clean any stray PropertyBuilding and BuildingWorldIndicator components from all children of Park & ParkWaypoints
            GameObject parkObj = GameObject.Find("Park");
            if (parkObj == null)
            {
                var allT = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var tr in allT)
                {
                    if (tr != null && (tr.name == "Park" || tr.CompareTag("Park")) && (tr.parent == null || tr.parent.name != "Park"))
                    {
                        parkObj = tr.gameObject;
                        break;
                    }
                }
            }

            if (parkObj != null)
            {
                foreach (var childPb in parkObj.GetComponentsInChildren<PropertyBuilding>(true))
                {
                    if (childPb.gameObject != parkObj)
                    {
                        Destroy(childPb);
                    }
                }
                foreach (var childInd in parkObj.GetComponentsInChildren<BuildingWorldIndicator>(true))
                {
                    if (childInd.gameObject != parkObj && childInd.transform.parent != parkObj.transform)
                    {
                        Destroy(childInd.gameObject);
                    }
                }
            }

            GameObject pwpRoot = GameObject.Find("ParkWaypoints");
            if (pwpRoot != null)
            {
                foreach (var childPb in pwpRoot.GetComponentsInChildren<PropertyBuilding>(true))
                {
                    Destroy(childPb);
                }
                foreach (var childInd in pwpRoot.GetComponentsInChildren<BuildingWorldIndicator>(true))
                {
                    Destroy(childInd.gameObject);
                }
            }

            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int registeredCount = 0;

            foreach (var t in allTransforms)
            {
                if (t == null) continue;

                // Check if this object represents a recognized individual building
                if (IsBuildingObject(t, out string friendlyName, out PropertyType pType, out double price, out double dailyRent, out double taxiFee, out double taxiFare, out string desc))
                {
                    // Ensure PropertyBuilding component
                    PropertyBuilding pb = t.GetComponent<PropertyBuilding>();
                    bool isNew = pb == null;
                    if (isNew)
                    {
                        pb = t.gameObject.AddComponent<PropertyBuilding>();
                        pb.SetConfig(friendlyName, pType, price, dailyRent, desc);
                        if (pType == PropertyType.TaxiStation)
                        {
                            pb.SetTaxiConfig(taxiFee, taxiFare);
                        }
                    }

                    if (defaultSelectionMaterial != null && pb.SelectionShaderMaterial == null)
                    {
                        pb.SetSelectionMaterial(defaultSelectionMaterial);
                    }

                    pb.EnsureCollider();
                    pb.EnsureWorldIndicator();
                    registeredCount++;
                }
            }

            // Also guarantee all pre-existing valid PropertyBuildings have active colliders
            PropertyBuilding[] existingPbs = UnityEngine.Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pb in existingPbs)
            {
                if (pb == null) continue;
                if (IsSectorOrContainer(pb.gameObject.name))
                {
                    Destroy(pb);
                    continue;
                }
                pb.EnsureCollider();
                pb.EnsureWorldIndicator();
                if (defaultSelectionMaterial != null && pb.SelectionShaderMaterial == null)
                {
                    pb.SetSelectionMaterial(defaultSelectionMaterial);
                }
            }

            // Ensure TennisMatchManager exists
            if (TennisMatchManager.Instance == null)
            {
                var tmm = UnityEngine.Object.FindAnyObjectByType<TennisMatchManager>();
                if (tmm == null)
                {
                    gameObject.AddComponent<TennisMatchManager>();
                }
            }

            // Ensure ParkAIManager exists
            if (EmpireBuilder.Traffic.ParkAIManager.Instance == null)
            {
                var pam = UnityEngine.Object.FindAnyObjectByType<EmpireBuilder.Traffic.ParkAIManager>();
                if (pam == null)
                {
                    GameObject pwp = GameObject.Find("ParkWaypoints");
                    if (pwp != null)
                    {
                        pwp.AddComponent<EmpireBuilder.Traffic.ParkAIManager>();
                    }
                    else
                    {
                        gameObject.AddComponent<EmpireBuilder.Traffic.ParkAIManager>();
                    }
                }
            }

            Debug.Log($"[BuildingInteractionManager] Initialized {registeredCount} interactive buildings across the city.");
        }

        public static bool IsBuildingObject(Transform t, out string friendlyName, out PropertyType pType, out double price, out double dailyRent, out double taxiFee, out double taxiFare, out string desc)
        {
            friendlyName = "";
            pType = PropertyType.Apartment;
            price = 25000;
            dailyRent = 850;
            taxiFee = 150;
            taxiFare = 450;
            desc = "A prominent real estate asset in Capital Kingdom.";

            if (t == null) return false;

            string n = t.name;

            // 1. Immediately reject sector folders, containers, UI canvas, and non-building scene roots
            if (IsSectorOrContainer(n)) return false;
            if (t.GetComponentInParent<Canvas>() != null) return false;

            // 2. Reject UI elements, roads, tiles, vehicles, characters, vegetation, props, particles
            if (n.Contains("Section") || n.Contains("Modal") || n.Contains("Button") ||
                n.Contains("Text") || n.Contains("Panel") || n.Contains("Header") ||
                n.Contains("Stats") || n.Contains("Title") || n.Contains("Card") ||
                n.Contains("Toast") || n.Contains("Image") || n.Contains("Placeholder"))
            {
                return false;
            }

            if (n.StartsWith("road_") || n.StartsWith("Road") || n.StartsWith("Tile") || n.StartsWith("Set_B_") ||
                n.StartsWith("Bush_") || n.StartsWith("Palm_") || n.StartsWith("Tree_") || n.StartsWith("Trash_") ||
                n.StartsWith("Spotlight_") || n.StartsWith("traffic_light_") || n.StartsWith("Car_") ||
                n.StartsWith("Vehicle_") || n.StartsWith("Train") || n.StartsWith("Rail") || n.StartsWith("Straight rail") ||
                n.StartsWith("Bus_Stop") || n.StartsWith("Signboard") || n.StartsWith("Billboard") ||
                n.StartsWith("Fountain") || n.StartsWith("Van") || n.StartsWith("Taxi") || n.StartsWith("AI Character") ||
                n.StartsWith("Park AI") || n.Contains("Park AI Character") ||
                n.StartsWith("Smoke") || n.StartsWith("Waypoint") || n.Contains("Marker") || n.Contains("Particle") ||
                n.Contains("Car") || n.Contains("Vehicle") || n.Contains("Train") || n.Contains("Rail") || n.Contains("Spawn Point"))
            {
                return false;
            }

            // 2.5 Exclude any sub-object or asset inside Park or ParkWaypoints (Only the root Park is interactive)
            Transform checkAncestor = t.parent;
            while (checkAncestor != null)
            {
                if (checkAncestor.name == "Park" || checkAncestor.CompareTag("Park") ||
                    checkAncestor.name == "ParkWaypoints" || checkAncestor.CompareTag("ParkWaypoints"))
                {
                    return false;
                }
                checkAncestor = checkAncestor.parent;
            }

            // 3. Do not register sub-nodes if an ancestor is already a registered or recognizable building
            Transform p = t.parent;
            while (p != null)
            {
                if (IsSectorOrContainer(p.name)) break;
                if (p.GetComponent<PropertyBuilding>() != null || IsKnownBuildingName(p.name))
                {
                    return false;
                }
                p = p.parent;
            }

            // 4. Check Unity GameObject Tags Assigned by User (Primary source of identification)
            string objectTag = t.tag;
            if (!string.IsNullOrEmpty(objectTag) && objectTag != "Untagged")
            {
                if (objectTag == "Rental Building")
                {
                    friendlyName = GetCleanName(n, "Tower Residences");
                    pType = PropertyType.RentalBuilding;
                    price = 95000;
                    dailyRent = 1800;
                    desc = "High-density residential high-rise. Purchase individual flats (25 flats available) to lease out for rent, or buy the entire building for massive daily revenue!";
                    return true;
                }
                if (objectTag == "Small Rental Building")
                {
                    friendlyName = GetCleanName(n, "Suburban Rental House");
                    pType = PropertyType.SmallRentalBuilding;
                    price = 28000;
                    dailyRent = 600;
                    desc = "Multi-unit residential property. Purchase individual flats (8 flats available) to lease out for rent, or buy the entire house!";
                    return true;
                }
                if (objectTag == "Restaurent" || objectTag == "Restaurant")
                {
                    friendlyName = GetCleanName(n, "Gourmet City Restaurant");
                    pType = PropertyType.Restaurant;
                    price = 45000;
                    dailyRent = 900;
                    desc = "Premier dining establishment. Visit to eat gourmet dishes or drink beverages to restore health and stamina, or acquire the restaurant!";
                    return true;
                }
                if (objectTag == "Cafe")
                {
                    friendlyName = GetCleanName(n, "Artisan Coffee Cafe");
                    pType = PropertyType.Cafe;
                    price = 32000;
                    dailyRent = 700;
                    desc = "Popular urban coffeehouse. Visit to drink roasted espresso or food to replenish health, or buy the cafe for continuous income!";
                    return true;
                }
                if (objectTag == "Texi Station" || objectTag == "Taxi Station")
                {
                    int num = n.Contains("(1)") ? 2 : 1;
                    friendlyName = $"City Central Taxi Station #{num}";
                    pType = PropertyType.TaxiStation;
                    price = 50000;
                    dailyRent = 1100;
                    taxiFee = 150;
                    taxiFare = 450;
                    desc = "Central taxi transit depot. Rent up to 8 taxis per day with roof markers to collect passenger fares on city roads, or purchase the station!";
                    return true;
                }
                if (objectTag == "Stadium")
                {
                    friendlyName = "Grand Olympic Stadium";
                    pType = PropertyType.Stadium;
                    price = 180000;
                    dailyRent = 3500;
                    desc = "Massive sports and entertainment colosseum. Buy the stadium to receive football match hosting rental requests and earn huge rewards!";
                    return true;
                }
                if (objectTag == "Factory")
                {
                    int num = n.Contains("(1)") ? 2 : 1;
                    friendlyName = $"Capital Industrial Manufacturing Plant #{num}";
                    pType = PropertyType.Factory;
                    price = 120000;
                    dailyRent = 2400;
                    desc = "Heavy manufacturing plant. Buy the factory for large-scale industrial output and periodic production revenue collections!";
                    return true;
                }
                if (objectTag == "Oil Station")
                {
                    int num = n.Contains("(1)") ? 2 : 1;
                    friendlyName = $"Metropolitan Oil & Petroleum Station #{num}";
                    pType = PropertyType.OilStation;
                    price = 85000;
                    dailyRent = 1900;
                    desc = "High-traffic fuel and petroleum station. Buy the oil station to earn regular revenue from citywide vehicle fuel sales!";
                    return true;
                }
                if (objectTag == "Hospital")
                {
                    friendlyName = "Capital General Hospital";
                    pType = PropertyType.Hospital;
                    price = 150000;
                    dailyRent = 2800;
                    desc = "Advanced metropolitan medical center. Visit anytime you are sick or low health for full medical treatment (restoring 100 HP), or buy the hospital!";
                    return true;
                }
                if (objectTag == "School")
                {
                    friendlyName = "Capital Public Academy";
                    pType = PropertyType.School;
                    price = 0;
                    dailyRent = 0;
                    desc = "Civic public school dedicated to city education. Public municipal facility (not available for private purchase or flat leasing).";
                    return true;
                }
                if (objectTag == "Police Station")
                {
                    friendlyName = "Metropolitan Police Department HQ";
                    pType = PropertyType.PoliceStation;
                    price = 0;
                    dailyRent = 0;
                    desc = "Municipal law enforcement headquarters ensuring city safety. Public facility (not available for private purchase or flat leasing).";
                    return true;
                }
                if (objectTag == "Tennis Court" || objectTag == "Tennis")
                {
                    int num = n.Contains("(1)") ? 2 : 1;
                    friendlyName = $"Capital Tennis Club Court #{num}";
                    pType = PropertyType.TennisCourt;
                    price = 45000;
                    dailyRent = 1500;
                    desc = "Championship tennis courts with professional lighting. Buy the tennis court to receive lucrative tournament match hosting requests and earn match fees!";
                    return true;
                }
                if (objectTag == "Park")
                {
                    friendlyName = "Grand Central Municipal Park";
                    pType = PropertyType.Park;
                    price = 200000;
                    dailyRent = 2500;
                    desc = "Expansive scenic public park with walking trails and garden attractions. Buy this prestigious park to earn visitor entry fees from citizens walking through!";
                    return true;
                }
            }

            // 5. Fallback Name-Based Matching (For any untagged structures)
            // 5.1 Tennis Court
            if (n.Contains("Tennis Court") || n.Contains("Tennis"))
            {
                int num = n.Contains("(1)") ? 2 : 1;
                friendlyName = $"Capital Tennis Club Court #{num}";
                pType = PropertyType.TennisCourt;
                price = 45000;
                dailyRent = 1500;
                desc = "Championship tennis courts with professional lighting. Buy the tennis court to receive lucrative tournament match hosting requests and earn match fees!";
                return true;
            }

            // 5.2 Park
            if (n.Contains("Park") && !n.Contains("ParkWaypoints"))
            {
                friendlyName = "Grand Central Municipal Park";
                pType = PropertyType.Park;
                price = 200000;
                dailyRent = 2500;
                desc = "Expansive scenic public park with walking trails and garden attractions. Buy this prestigious park to earn visitor entry fees from citizens walking through!";
                return true;
            }

            // 5.3 Taxi Station
            if (n.Contains("Texi Station") || n.Contains("Taxi Station"))
            {
                int num = n.Contains("(1)") ? 2 : 1;
                friendlyName = $"City Taxi Central Station #{num}";
                pType = PropertyType.TaxiStation;
                price = 35000;
                dailyRent = 1200;
                taxiFee = 150;
                taxiFare = 450;
                desc = "Central taxi transit depot. Rent a taxi for 1 day to work the streets and earn passenger fares, or buy the entire station for steady daily rent!";
                return true;
            }

            // 4.2 Stadium
            if (n.Contains("Stadium"))
            {
                friendlyName = "Grand Olympic Stadium";
                pType = PropertyType.Stadium;
                price = 150000;
                dailyRent = 5200;
                desc = "Massive sports and entertainment colosseum. Hosts championship games and international events generating enormous daily revenues.";
                return true;
            }

            // 4.3 Police Station
            if (n.Contains("Police Station") || n.Contains("Police_Station"))
            {
                friendlyName = "Metropolitan Police HQ";
                pType = PropertyType.Commercial;
                price = 50000;
                dailyRent = 1800;
                desc = "Central municipal security headquarters providing stable long-term civic leases and security returns.";
                return true;
            }

            // 4.4 Hospital
            if (n.Contains("Hospital"))
            {
                friendlyName = "Capital General Hospital";
                pType = PropertyType.Hospital;
                price = 95000;
                dailyRent = 3200;
                desc = "Advanced regional medical center with diagnostic wings, serving city residents with high healthcare dividends.";
                return true;
            }

            // 4.5 Restaurant / Cafe
            if (n.Contains("Cafe") || n.Contains("Restaurant"))
            {
                if (n.Contains("1")) friendlyName = "Downtown Espresso Cafe";
                else if (n.Contains("2")) friendlyName = "Sunset Bistro & Lounge";
                else friendlyName = "Metropolitan Coffee & Dining";

                pType = PropertyType.Restaurant;
                price = 22000;
                dailyRent = 780;
                desc = "Lively dining establishment with premier sidewalk seating, serving fine coffee and gourmet dishes to city shoppers.";
                return true;
            }

            // 4.6 Market / Shop
            if (n.Contains("Market") || n.Contains("Shop") || n.Contains("Store"))
            {
                friendlyName = "Fresh Town Supermarket";
                pType = PropertyType.Market;
                price = 18000;
                dailyRent = 650;
                desc = "High-traffic neighborhood retail and grocery market supplying fresh goods and everyday essentials.";
                return true;
            }

            // 4.7 Gas Station
            if (n.Contains("Gas Station") || n.Contains("Building_Gas Station"))
            {
                int num = n.Contains("(1)") ? 2 : 1;
                friendlyName = $"Express Service & Fuel Station #{num}";
                pType = PropertyType.Commercial;
                price = 28000;
                dailyRent = 950;
                desc = "Strategic automotive fueling depot and convenience mart at the intersection of major avenues.";
                return true;
            }

            // 4.8 School
            if (n.Contains("School"))
            {
                friendlyName = "Capital City Academy";
                pType = PropertyType.Commercial;
                price = 42000;
                dailyRent = 1350;
                desc = "Prestigious academic institution funded by public education grants and educational sponsorships.";
                return true;
            }

            // 4.9 Factory
            if (n.Contains("Factory") || n.Contains("Building_Factory"))
            {
                friendlyName = "Capital Industrial Factory";
                pType = PropertyType.Industrial;
                price = 85000;
                dailyRent = 2800;
                desc = "Heavy manufacturing plant driving metropolitan logistics and supply chain production.";
                return true;
            }

            // 4.10 Standalone Workshop (not inside Texi Station)
            if (n.Contains("Workshop") && (t.parent == null || !t.parent.name.Contains("Station")))
            {
                friendlyName = "Precision Machine Workshop";
                pType = PropertyType.Industrial;
                price = 30000;
                dailyRent = 1050;
                desc = "Mechanical repair depot and fabrication workspace servicing transit vehicles and equipment.";
                return true;
            }

            // 4.11 Apartments & High Rises
            if (n.Contains("Eco_Building_Terrace"))
            {
                friendlyName = "Eco Terrace Sky Suites";
                pType = PropertyType.Apartment;
                price = 45000;
                dailyRent = 1500;
                desc = "Terraced green residential high-rise featuring botanical rooftop patios and luxury suites.";
                return true;
            }
            if (n.Contains("TwistedTower"))
            {
                friendlyName = "Helix Twisted Tower Apartments";
                pType = PropertyType.Apartment;
                price = 68000;
                dailyRent = 2300;
                desc = "Iconic architectural marvel offering elite penthouse residences with panoramic city views.";
                return true;
            }
            if (n.Contains("Eco_Building_Slope"))
            {
                friendlyName = "Verdant Slope Modern Condos";
                pType = PropertyType.Apartment;
                price = 32000;
                dailyRent = 1050;
                desc = "Sleek contemporary condominiums with energy-efficient passive cooling and skyline balconies.";
                return true;
            }
            if (n.Contains("Eco_Building_Grid") || n.Contains("Eco_Building"))
            {
                friendlyName = "Metropolitan Plaza Apartments";
                pType = PropertyType.Apartment;
                price = 30000;
                dailyRent = 980;
                desc = "Centrally located multi-story residences offering reliable long-term tenant rent income.";
                return true;
            }

            // 4.12 Houses
            if (n.Contains("Rental House") || n.Contains("House") || n.Contains("Suburban") || n.Contains("Villa") || n.Contains("Cottage"))
            {
                friendlyName = "Suburban Family Residence";
                pType = PropertyType.House;
                price = 12000;
                dailyRent = 420;
                desc = "Charming single-family home with landscaped yard in a quiet residential district.";
                return true;
            }

            // 4.13 Blocks & Demo Blocks (Tiny Toony City)
            if (n.Contains("Demo_Block"))
            {
                int siblingIdx = t.GetSiblingIndex() + 1;
                friendlyName = $"Metropolitan Block #{siblingIdx} Apartments";
                pType = PropertyType.Apartment;
                price = 26000;
                dailyRent = 900;
                desc = "Multi-story urban apartment complex featuring spacious residential flats with terrace balconies and street-level shops.";
                return true;
            }

            return false;
        }

        private static bool IsKnownBuildingName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.Contains("Station") || name.Contains("Stadium") || name.Contains("Hospital") ||
                   name.Contains("Cafe") || name.Contains("Restaurant") || name.Contains("Market") ||
                   name.Contains("Shop") || name.Contains("School") || name.Contains("Factory") ||
                   name.Contains("Eco_Building") || name.Contains("House") || name.Contains("Demo_Block") ||
                   name.Contains("Park") || name.Contains("Tennis");
        }

        private static string GetCleanName(string raw, string fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;
            string clean = raw.Replace('_', ' ').Trim();
            if (clean.StartsWith("Building ")) clean = clean.Substring(9);
            return clean;
        }
        #endregion

        #region Touch Input Handling (Mobile / Touch Devices)
        private void HandleTouchInput()
        {
            if (Input.touchCount != 1)
            {
                if (Input.touchCount > 1)
                {
                    // Multi-touch pinch zoom in progress - cancel building tap tracking
                    isPointerTracking = false;
                }
                return;
            }

            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                pointerStartedOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                if (pointerStartedOverUI)
                {
                    isPointerTracking = false;
                    return;
                }

                pointerDownPosition = touch.position;
                isPointerTracking = true;
            }
            else if (touch.phase == TouchPhase.Ended && isPointerTracking)
            {
                isPointerTracking = false;

                if (pointerStartedOverUI) return;

                float moveDistance = Vector2.Distance(pointerDownPosition, touch.position);
                if (moveDistance <= tapMaxPixelDistance)
                {
                    // Valid single-finger tap!
                    ProcessRaycastAtScreenPoint(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Canceled)
            {
                isPointerTracking = false;
            }
        }
        #endregion

        #region Mouse Input Handling (Editor & Desktop)
        private void HandleMouseInput()
        {
            if (Input.touchCount > 0) return; // Prioritize touches

            if (Input.GetMouseButtonDown(0))
            {
                pointerStartedOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                if (pointerStartedOverUI)
                {
                    isPointerTracking = false;
                    return;
                }

                pointerDownPosition = Input.mousePosition;
                isPointerTracking = true;
            }
            else if (Input.GetMouseButtonUp(0) && isPointerTracking)
            {
                isPointerTracking = false;

                if (pointerStartedOverUI) return;

                float moveDistance = Vector2.Distance(pointerDownPosition, (Vector2)Input.mousePosition);
                if (moveDistance <= tapMaxPixelDistance)
                {
                    // Valid mouse click without dragging!
                    ProcessRaycastAtScreenPoint(Input.mousePosition);
                }
            }
        }
        #endregion

        private void ProcessRaycastAtScreenPoint(Vector2 screenPoint)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask))
            {
                PropertyBuilding hitBuilding = null;

                // Check if hit object is inside the Park area
                Transform trCheck = hit.collider.transform;
                while (trCheck != null)
                {
                    if (trCheck.name == "Park" || trCheck.CompareTag("Park"))
                    {
                        hitBuilding = trCheck.GetComponent<PropertyBuilding>();
                        break;
                    }
                    if (trCheck.name == "ParkWaypoints" || trCheck.CompareTag("ParkWaypoints"))
                    {
                        GameObject pRoot = GameObject.Find("Park");
                        if (pRoot != null) hitBuilding = pRoot.GetComponent<PropertyBuilding>();
                        break;
                    }
                    trCheck = trCheck.parent;
                }

                if (hitBuilding == null)
                {
                    hitBuilding = hit.collider.GetComponentInParent<PropertyBuilding>();
                }

                // If hitBuilding was found on a sector container, ignore it
                if (hitBuilding != null && IsSectorOrContainer(hitBuilding.gameObject.name))
                {
                    hitBuilding = null;
                }

                if (hitBuilding == null)
                {
                    Transform curr = hit.collider.transform;
                    while (curr != null)
                    {
                        if (IsSectorOrContainer(curr.name)) break;

                        if (IsBuildingObject(curr, out string fName, out PropertyType pType, out double pr, out double rent, out double tFee, out double tFare, out string d))
                        {
                            hitBuilding = curr.GetComponent<PropertyBuilding>() ?? curr.gameObject.AddComponent<PropertyBuilding>();
                            hitBuilding.SetConfig(fName, pType, pr, rent, d);
                            if (pType == PropertyType.TaxiStation) hitBuilding.SetTaxiConfig(tFee, tFare);
                            if (defaultSelectionMaterial != null && hitBuilding.SelectionShaderMaterial == null) hitBuilding.SetSelectionMaterial(defaultSelectionMaterial);
                            hitBuilding.EnsureCollider();
                            hitBuilding.EnsureWorldIndicator();
                            break;
                        }
                        curr = curr.parent;
                    }
                }
                if (hitBuilding != null && !IsSectorOrContainer(hitBuilding.gameObject.name))
                {
                    SelectBuilding(hitBuilding);
                    return;
                }
            }

            // Tapped empty space or non-building object: deselect
            DeselectCurrent();
        }

        public void SelectBuilding(PropertyBuilding building)
        {
            if (building == null)
            {
                DeselectCurrent();
                return;
            }

            if (currentSelectedBuilding == building)
            {
                // Already selected, re-open UI if closed
                if (detailModalUI != null)
                {
                    detailModalUI.Open(building);
                }
                return;
            }

            // Deselect previous
            if (currentSelectedBuilding != null)
            {
                currentSelectedBuilding.Deselect();
            }

            currentSelectedBuilding = building;
            currentSelectedBuilding.Select(defaultSelectionMaterial);

            if (detailModalUI == null)
            {
                detailModalUI = FindObjectOfType<PropertyDetailModalUI>(true);
            }

            if (detailModalUI != null)
            {
                detailModalUI.Open(building);
            }
        }

        public void DeselectCurrent()
        {
            if (currentSelectedBuilding != null)
            {
                currentSelectedBuilding.Deselect();
                currentSelectedBuilding = null;
            }

            if (detailModalUI != null)
            {
                detailModalUI.Close();
            }
        }

        public void SetDefaultSelectionMaterial(Material mat)
        {
            defaultSelectionMaterial = mat;
        }
    }
}
