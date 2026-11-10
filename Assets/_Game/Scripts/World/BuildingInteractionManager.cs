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
                    "Assets/characterEffects/shaders/ghostEffect/ghostMaterial.mat"
                );
            }
#endif

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
        /// Scans all game objects in the city and attaches/configures PropertyBuilding
        /// and BoxColliders so that every house, apartment, restaurant, stadium, and taxi station
        /// is fully touchable and interactive.
        /// </summary>
        public void InitializeCityBuildings()
        {
            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int registeredCount = 0;

            foreach (var t in allTransforms)
            {
                if (t == null) continue;

                // Check if this object represents a recognized building
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
                    registeredCount++;
                }
            }

            // Also guarantee all pre-existing PropertyBuildings have active colliders
            PropertyBuilding[] existingPbs = UnityEngine.Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pb in existingPbs)
            {
                if (pb == null) continue;
                pb.EnsureCollider();
                if (defaultSelectionMaterial != null && pb.SelectionShaderMaterial == null)
                {
                    pb.SetSelectionMaterial(defaultSelectionMaterial);
                }
            }

            Debug.Log($"[BuildingInteractionManager] Initialized {registeredCount} interactive buildings across the city (Total PropertyBuildings: {existingPbs.Length}).");
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

            // Ignore vehicles, roads, rails, trees, props, UI
            string n = t.name;
            string parentName = t.parent != null ? t.parent.name : "";

            if (n.StartsWith("road_") || n.StartsWith("Tile") || n.StartsWith("Set_B_") || n.StartsWith("Bush_") ||
                n.StartsWith("Palm_") || n.StartsWith("Tree_") || n.StartsWith("Trash_") || n.StartsWith("Spotlight_") ||
                n.StartsWith("traffic_light_") || n.StartsWith("Car_") || n.StartsWith("Vehicle_") || n.StartsWith("Train") ||
                n.StartsWith("Waypoint") || n.StartsWith("RoadNetwork") || n.StartsWith("Rail") || n.Contains("Canvas") ||
                n.Contains("Camera") || n.Contains("Light") || n.Contains("Volume"))
            {
                return false;
            }

            // Only inspect roots or direct children of sectors/landmarks
            bool isEligibleHierarchy = t.parent == null ||
                parentName.Contains("Hotel Zone") ||
                parentName.Contains("Demonstration") ||
                parentName.Contains("Station") ||
                parentName.Contains("Buildings") ||
                parentName.Contains("Building") ||
                t.GetComponent<PropertyBuilding>() != null;

            if (!isEligibleHierarchy) return false;

            // 1. Taxi Station
            if (n.Contains("Texi Station") || n.Contains("Taxi Station") || n.Equals("Texi Station"))
            {
                friendlyName = "City Taxi Central Station";
                pType = PropertyType.TaxiStation;
                price = 35000;
                dailyRent = 1200;
                taxiFee = 150;
                taxiFare = 450;
                desc = "Central taxi transit depot. Rent a taxi for 1 day to work the streets and earn passenger fares, or buy the entire station for steady daily rent!";
                return true;
            }

            // 2. Stadium
            if (n.Contains("Stadium") || n.Contains("Building_Stadium"))
            {
                friendlyName = "Grand Olympic Stadium";
                pType = PropertyType.Stadium;
                price = 150000;
                dailyRent = 5200;
                desc = "Massive sports and entertainment colosseum. Hosts championship games and international events generating enormous daily revenues.";
                return true;
            }

            // 3. Police Station
            if (n.Contains("Police Station") || n.Contains("Police_Station"))
            {
                friendlyName = "Metropolitan Police HQ";
                pType = PropertyType.Commercial;
                price = 50000;
                dailyRent = 1800;
                desc = "Central municipal security headquarters providing stable long-term civic leases and security returns.";
                return true;
            }

            // 4. Hospital
            if (n.Contains("Hospital"))
            {
                friendlyName = "Capital General Hospital";
                pType = PropertyType.Hospital;
                price = 95000;
                dailyRent = 3200;
                desc = "Advanced regional medical center with diagnostic wings, serving city residents with high healthcare dividends.";
                return true;
            }

            // 5. Restaurant / Cafe
            if (n.Contains("Cafe") || n.Contains("Restaurant"))
            {
                friendlyName = n.Contains("2") ? "Sunset Bistro & Lounge" : "Downtown Espresso Cafe";
                pType = PropertyType.Restaurant;
                price = 22000;
                dailyRent = 780;
                desc = "Lively dining establishment with premier sidewalk seating, serving fine coffee and gourmet dishes to city shoppers.";
                return true;
            }

            // 6. Market / Shop
            if (n.Contains("Market") || n.Contains("Shop") || n.Contains("Store"))
            {
                friendlyName = "Fresh Town Supermarket";
                pType = PropertyType.Market;
                price = 18000;
                dailyRent = 650;
                desc = "High-traffic neighborhood retail and grocery market supplying fresh goods and everyday essentials.";
                return true;
            }

            // 7. Gas Station
            if (n.Contains("Gas Station") || n.Contains("Building_Gas Station"))
            {
                friendlyName = "Express Service & Fuel Station";
                pType = PropertyType.Commercial;
                price = 28000;
                dailyRent = 950;
                desc = "Strategic automotive fueling depot and convenience mart at the intersection of major avenues.";
                return true;
            }

            // 8. School
            if (n.Contains("School"))
            {
                friendlyName = "Capital City Academy";
                pType = PropertyType.Commercial;
                price = 42000;
                dailyRent = 1350;
                desc = "Prestigious academic institution funded by public education grants and educational sponsorships.";
                return true;
            }

            // 9. Factory
            if (n.Contains("Factory") || n.Contains("Building_Factory"))
            {
                friendlyName = "Capital Industrial Factory";
                pType = PropertyType.Industrial;
                price = 85000;
                dailyRent = 2800;
                desc = "Heavy manufacturing plant driving metropolitan logistics and supply chain production.";
                return true;
            }

            // 10. Workshop
            if (n.Contains("Workshop"))
            {
                friendlyName = "Precision Machine Workshop";
                pType = PropertyType.Industrial;
                price = 30000;
                dailyRent = 1050;
                desc = "Mechanical repair depot and fabrication workspace servicing transit vehicles and equipment.";
                return true;
            }

            // 11. Apartments & High Rises
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

            // 12. Houses
            if (n.Contains("House") || n.Contains("Suburban") || n.Contains("Villa") || n.Contains("Cottage"))
            {
                friendlyName = "Suburban Family Residence";
                pType = PropertyType.House;
                price = 12000;
                dailyRent = 420;
                desc = "Charming single-family home with landscaped yard in a quiet residential district.";
                return true;
            }

            // 13. Blocks & Demo Blocks (Tiny Toony City)
            if (n.Contains("Demo_Block") || n.Contains("Block") || n.Contains("Bocks"))
            {
                int siblingIdx = t.GetSiblingIndex() + 1;
                friendlyName = $"Metropolitan Block #{siblingIdx} Apartments";
                pType = PropertyType.Apartment;
                price = 26000;
                dailyRent = 900;
                desc = "Multi-story urban apartment complex featuring spacious residential flats with terrace balconies and street-level shops.";
                return true;
            }

            // 14. Hotel Zone Buildings
            if (n.Contains("Hotel") || parentName.Contains("Hotel Zone"))
            {
                int siblingIdx = t.GetSiblingIndex() + 1;
                friendlyName = $"Grand Royal Hotel & Suites #{siblingIdx}";
                pType = PropertyType.Commercial;
                price = 65000;
                dailyRent = 2200;
                desc = "Luxurious metropolitan hotel complex featuring executive suites and residential flats for rent.";
                return true;
            }

            // 15. Demonstration District Buildings
            if (parentName.Contains("Demonstration") || n.Contains("Demonstration"))
            {
                int siblingIdx = t.GetSiblingIndex() + 1;
                friendlyName = $"Capital Plaza District #{siblingIdx}";
                pType = PropertyType.Apartment;
                price = 32000;
                dailyRent = 1100;
                desc = "Modern commercial and residential district with multiple apartments and units for rent.";
                return true;
            }

            // 16. General Buildings in Sector
            if (n.Contains("Building") || parentName.Contains("Buildings"))
            {
                int siblingIdx = t.GetSiblingIndex() + 1;
                friendlyName = $"Capital City Building #{siblingIdx}";
                pType = PropertyType.Apartment;
                price = 28000;
                dailyRent = 950;
                desc = "Prime city real estate property featuring buyable flats and reliable daily rental income.";
                return true;
            }

            return false;
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
                PropertyBuilding hitBuilding = hit.collider.GetComponentInParent<PropertyBuilding>();
                if (hitBuilding == null)
                {
                    Transform curr = hit.collider.transform;
                    while (curr != null)
                    {
                        if (IsBuildingObject(curr, out string fName, out PropertyType pType, out double pr, out double rent, out double tFee, out double tFare, out string d))
                        {
                            hitBuilding = curr.GetComponent<PropertyBuilding>() ?? curr.gameObject.AddComponent<PropertyBuilding>();
                            hitBuilding.SetConfig(fName, pType, pr, rent, d);
                            if (pType == PropertyType.TaxiStation) hitBuilding.SetTaxiConfig(tFee, tFare);
                            if (defaultSelectionMaterial != null && hitBuilding.SelectionShaderMaterial == null) hitBuilding.SetSelectionMaterial(defaultSelectionMaterial);
                            break;
                        }
                        curr = curr.parent;
                    }
                }
                if (hitBuilding != null)
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
