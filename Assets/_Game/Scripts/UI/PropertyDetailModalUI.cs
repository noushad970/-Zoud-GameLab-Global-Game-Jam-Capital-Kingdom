using System;
using System.Collections;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    /// <summary>
    /// Modal inspector dialog displayed when a player taps/clicks on any building in the city.
    /// Shows building details, purchase options, daily rent income,
    /// and special taxi rental / fare collection mechanics for the Taxi Station.
    /// Can be wired in Inspector OR dynamically built on the Canvas automatically.
    /// </summary>
    public class PropertyDetailModalUI : MonoBehaviour
    {
        public static PropertyDetailModalUI Instance { get; private set; }

        [Header("Main Panel Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform modalCardTransform;
        [SerializeField] private Button overlayButton;

        [Header("Building Header Labels")]
        [SerializeField] private TextMeshProUGUI propertyTitleText;
        [SerializeField] private TextMeshProUGUI categoryBadgeText;
        [SerializeField] private Image categoryBadgeBg;
        [SerializeField] private TextMeshProUGUI statusBadgeText;
        [SerializeField] private Image statusBadgeBg;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Financial Stats")]
        [SerializeField] private TextMeshProUGUI priceValueText;
        [SerializeField] private TextMeshProUGUI dailyIncomeValueText;

        [Header("Standard Actions")]
        [SerializeField] private Button buyPropertyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;
        [SerializeField] private Image buyButtonBg;
        [SerializeField] private Button collectRentButton;
        [SerializeField] private TextMeshProUGUI collectRentButtonText;
        [SerializeField] private Button closeButton;

        [Header("Flat / Unit Section (Special)")]
        [SerializeField] private GameObject flatSectionRoot;
        [SerializeField] private TextMeshProUGUI flatsCountText;
        [SerializeField] private TextMeshProUGUI flatIncomeText;
        [SerializeField] private Button buyFlatButton;
        [SerializeField] private TextMeshProUGUI buyFlatButtonText;
        [SerializeField] private Button giveRentButton;
        [SerializeField] private TextMeshProUGUI giveRentButtonText;

        [Header("Cafe & Restaurant Dining Section (Special)")]
        [SerializeField] private GameObject restaurantSectionRoot;
        [SerializeField] private TextMeshProUGUI diningStatusText;
        [SerializeField] private Button drinkButton;
        [SerializeField] private TextMeshProUGUI drinkButtonText;
        [SerializeField] private Button eatButton;
        [SerializeField] private TextMeshProUGUI eatButtonText;

        [Header("Taxi Station Section (Special)")]
        [SerializeField] private GameObject taxiSectionRoot;
        [SerializeField] private TextMeshProUGUI taxiRentalFeeText;
        [SerializeField] private TextMeshProUGUI taxiFareRevenueText;
        [SerializeField] private TextMeshProUGUI taxiNetProfitText;
        [SerializeField] private TextMeshProUGUI taxiFleetStatusText;
        [SerializeField] private Button rentTaxiButton;
        [SerializeField] private TextMeshProUGUI rentTaxiButtonText;
        [SerializeField] private Image rentTaxiButtonBg;

        [Header("Stadium Section (Special)")]
        [SerializeField] private GameObject stadiumSectionRoot;
        [SerializeField] private TextMeshProUGUI stadiumStatsText;
        [SerializeField] private Button requestMatchButton;
        [SerializeField] private TextMeshProUGUI requestMatchButtonText;

        [Header("Hospital Healthcare Section (Special)")]
        [SerializeField] private GameObject hospitalSectionRoot;
        [SerializeField] private TextMeshProUGUI hospitalStatusText;
        [SerializeField] private Button treatmentButton;
        [SerializeField] private TextMeshProUGUI treatmentButtonText;
        [SerializeField] private Image treatmentButtonBg;

        [Header("Factory Industrial Section (Special)")]
        [SerializeField] private GameObject factorySectionRoot;
        [SerializeField] private TextMeshProUGUI factoryStatusText;
        [SerializeField] private Button collectProductionButton;
        [SerializeField] private TextMeshProUGUI collectProductionButtonText;
        [SerializeField] private Image collectProductionButtonBg;

        [Header("Oil & Petroleum Station Section (Special)")]
        [SerializeField] private GameObject oilSectionRoot;
        [SerializeField] private TextMeshProUGUI oilStatusText;
        [SerializeField] private Button collectOilButton;
        [SerializeField] private TextMeshProUGUI collectOilButtonText;
        [SerializeField] private Image collectOilButtonBg;

        [Header("Park Visitor Operations (Special)")]
        [SerializeField] private GameObject parkSectionRoot;
        [SerializeField] private TextMeshProUGUI parkStatusText;
        [SerializeField] private Button collectParkButton;
        [SerializeField] private TextMeshProUGUI collectParkButtonText;
        [SerializeField] private Image collectParkButtonBg;

        [Header("Building Maintenance Section")]
        [SerializeField] private GameObject maintenanceSectionRoot;
        [SerializeField] private TextMeshProUGUI maintenanceReasonText;
        [SerializeField] private Button payMaintenanceButton;
        [SerializeField] private TextMeshProUGUI payMaintenanceButtonText;
        [SerializeField] private Image payMaintenanceButtonBg;

        [Header("Feedback Toast")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private TextMeshProUGUI toastMessageText;

        [Header("UI Sprites & Assets")]
        [SerializeField] private Sprite roundedBoxSprite;

        private PropertyBuilding currentBuilding;
        private Coroutine toastCoroutine;
        private Canvas targetCanvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            targetCanvas = GetComponentInParent<Canvas>() ?? FindObjectOfType<Canvas>();

            // If panelRoot is not assigned in Inspector, automatically generate the modern UI!
            if (panelRoot == null)
            {
                BuildDynamicModalUI();
            }

            EnsureBindings();
        }

        private void Start()
        {
            AttachButtonListeners();

            if (toastRoot != null)
            {
                toastRoot.SetActive(false);
            }

            // Initially closed until a building is tapped
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (currentBuilding != null)
            {
                currentBuilding.OnStateChanged -= HandleBuildingStateChanged;
            }
        }

        private void EnsureBindings()
        {
            if (panelRoot == null && transform.childCount > 0)
            {
                panelRoot = transform.GetChild(0).gameObject;
            }

            Transform card = transform.Find("ModalCard");
            if (card == null && panelRoot != null)
            {
                card = panelRoot.transform.Find("ModalCard") ?? panelRoot.transform;
            }

            if (flatSectionRoot == null && card != null)
            {
                var t = card.Find("FlatSection");
                if (t != null) flatSectionRoot = t.gameObject;
            }
            if (flatSectionRoot != null)
            {
                if (flatsCountText == null)
                    flatsCountText = flatSectionRoot.transform.Find("FlatsCount")?.GetComponent<TextMeshProUGUI>();
                if (flatIncomeText == null)
                    flatIncomeText = flatSectionRoot.transform.Find("FlatIncome")?.GetComponent<TextMeshProUGUI>();
                if (buyFlatButton == null)
                    buyFlatButton = flatSectionRoot.transform.Find("BuyFlatButton")?.GetComponent<Button>();
                if (buyFlatButtonText == null && buyFlatButton != null)
                    buyFlatButtonText = buyFlatButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (giveRentButton == null)
                    giveRentButton = flatSectionRoot.transform.Find("GiveRentButton")?.GetComponent<Button>();
                if (giveRentButtonText == null && giveRentButton != null)
                    giveRentButtonText = giveRentButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (restaurantSectionRoot == null && card != null)
            {
                var t = card.Find("RestaurantSection");
                if (t != null) restaurantSectionRoot = t.gameObject;
            }
            if (restaurantSectionRoot != null)
            {
                if (diningStatusText == null)
                    diningStatusText = restaurantSectionRoot.transform.Find("DiningStatus")?.GetComponent<TextMeshProUGUI>();
                if (drinkButton == null)
                    drinkButton = restaurantSectionRoot.transform.Find("DrinkButton")?.GetComponent<Button>();
                if (drinkButtonText == null && drinkButton != null)
                    drinkButtonText = drinkButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (eatButton == null)
                    eatButton = restaurantSectionRoot.transform.Find("EatButton")?.GetComponent<Button>();
                if (eatButtonText == null && eatButton != null)
                    eatButtonText = eatButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (stadiumSectionRoot == null && card != null)
            {
                var t = card.Find("StadiumSection");
                if (t != null) stadiumSectionRoot = t.gameObject;
            }
            if (stadiumSectionRoot != null)
            {
                if (stadiumStatsText == null)
                    stadiumStatsText = stadiumSectionRoot.transform.Find("StadiumStats")?.GetComponent<TextMeshProUGUI>();
                if (requestMatchButton == null)
                    requestMatchButton = stadiumSectionRoot.transform.Find("RequestMatchButton")?.GetComponent<Button>();
                if (requestMatchButtonText == null && requestMatchButton != null)
                    requestMatchButtonText = requestMatchButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (hospitalSectionRoot == null && card != null)
            {
                var t = card.Find("HospitalSection");
                if (t != null) hospitalSectionRoot = t.gameObject;
            }
            if (hospitalSectionRoot != null)
            {
                if (hospitalStatusText == null)
                    hospitalStatusText = hospitalSectionRoot.transform.Find("HospitalStatus")?.GetComponent<TextMeshProUGUI>();
                if (treatmentButton == null)
                    treatmentButton = hospitalSectionRoot.transform.Find("TreatmentButton")?.GetComponent<Button>();
                if (treatmentButtonText == null && treatmentButton != null)
                    treatmentButtonText = treatmentButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (factorySectionRoot == null && card != null)
            {
                var t = card.Find("FactorySection");
                if (t != null) factorySectionRoot = t.gameObject;
            }
            if (factorySectionRoot != null)
            {
                if (factoryStatusText == null)
                    factoryStatusText = factorySectionRoot.transform.Find("FactoryStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectProductionButton == null)
                    collectProductionButton = factorySectionRoot.transform.Find("CollectProductionButton")?.GetComponent<Button>();
                if (collectProductionButtonText == null && collectProductionButton != null)
                    collectProductionButtonText = collectProductionButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (oilSectionRoot == null && card != null)
            {
                var t = card.Find("OilSection");
                if (t != null) oilSectionRoot = t.gameObject;
            }
            if (oilSectionRoot != null)
            {
                if (oilStatusText == null)
                    oilStatusText = oilSectionRoot.transform.Find("OilStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectOilButton == null)
                    collectOilButton = oilSectionRoot.transform.Find("CollectOilButton")?.GetComponent<Button>();
                if (collectOilButtonText == null && collectOilButton != null)
                    collectOilButtonText = collectOilButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (parkSectionRoot == null && card != null)
            {
                var t = card.Find("ParkSection");
                if (t != null) parkSectionRoot = t.gameObject;
            }
            if (parkSectionRoot != null)
            {
                if (parkStatusText == null)
                    parkStatusText = parkSectionRoot.transform.Find("ParkStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectParkButton == null)
                    collectParkButton = parkSectionRoot.transform.Find("CollectParkButton")?.GetComponent<Button>();
                if (collectParkButtonText == null && collectParkButton != null)
                    collectParkButtonText = collectParkButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (maintenanceSectionRoot == null && card != null)
            {
                var t = card.Find("MaintenanceSection");
                if (t != null) maintenanceSectionRoot = t.gameObject;
            }
            if (maintenanceSectionRoot != null)
            {
                if (maintenanceReasonText == null)
                    maintenanceReasonText = maintenanceSectionRoot.transform.Find("MaintenanceReason")?.GetComponent<TextMeshProUGUI>();
                if (payMaintenanceButton == null)
                    payMaintenanceButton = maintenanceSectionRoot.transform.Find("PayMaintenanceButton")?.GetComponent<Button>();
                if (payMaintenanceButtonText == null && payMaintenanceButton != null)
                    payMaintenanceButtonText = payMaintenanceButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            }

            if (collectRentButton == null && card != null)
            {
                collectRentButton = card.Find("CollectRentButton")?.GetComponent<Button>();
                if (collectRentButton != null && collectRentButtonText == null)
                {
                    collectRentButtonText = collectRentButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                }
            }

            if (buyPropertyButton == null && card != null)
            {
                buyPropertyButton = card.Find("BuyPropertyButton")?.GetComponent<Button>();
                if (buyPropertyButton != null && buyButtonText == null)
                {
                    buyButtonText = buyPropertyButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        private void AttachButtonListeners()
        {
            EnsureBindings();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }
            if (overlayButton != null)
            {
                overlayButton.onClick.RemoveAllListeners();
                overlayButton.onClick.AddListener(Close);
            }
            if (buyPropertyButton != null)
            {
                buyPropertyButton.onClick.RemoveAllListeners();
                buyPropertyButton.onClick.AddListener(OnBuyClicked);
            }
            if (rentTaxiButton != null)
            {
                rentTaxiButton.onClick.RemoveAllListeners();
                rentTaxiButton.onClick.AddListener(OnRentTaxiClicked);
            }
            if (collectRentButton != null)
            {
                collectRentButton.onClick.RemoveAllListeners();
                collectRentButton.onClick.AddListener(OnCollectRentClicked);
            }
            if (buyFlatButton != null)
            {
                buyFlatButton.onClick.RemoveAllListeners();
                buyFlatButton.onClick.AddListener(OnBuyFlatClicked);
            }
            if (giveRentButton != null)
            {
                giveRentButton.onClick.RemoveAllListeners();
                giveRentButton.onClick.AddListener(OnGiveRentClicked);
            }
            if (drinkButton != null)
            {
                drinkButton.onClick.RemoveAllListeners();
                drinkButton.onClick.AddListener(OnDrinkClicked);
            }
            if (eatButton != null)
            {
                eatButton.onClick.RemoveAllListeners();
                eatButton.onClick.AddListener(OnEatClicked);
            }
            if (requestMatchButton != null)
            {
                requestMatchButton.onClick.RemoveAllListeners();
                requestMatchButton.onClick.AddListener(OnRequestMatchClicked);
            }
            if (treatmentButton != null)
            {
                treatmentButton.onClick.RemoveAllListeners();
                treatmentButton.onClick.AddListener(OnTreatmentClicked);
            }
            if (collectProductionButton != null)
            {
                collectProductionButton.onClick.RemoveAllListeners();
                collectProductionButton.onClick.AddListener(OnCollectProductionClicked);
            }
            if (collectOilButton != null)
            {
                collectOilButton.onClick.RemoveAllListeners();
                collectOilButton.onClick.AddListener(OnCollectOilClicked);
            }
            if (collectParkButton != null)
            {
                collectParkButton.onClick.RemoveAllListeners();
                collectParkButton.onClick.AddListener(OnCollectParkRevenueClicked);
            }
            if (payMaintenanceButton != null)
            {
                payMaintenanceButton.onClick.RemoveAllListeners();
                payMaintenanceButton.onClick.AddListener(OnPayMaintenanceClicked);
            }
        }

        public void Open(PropertyBuilding building)
        {
            if (building == null)
            {
                Close();
                return;
            }

            if (currentBuilding != null)
            {
                currentBuilding.OnStateChanged -= HandleBuildingStateChanged;
            }

            currentBuilding = building;
            currentBuilding.OnStateChanged += HandleBuildingStateChanged;

            AttachButtonListeners();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            GameSoundManager.Instance?.PlayClick(building.transform.position);

            RefreshUI();
        }

        public void Close()
        {
            GameSoundManager.Instance?.PlayClick();

            if (currentBuilding != null)
            {
                currentBuilding.OnStateChanged -= HandleBuildingStateChanged;
                // Deselect in interaction manager if currently selected
                if (BuildingInteractionManager.Instance != null &&
                    BuildingInteractionManager.Instance.CurrentSelectedBuilding == currentBuilding)
                {
                    BuildingInteractionManager.Instance.DeselectCurrent();
                }
                currentBuilding = null;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void HandleBuildingStateChanged(PropertyBuilding building)
        {
            if (building == currentBuilding)
            {
                RefreshUI();
            }
        }

        public void RefreshUI()
        {
            if (currentBuilding == null) return;

            // 1. Title & Description
            if (propertyTitleText != null)
            {
                propertyTitleText.text = currentBuilding.PropertyName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = currentBuilding.Description;
            }

            // 2. Category Badge
            if (categoryBadgeText != null)
            {
                categoryBadgeText.text = currentBuilding.PropertyType.ToString().ToUpper();
            }

            if (categoryBadgeBg != null)
            {
                categoryBadgeBg.color = GetCategoryColor(currentBuilding.PropertyType);
            }

            // 3. Status Badge (Owned vs For Sale vs Flats Owned vs Civic)
            if (statusBadgeText != null)
            {
                if (currentBuilding.IsCivic)
                {
                    statusBadgeText.text = "CIVIC PUBLIC FACILITY";
                }
                else if (currentBuilding.IsOwned)
                {
                    statusBadgeText.text = "OWNED BY YOU";
                }
                else if (currentBuilding.FlatsOwned > 0)
                {
                    statusBadgeText.text = $"YOU OWN {currentBuilding.FlatsOwned}/{currentBuilding.TotalFlats} FLATS";
                }
                else
                {
                    statusBadgeText.text = "AVAILABLE FOR SALE";
                }
            }

            if (statusBadgeBg != null)
            {
                if (currentBuilding.IsCivic)
                {
                    statusBadgeBg.color = new Color(0.35f, 0.45f, 0.60f, 1f); // Civic Slate Navy
                }
                else if (currentBuilding.IsOwned)
                {
                    statusBadgeBg.color = new Color(0.18f, 0.75f, 0.44f, 1f); // Emerald Green
                }
                else if (currentBuilding.FlatsOwned > 0)
                {
                    statusBadgeBg.color = new Color(0.2f, 0.62f, 0.95f, 1f); // Sky Blue
                }
                else
                {
                    statusBadgeBg.color = new Color(0.96f, 0.65f, 0.15f, 1f); // Warm Gold
                }
            }

            // 4. Financial Stats
            if (priceValueText != null)
            {
                priceValueText.text = currentBuilding.IsCivic ? "N/A (CIVIC)" : $"${currentBuilding.PurchasePrice:N0}";
            }

            if (dailyIncomeValueText != null)
            {
                dailyIncomeValueText.text = currentBuilding.IsCivic ? "PUBLIC UTILITY" : $"+${currentBuilding.DailyRentIncome:N0} / day";
            }

            // 5. Buy Property Button State
            double currentCash = GetPlayerCash();
            double effectiveBuildingPrice = currentBuilding.PurchasePrice;
            if (currentBuilding.HasFlats && currentBuilding.FlatsOwned > 0 && !currentBuilding.IsOwned)
            {
                effectiveBuildingPrice = System.Math.Max(0, currentBuilding.PurchasePrice - (currentBuilding.FlatsOwned * currentBuilding.FlatPurchasePrice));
            }
            bool canAfford = currentCash >= effectiveBuildingPrice;

            if (buyPropertyButton != null)
            {
                if (currentBuilding.IsCivic)
                {
                    buyPropertyButton.interactable = false;
                    if (buyButtonText != null) buyButtonText.text = "CIVIC FACILITY (NOT FOR SALE)";
                    if (buyButtonBg != null) buyButtonBg.color = new Color(0.25f, 0.30f, 0.40f, 0.7f);
                }
                else if (currentBuilding.IsOwned)
                {
                    buyPropertyButton.interactable = false;
                    if (buyButtonText != null) buyButtonText.text = "ALREADY OWNED";
                    if (buyButtonBg != null) buyButtonBg.color = new Color(0.3f, 0.35f, 0.45f, 0.7f);
                }
                else
                {
                    buyPropertyButton.interactable = canAfford;
                    if (buyButtonText != null)
                    {
                        string label = currentBuilding.FlatsOwned > 0
                            ? $"BUY REST OF BUILDING (${effectiveBuildingPrice:N0})"
                            : $"BUY PROPERTY (${effectiveBuildingPrice:N0})";
                        buyButtonText.text = canAfford ? label : $"INSUFFICIENT FUNDS (${effectiveBuildingPrice:N0})";
                    }
                    if (buyButtonBg != null)
                    {
                        buyButtonBg.color = canAfford
                            ? new Color(0.12f, 0.78f, 0.38f, 1f) // Bright Green
                            : new Color(0.55f, 0.25f, 0.25f, 0.8f); // Muted Red
                    }
                }
            }

            // 6. Collect Rent Button
            if (collectRentButton != null)
            {
                double buildingRent = currentBuilding.IsOwned ? currentBuilding.DailyRentIncome : 0;
                double flatRent = currentBuilding.TotalDailyFlatRentIncome;
                double totalDue = buildingRent + flatRent;

                bool showCollect = currentBuilding.IsOwned || currentBuilding.FlatsRentedOut > 0;
                collectRentButton.gameObject.SetActive(showCollect);
                if (showCollect)
                {
                    bool canCollect = currentBuilding.RentAvailableToCollect && totalDue > 0;
                    collectRentButton.interactable = canCollect;
                    if (collectRentButtonText != null)
                    {
                        collectRentButtonText.text = canCollect
                            ? $"COLLECT RENT (+${totalDue:N0})"
                            : (totalDue > 0 ? "RENT COLLECTED TODAY" : "NO RENT DUE");
                    }
                }
            }

            // 7. Flats / Units Section
            bool showFlats = currentBuilding.HasFlats;
            if (flatSectionRoot != null)
            {
                flatSectionRoot.SetActive(showFlats);
            }
            if (showFlats)
            {
                if (flatsCountText != null)
                {
                    flatsCountText.text = $"Flats Owned: {currentBuilding.FlatsOwned}/{currentBuilding.TotalFlats}  |  Leased: {currentBuilding.FlatsRentedOut}/{currentBuilding.FlatsOwned}";
                }
                if (flatIncomeText != null)
                {
                    flatIncomeText.text = $"+${currentBuilding.TotalDailyFlatRentIncome:N0} / day from leased flats";
                }
                if (buyFlatButton != null)
                {
                    bool canAffordFlat = currentCash >= currentBuilding.FlatPurchasePrice;
                    bool hasUnbought = currentBuilding.FlatsOwned < currentBuilding.TotalFlats;
                    buyFlatButton.interactable = hasUnbought && canAffordFlat;
                    if (buyFlatButtonText != null)
                    {
                        buyFlatButtonText.text = hasUnbought
                            ? $"BUY FLAT #{currentBuilding.FlatsOwned + 1} (${currentBuilding.FlatPurchasePrice:N0})"
                            : "ALL FLATS OWNED";
                    }
                }
                if (giveRentButton != null)
                {
                    bool canGiveRent = currentBuilding.FlatsRentedOut < currentBuilding.FlatsOwned;
                    giveRentButton.interactable = canGiveRent;
                    if (giveRentButtonText != null)
                    {
                        giveRentButtonText.text = canGiveRent
                            ? $"RENT OUT FLAT (+${currentBuilding.FlatDailyRent:N0}/day)"
                            : (currentBuilding.FlatsOwned == 0 ? "BUY A FLAT FIRST" : "ALL FLATS LEASED");
                    }
                }
            }

            // 8. Cafe & Restaurant Dining Section
            bool isDining = currentBuilding.PropertyType == PropertyType.Restaurant || currentBuilding.PropertyType == PropertyType.Cafe;
            if (restaurantSectionRoot != null)
            {
                restaurantSectionRoot.SetActive(isDining);
            }
            if (isDining)
            {
                float foodRestore = PlayerHealthManager.Instance != null ? PlayerHealthManager.Instance.FoodHealthRestore : 45f;
                float drinkRestore = PlayerHealthManager.Instance != null ? PlayerHealthManager.Instance.DrinkHealthRestore : 25f;

                if (diningStatusText != null)
                {
                    diningStatusText.text = currentBuilding.IsOwned
                        ? $"VIP Owner: Food (+{foodRestore:0} HP) & Drinks (+{drinkRestore:0} HP) are FREE! Replenish your health anytime."
                        : $"Replenish Health! Food restores +{foodRestore:0} HP, Drinks restore +{drinkRestore:0} HP.";
                }
                if (drinkButton != null)
                {
                    if (drinkButtonText != null)
                    {
                        drinkButtonText.text = currentBuilding.IsOwned
                            ? $"DRINK (+{drinkRestore:0} HP, FREE)"
                            : $"DRINK (+{drinkRestore:0} HP, ${currentBuilding.DrinkPrice:N0})";
                    }
                }
                if (eatButton != null)
                {
                    if (eatButtonText != null)
                    {
                        eatButtonText.text = currentBuilding.IsOwned
                            ? $"EAT (+{foodRestore:0} HP, FREE)"
                            : $"EAT (+{foodRestore:0} HP, ${currentBuilding.FoodPrice:N0})";
                    }
                }
            }

            // 9. Taxi Station Special Section (Max 8 taxis)
            bool isTaxi = currentBuilding.PropertyType == PropertyType.TaxiStation;
            if (taxiSectionRoot != null)
            {
                taxiSectionRoot.SetActive(isTaxi);
            }

            if (isTaxi)
            {
                if (taxiRentalFeeText != null)
                {
                    taxiRentalFeeText.text = $"Rental Fee: -${currentBuilding.TaxiRentalFee:N0} / taxi";
                }

                if (taxiFareRevenueText != null)
                {
                    taxiFareRevenueText.text = $"Random Fares: ~$35 - $110 per trip";
                }

                if (taxiNetProfitText != null)
                {
                    taxiNetProfitText.text = $"Active Fleet: {currentBuilding.CurrentlyRentedTaxis} / {currentBuilding.MaxRentableTaxis} Taxis on Road";
                }

                if (taxiFleetStatusText != null)
                {
                    taxiFleetStatusText.text = $"Rented Taxis: {currentBuilding.CurrentlyRentedTaxis} / {currentBuilding.MaxRentableTaxis} operating on city roads";
                }

                if (rentTaxiButton != null)
                {
                    bool maxReached = currentBuilding.CurrentlyRentedTaxis >= currentBuilding.MaxRentableTaxis;
                    bool canAffordTaxi = currentCash >= currentBuilding.TaxiRentalFee;

                    rentTaxiButton.interactable = !maxReached && canAffordTaxi;
                    if (rentTaxiButtonText != null)
                    {
                        if (maxReached)
                        {
                            rentTaxiButtonText.text = $"MAX {currentBuilding.MaxRentableTaxis} TAXIS ACTIVE TODAY";
                        }
                        else
                        {
                            rentTaxiButtonText.text = canAffordTaxi
                                ? $"RENT TAXI #{currentBuilding.CurrentlyRentedTaxis + 1} (${currentBuilding.TaxiRentalFee:N0}) -> Dispatches to Road"
                                : $"NEED ${currentBuilding.TaxiRentalFee:N0} TO RENT TAXI";
                        }
                    }
                    if (rentTaxiButtonBg != null)
                    {
                        rentTaxiButtonBg.color = maxReached
                            ? new Color(0.4f, 0.45f, 0.5f, 0.7f)
                            : (canAffordTaxi ? new Color(0.98f, 0.74f, 0.12f, 1f) : new Color(0.5f, 0.45f, 0.3f, 0.8f));
                    }
                }
            }

            // 10. Stadium & Tennis Court Section
            bool isStadiumOrTennis = currentBuilding.PropertyType == PropertyType.Stadium || currentBuilding.PropertyType == PropertyType.TennisCourt;
            if (stadiumSectionRoot != null)
            {
                stadiumSectionRoot.SetActive(isStadiumOrTennis);
            }
            if (isStadiumOrTennis)
            {
                bool isTennis = currentBuilding.PropertyType == PropertyType.TennisCourt;
                if (stadiumStatsText != null)
                {
                    string matchTerm = isTennis ? "Tournaments Hosted" : "Matches Hosted";
                    stadiumStatsText.text = $"{matchTerm}: {currentBuilding.MatchesHosted}  |  Total Match Rent: ${currentBuilding.TotalMatchRevenue:N0}";
                }
                if (requestMatchButton != null)
                {
                    requestMatchButton.interactable = currentBuilding.IsOwned;
                    if (requestMatchButtonText != null)
                    {
                        string venueTerm = isTennis ? "TENNIS COURT" : "STADIUM";
                        requestMatchButtonText.text = currentBuilding.IsOwned
                            ? "REQUEST MATCH RENT OFFER NOW"
                            : $"BUY {venueTerm} TO HOST MATCHES";
                    }
                }
            }

            // 11. Hospital Healthcare Section
            bool isHospital = currentBuilding.PropertyType == PropertyType.Hospital;
            if (hospitalSectionRoot != null)
            {
                hospitalSectionRoot.SetActive(isHospital);
            }
            if (isHospital)
            {
                float curHp = PlayerHealthManager.Instance != null ? PlayerHealthManager.Instance.CurrentHealth : 100f;
                float maxHp = PlayerHealthManager.Instance != null ? PlayerHealthManager.Instance.MaxHealth : 100f;
                bool isIll = PlayerHealthManager.Instance != null && PlayerHealthManager.Instance.IsIll;
                string illnessReason = PlayerHealthManager.Instance != null ? PlayerHealthManager.Instance.IllnessReason : "";
                bool needsHeal = isIll || curHp < maxHp - 0.5f;

                if (hospitalStatusText != null)
                {
                    if (isIll)
                    {
                        hospitalStatusText.text = currentBuilding.IsOwned
                            ? $"<color=#FF4444>🚨 DIAGNOSIS: SICK ({illnessReason})</color>\nHealth: {curHp:0}/{maxHp:0} HP. As Hospital Owner, full emergency care is COMPLIMENTARY (FREE)!"
                            : $"<color=#FF4444>🚨 DIAGNOSIS: SICK ({illnessReason})</color>\nHealth: {curHp:0}/{maxHp:0} HP. Immediate hospital treatment required (${currentBuilding.HospitalTreatmentFee:N0})!";
                    }
                    else if (needsHeal)
                    {
                        hospitalStatusText.text = currentBuilding.IsOwned
                            ? $"Health: {curHp:0}/{maxHp:0} HP. As Hospital Owner, full medical care is COMPLIMENTARY (FREE)!"
                            : $"Health: {curHp:0}/{maxHp:0} HP. Visit anytime you are sick or weak to restore to 100 HP (${currentBuilding.HospitalTreatmentFee:N0})!";
                    }
                    else
                    {
                        hospitalStatusText.text = "<color=#44FF88>✅ HEALTHY & ENERGIZED (100% HP)</color>\nNo illness detected. Hospital emergency facilities standing by.";
                    }
                }

                if (treatmentButton != null)
                {
                    bool canAffordTreatment = currentBuilding.IsOwned || currentCash >= currentBuilding.HospitalTreatmentFee;
                    treatmentButton.interactable = needsHeal && canAffordTreatment;
                    if (treatmentButtonText != null)
                    {
                        if (!needsHeal)
                        {
                            treatmentButtonText.text = "HEALTH AT 100% (NO TREATMENT NEEDED)";
                        }
                        else if (currentBuilding.IsOwned)
                        {
                            treatmentButtonText.text = isIll
                                ? "CURE ILLNESS & RESTORE 100 HP (FREE)"
                                : "RECEIVE TREATMENT (HEAL 100 HP, FREE)";
                        }
                        else
                        {
                            if (canAffordTreatment)
                            {
                                treatmentButtonText.text = isIll
                                    ? $"CURE ILLNESS & RESTORE 100 HP (${currentBuilding.HospitalTreatmentFee:N0})"
                                    : $"RECEIVE TREATMENT (HEAL 100 HP, ${currentBuilding.HospitalTreatmentFee:N0})";
                            }
                            else
                            {
                                treatmentButtonText.text = $"NEED ${currentBuilding.HospitalTreatmentFee:N0} FOR TREATMENT";
                            }
                        }
                    }
                    if (treatmentButtonBg != null)
                    {
                        if (isIll && canAffordTreatment)
                        {
                            treatmentButtonBg.color = new Color(0.92f, 0.22f, 0.28f, 1f); // Emergency Red
                        }
                        else
                        {
                            treatmentButtonBg.color = needsHeal && canAffordTreatment
                                ? new Color(0.12f, 0.78f, 0.38f, 1f) // Healthy Green
                                : new Color(0.45f, 0.48f, 0.52f, 0.8f);
                        }
                    }
                }
            }

            // 12. Factory Industrial Production Section
            bool isFactory = currentBuilding.PropertyType == PropertyType.Factory;
            if (factorySectionRoot != null)
            {
                factorySectionRoot.SetActive(isFactory);
            }
            if (isFactory)
            {
                if (factoryStatusText != null)
                {
                    factoryStatusText.text = $"Batches: {currentBuilding.ProductionBatchesCompleted} | Revenue: ${currentBuilding.TotalProductionRevenueCollected:N0}\nReady: +${currentBuilding.PendingProductionEarnings:N0}";
                }
                if (collectProductionButton != null)
                {
                    bool hasEarnings = currentBuilding.PendingProductionEarnings > 0;
                    collectProductionButton.interactable = hasEarnings;
                    if (collectProductionButtonText != null)
                    {
                        collectProductionButtonText.text = hasEarnings
                            ? $"COLLECT PRODUCTION REVENUE (+${currentBuilding.PendingProductionEarnings:N0})"
                            : (currentBuilding.IsOwned ? "MANUFACTURING IN PROGRESS..." : "BUY FACTORY TO PRODUCE GOODS");
                    }
                    if (collectProductionButtonBg != null)
                    {
                        collectProductionButtonBg.color = hasEarnings
                            ? new Color(0.15f, 0.65f, 0.95f, 1f)
                            : new Color(0.40f, 0.45f, 0.52f, 0.8f);
                    }
                }
            }

            // 13. Oil Station Petroleum Section
            bool isOil = currentBuilding.PropertyType == PropertyType.OilStation;
            if (oilSectionRoot != null)
            {
                oilSectionRoot.SetActive(isOil);
            }
            if (isOil)
            {
                if (oilStatusText != null)
                {
                    oilStatusText.text = $"Total Fuel Sales: ${currentBuilding.TotalOilRevenueCollected:N0}\nReady: +${currentBuilding.PendingOilRevenue:N0}";
                }
                if (collectOilButton != null)
                {
                    bool hasOilRev = currentBuilding.PendingOilRevenue > 0;
                    collectOilButton.interactable = hasOilRev;
                    if (collectOilButtonText != null)
                    {
                        collectOilButtonText.text = hasOilRev
                            ? $"COLLECT OIL REVENUE (+${currentBuilding.PendingOilRevenue:N0})"
                            : (currentBuilding.IsOwned ? "FUEL SALES IN PROGRESS..." : "BUY OIL STATION TO EARN FUEL SALES");
                    }
                    if (collectOilButtonBg != null)
                    {
                        collectOilButtonBg.color = hasOilRev
                            ? new Color(0.95f, 0.65f, 0.12f, 1f)
                            : new Color(0.45f, 0.45f, 0.45f, 0.8f);
                    }
                }
            }

            // 13b. Park Visitor Revenue Section
            bool isPark = currentBuilding.PropertyType == PropertyType.Park;
            if (parkSectionRoot != null)
            {
                parkSectionRoot.SetActive(isPark);
            }
            if (isPark)
            {
                if (parkStatusText != null)
                {
                    parkStatusText.text = $"Total Visitor Revenue: ${currentBuilding.TotalParkRevenueCollected:N0}  (Visitors: {currentBuilding.ParkVisitorsServed})\nReady to Collect: +${currentBuilding.PendingParkRevenue:N0}";
                }
                if (collectParkButton != null)
                {
                    bool hasParkRev = currentBuilding.PendingParkRevenue > 0;
                    collectParkButton.interactable = hasParkRev;
                    if (collectParkButtonText != null)
                    {
                        collectParkButtonText.text = hasParkRev
                            ? $"COLLECT ENTRY FEES (+${currentBuilding.PendingParkRevenue:N0})"
                            : (currentBuilding.IsOwned ? "VISITORS STROLLING IN PARK..." : "BUY PARK TO EARN ENTRY FEES");
                    }
                    if (collectParkButtonBg != null)
                    {
                        collectParkButtonBg.color = hasParkRev
                            ? new Color(0.15f, 0.72f, 0.40f, 1f)
                            : new Color(0.45f, 0.45f, 0.45f, 0.8f);
                    }
                }
            }

            // 14. Building Maintenance Alert Section
            bool hasMaint = currentBuilding.HasPendingMaintenance;
            if (maintenanceSectionRoot != null)
            {
                maintenanceSectionRoot.SetActive(hasMaint);
            }
            if (hasMaint)
            {
                if (maintenanceReasonText != null)
                {
                    maintenanceReasonText.text = $"⚠️ MAINTENANCE REQUIRED: -${currentBuilding.PendingMaintenanceCost:N0}\n{currentBuilding.MaintenanceReason}";
                }
                if (payMaintenanceButton != null)
                {
                    bool canAffordMaint = currentCash >= currentBuilding.PendingMaintenanceCost;
                    payMaintenanceButton.interactable = canAffordMaint;
                    if (payMaintenanceButtonText != null)
                    {
                        payMaintenanceButtonText.text = canAffordMaint
                            ? $"PAY MAINTENANCE (-${currentBuilding.PendingMaintenanceCost:N0})"
                            : $"INSUFFICIENT FUNDS (-${currentBuilding.PendingMaintenanceCost:N0})";
                    }
                    if (payMaintenanceButtonBg != null)
                    {
                        payMaintenanceButtonBg.color = canAffordMaint
                            ? new Color(0.90f, 0.35f, 0.15f, 1f)
                            : new Color(0.55f, 0.25f, 0.25f, 0.8f);
                    }
                }
            }
        }

        private void OnBuyFlatClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.BuyFlat(out string msg))
            {
                GameSoundManager.Instance?.PlayBuy(currentBuilding.transform.position);
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnGiveRentClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.GiveRentForFlat(out string msg))
            {
                GameSoundManager.Instance?.PlayTenantMoveIn(currentBuilding.transform.position);
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnDrinkClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.DrinkBeverage(out string msg))
            {
                GameSoundManager.Instance?.PlayDrink(currentBuilding.transform.position);
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnEatClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.EatFood(out string msg))
            {
                GameSoundManager.Instance?.PlayEat(currentBuilding.transform.position);
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnRequestMatchClicked()
        {
            if (currentBuilding == null) return;
            GameSoundManager.Instance?.PlayClick(currentBuilding.transform.position);
            currentBuilding.TriggerExhibitionMatchRequest();
        }

        private void OnBuyClicked()
        {
            if (currentBuilding == null) return;

            bool success = currentBuilding.BuyProperty();
            if (success)
            {
                GameSoundManager.Instance?.PlayBuy(currentBuilding.transform.position);
                ShowToast($"Congratulations! You purchased {currentBuilding.PropertyName}!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast("Cannot afford to purchase this property!");
            }
        }

        private void OnRentTaxiClicked()
        {
            if (currentBuilding == null) return;

            bool success = currentBuilding.RentTaxiForDay(out string feedback);
            ShowToast(feedback);
            if (success)
            {
                GameSoundManager.Instance?.PlayTaxiSpawn(currentBuilding.transform.position);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
            }
        }

        private void OnCollectRentClicked()
        {
            if (currentBuilding == null) return;

            if (currentBuilding.CollectRent(out double amount))
            {
                GameSoundManager.Instance?.PlayCollect(currentBuilding.transform.position);
                ShowToast($"Collected +${amount:N0} in property rent income!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
        }

        private void OnTreatmentClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.ReceiveHospitalTreatment(out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnCollectProductionClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.CollectProductionRevenue(out double amt))
            {
                ShowToast($"Collected +${amt:N0} in factory production revenue!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast("No factory production revenue ready to collect.");
            }
        }

        private void OnCollectOilClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.CollectOilRevenue(out double amt))
            {
                ShowToast($"Collected +${amt:N0} in oil & fuel sales revenue!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast("No oil sales revenue ready to collect.");
            }
        }

        private void OnCollectParkRevenueClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.CollectParkRevenue(out double amt, out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        private void OnPayMaintenanceClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.PayMaintenance(out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(msg);
            }
        }

        public void ShowToast(string message)
        {
            if (toastRoot == null || toastMessageText == null)
            {
                Debug.Log($"[PropertyDetailModalUI] {message}");
                return;
            }

            toastMessageText.text = message;
            toastRoot.SetActive(true);

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }
            toastCoroutine = StartCoroutine(HideToastRoutine(3.5f));
        }

        private IEnumerator HideToastRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (toastRoot != null)
            {
                toastRoot.SetActive(false);
            }
        }

        private double GetPlayerCash()
        {
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                return GameManager.Instance.Economy.Cash;
            }
            return PlayerPrefs.GetFloat("SavedPlayerCash", 10000f);
        }

        private Color GetCategoryColor(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.RentalBuilding:
                    return new Color(0.18f, 0.65f, 0.92f, 1f); // Sky Blue
                case PropertyType.SmallRentalBuilding:
                    return new Color(0.28f, 0.75f, 0.42f, 1f); // Forest Green
                case PropertyType.Restaurant:
                    return new Color(0.88f, 0.28f, 0.35f, 1f); // Crimson
                case PropertyType.Cafe:
                    return new Color(0.95f, 0.58f, 0.15f, 1f); // Amber
                case PropertyType.TaxiStation:
                    return new Color(0.98f, 0.74f, 0.12f, 1f); // Taxi Yellow
                case PropertyType.Stadium:
                    return new Color(0.92f, 0.42f, 0.18f, 1f); // Vibrant Coral
                case PropertyType.Factory:
                    return new Color(0.48f, 0.54f, 0.62f, 1f); // Industrial Slate
                case PropertyType.OilStation:
                    return new Color(0.85f, 0.62f, 0.10f, 1f); // Petroleum Gold
                case PropertyType.Hospital:
                    return new Color(0.90f, 0.22f, 0.25f, 1f); // Red
                case PropertyType.School:
                    return new Color(0.25f, 0.55f, 0.85f, 1f); // Academic Blue
                case PropertyType.PoliceStation:
                    return new Color(0.18f, 0.35f, 0.68f, 1f); // Police Navy
                case PropertyType.TennisCourt:
                    return new Color(0.20f, 0.72f, 0.35f, 1f); // Tennis Green
                case PropertyType.Park:
                    return new Color(0.15f, 0.68f, 0.40f, 1f); // Park Emerald
                default:
                    return new Color(0.20f, 0.50f, 0.80f, 1f);
            }
        }

        #region Dynamic Modal UI Generation
        /// <summary>
        /// Automatically constructs the full modern UI hierarchy if not serialized in the scene.
        /// </summary>
        private void BuildDynamicModalUI()
        {
            Transform parentCanvas = targetCanvas != null ? targetCanvas.transform : transform;

            // 1. Root Container
            GameObject root = new GameObject("PropertyDetailModalRoot", typeof(RectTransform));
            root.transform.SetParent(parentCanvas, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            panelRoot = root;

            // 2. Dimmed Overlay Background
            GameObject overlay = new GameObject("DimmedOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            overlay.transform.SetParent(root.transform, false);
            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            Image overlayImg = overlay.GetComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.45f);
            Button overlayBtn = overlay.GetComponent<Button>();
            overlayBtn.onClick.AddListener(Close);

            // 3. Central Modal Card
            GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(root.transform, false);
            modalCardTransform = card.GetComponent<RectTransform>();
            modalCardTransform.anchorMin = new Vector2(0.5f, 0.5f);
            modalCardTransform.anchorMax = new Vector2(0.5f, 0.5f);
            modalCardTransform.pivot = new Vector2(0.5f, 0.5f);
            modalCardTransform.sizeDelta = new Vector2(520f, 680f);
            modalCardTransform.anchoredPosition = Vector2.zero;

            Image cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.10f, 0.13f, 0.20f, 0.98f); // Deep navy-slate
            if (roundedBoxSprite != null)
            {
                cardImg.sprite = roundedBoxSprite;
                cardImg.type = Image.Type.Sliced;
            }

            // 4. Header Bar with Close Button
            GameObject headerObj = CreateUIElement("HeaderBar", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 75), new Vector2(0, 0));

            // Category Badge
            GameObject catBadge = CreateUIElement("CategoryBadge", headerObj.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 28), new Vector2(25, 10));
            categoryBadgeBg = catBadge.AddComponent<Image>();
            categoryBadgeBg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            categoryBadgeText = CreateTextMesh("Text", catBadge.transform, "CATEGORY", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform catTextRt = categoryBadgeText.GetComponent<RectTransform>();
            catTextRt.anchorMin = Vector2.zero;
            catTextRt.anchorMax = Vector2.one;
            catTextRt.sizeDelta = Vector2.zero;

            // Status Badge
            GameObject statusBadge = CreateUIElement("StatusBadge", headerObj.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 28), new Vector2(175, 10));
            statusBadgeBg = statusBadge.AddComponent<Image>();
            statusBadgeBg.color = new Color(0.18f, 0.70f, 0.44f, 1f);
            statusBadgeText = CreateTextMesh("Text", statusBadge.transform, "FOR SALE", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform statTextRt = statusBadgeText.GetComponent<RectTransform>();
            statTextRt.anchorMin = Vector2.zero;
            statTextRt.anchorMax = Vector2.one;
            statTextRt.sizeDelta = Vector2.zero;

            // Close Button ('X')
            GameObject closeObj = CreateUIElement("CloseButton", headerObj.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(38, 38), new Vector2(-20, 10));
            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.color = new Color(0.85f, 0.25f, 0.25f, 0.9f);
            closeButton = closeObj.AddComponent<Button>();
            TextMeshProUGUI closeX = CreateTextMesh("X", closeObj.transform, "X", 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform closeXRt = closeX.GetComponent<RectTransform>();
            closeXRt.anchorMin = Vector2.zero;
            closeXRt.anchorMax = Vector2.one;
            closeXRt.sizeDelta = Vector2.zero;

            // 5. Title Text
            propertyTitleText = CreateTextMesh("PropertyTitle", card.transform, "Building Title", 24, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            RectTransform titleRt = propertyTitleText.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(25, -75);
            titleRt.sizeDelta = new Vector2(-50, 36);

            // 6. Description Text
            descriptionText = CreateTextMesh("Description", card.transform, "Building description here...", 13, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(0.78f, 0.82f, 0.90f));
            RectTransform descRt = descriptionText.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0, 1);
            descRt.anchorMax = new Vector2(1, 1);
            descRt.pivot = new Vector2(0.5f, 1);
            descRt.anchoredPosition = new Vector2(25, -115);
            descRt.sizeDelta = new Vector2(-50, 40);

            // 7. Economics Stats Box (Price & Daily Rent)
            GameObject statsBox = CreateUIElement("StatsBox", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 85), new Vector2(0, -165));
            Image statsBg = statsBox.AddComponent<Image>();
            statsBg.color = new Color(0.15f, 0.19f, 0.28f, 1f); // Card inner box

            // Purchase Price Column
            TextMeshProUGUI priceLabel = CreateTextMesh("PriceLabel", statsBox.transform, "PURCHASE PRICE", 11, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.65f, 0.70f, 0.80f));
            RectTransform plRt = priceLabel.GetComponent<RectTransform>();
            plRt.anchorMin = new Vector2(0, 0.5f);
            plRt.anchorMax = new Vector2(0.5f, 1);
            plRt.offsetMin = plRt.offsetMax = Vector2.zero;

            priceValueText = CreateTextMesh("PriceValue", statsBox.transform, "$25,000", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.25f));
            RectTransform pvRt = priceValueText.GetComponent<RectTransform>();
            pvRt.anchorMin = new Vector2(0, 0);
            pvRt.anchorMax = new Vector2(0.5f, 0.6f);
            pvRt.offsetMin = pvRt.offsetMax = Vector2.zero;

            // Daily Income Column
            TextMeshProUGUI rentLabel = CreateTextMesh("RentLabel", statsBox.transform, "DAILY RENT INCOME", 11, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.65f, 0.70f, 0.80f));
            RectTransform rlRt = rentLabel.GetComponent<RectTransform>();
            rlRt.anchorMin = new Vector2(0.5f, 0.5f);
            rlRt.anchorMax = new Vector2(1, 1);
            rlRt.offsetMin = rlRt.offsetMax = Vector2.zero;

            dailyIncomeValueText = CreateTextMesh("RentValue", statsBox.transform, "+$850 / day", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.25f, 0.95f, 0.50f));
            RectTransform rvRt = dailyIncomeValueText.GetComponent<RectTransform>();
            rvRt.anchorMin = new Vector2(0.5f, 0);
            rvRt.anchorMax = new Vector2(1, 0.6f);
            rvRt.offsetMin = rvRt.offsetMax = Vector2.zero;

            // 8. Taxi Station Special Section
            taxiSectionRoot = CreateUIElement("TaxiSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 150), new Vector2(0, -260));
            Image taxiBg = taxiSectionRoot.AddComponent<Image>();
            taxiBg.color = new Color(0.20f, 0.18f, 0.12f, 0.95f); // Gold tinted dark box

            TextMeshProUGUI taxiTitle = CreateTextMesh("TaxiTitle", taxiSectionRoot.transform, "TAXI FLEET MANAGEMENT (1-DAY PASS)", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.20f));
            RectTransform ttRt = taxiTitle.GetComponent<RectTransform>();
            ttRt.anchorMin = new Vector2(0, 1);
            ttRt.anchorMax = new Vector2(1, 1);
            ttRt.pivot = new Vector2(0, 1);
            ttRt.anchoredPosition = new Vector2(15, -10);
            ttRt.sizeDelta = new Vector2(-30, 22);

            taxiRentalFeeText = CreateTextMesh("RentalFee", taxiSectionRoot.transform, "Rental Fee: -$150", 12, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.95f, 0.5f, 0.5f));
            RectTransform trRt = taxiRentalFeeText.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0, 1);
            trRt.anchorMax = new Vector2(0.5f, 1);
            trRt.pivot = new Vector2(0, 1);
            trRt.anchoredPosition = new Vector2(15, -34);
            trRt.sizeDelta = new Vector2(-15, 20);

            taxiFareRevenueText = CreateTextMesh("FareRevenue", taxiSectionRoot.transform, "Fare Revenue: +$450", 12, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.4f, 0.95f, 0.5f));
            RectTransform tfRt = taxiFareRevenueText.GetComponent<RectTransform>();
            tfRt.anchorMin = new Vector2(0.5f, 1);
            tfRt.anchorMax = new Vector2(1, 1);
            tfRt.pivot = new Vector2(0, 1);
            tfRt.anchoredPosition = new Vector2(10, -34);
            tfRt.sizeDelta = new Vector2(-10, 20);

            taxiNetProfitText = CreateTextMesh("NetProfit", taxiSectionRoot.transform, "Net Daily Fare Profit: +$300 / day", 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.5f));
            RectTransform tpRt = taxiNetProfitText.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0, 1);
            tpRt.anchorMax = new Vector2(1, 1);
            tpRt.pivot = new Vector2(0.5f, 1);
            tpRt.anchoredPosition = new Vector2(0, -60);
            tpRt.sizeDelta = new Vector2(-30, 22);

            taxiFleetStatusText = CreateTextMesh("FleetStatus", taxiSectionRoot.transform, "Rented Taxis: 0 / 8 operating on city roads", 11, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f));
            RectTransform tfsRt = taxiFleetStatusText.GetComponent<RectTransform>();
            tfsRt.anchorMin = new Vector2(0, 1);
            tfsRt.anchorMax = new Vector2(1, 1);
            tfsRt.pivot = new Vector2(0.5f, 1);
            tfsRt.anchoredPosition = new Vector2(0, -82);
            tfsRt.sizeDelta = new Vector2(-30, 18);

            // Rent Taxi Button
            GameObject rentTaxiBtnObj = CreateUIElement("RentTaxiButton", taxiSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            rentTaxiButtonBg = rentTaxiBtnObj.AddComponent<Image>();
            rentTaxiButtonBg.color = new Color(0.98f, 0.74f, 0.12f, 1f);
            rentTaxiButton = rentTaxiBtnObj.AddComponent<Button>();
            rentTaxiButtonText = CreateTextMesh("Text", rentTaxiBtnObj.transform, "RENT TAXI FOR 1 DAY (-$150)", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            RectTransform rtTextRt = rentTaxiButtonText.GetComponent<RectTransform>();
            rtTextRt.anchorMin = Vector2.zero;
            rtTextRt.anchorMax = Vector2.one;
            rtTextRt.sizeDelta = Vector2.zero;

            // 8b. Flat / Unit Section
            flatSectionRoot = CreateUIElement("FlatSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image flatBg = flatSectionRoot.AddComponent<Image>();
            flatBg.color = new Color(0.12f, 0.20f, 0.28f, 0.95f);

            TextMeshProUGUI flatTitle = CreateTextMesh("FlatTitle", flatSectionRoot.transform, "APARTMENT RESIDENCES & FLATS", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.35f, 0.85f, 1f));
            RectTransform flatTitleRt = flatTitle.GetComponent<RectTransform>();
            flatTitleRt.anchorMin = new Vector2(0, 1);
            flatTitleRt.anchorMax = new Vector2(1, 1);
            flatTitleRt.pivot = new Vector2(0, 1);
            flatTitleRt.anchoredPosition = new Vector2(15, -10);
            flatTitleRt.sizeDelta = new Vector2(-30, 22);

            flatsCountText = CreateTextMesh("FlatsCount", flatSectionRoot.transform, "Flats Owned: 0/8 | Leased: 0/0", 12, FontStyles.Normal, TextAlignmentOptions.Left, Color.white);
            RectTransform fcRt = flatsCountText.GetComponent<RectTransform>();
            fcRt.anchorMin = new Vector2(0, 1);
            fcRt.anchorMax = new Vector2(1, 1);
            fcRt.pivot = new Vector2(0, 1);
            fcRt.anchoredPosition = new Vector2(15, -34);
            fcRt.sizeDelta = new Vector2(-30, 20);

            flatIncomeText = CreateTextMesh("FlatIncome", flatSectionRoot.transform, "+$0 / day from leased flats", 12, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.25f, 0.95f, 0.50f));
            RectTransform fiRt = flatIncomeText.GetComponent<RectTransform>();
            fiRt.anchorMin = new Vector2(0, 1);
            fiRt.anchorMax = new Vector2(1, 1);
            fiRt.pivot = new Vector2(0, 1);
            fiRt.anchoredPosition = new Vector2(15, -54);
            fiRt.sizeDelta = new Vector2(-30, 20);

            GameObject buyFlatBtnObj = CreateUIElement("BuyFlatButton", flatSectionRoot.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(-18, 44), new Vector2(12, 10));
            Image buyFlatImg = buyFlatBtnObj.AddComponent<Image>();
            buyFlatImg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            buyFlatButton = buyFlatBtnObj.AddComponent<Button>();
            buyFlatButtonText = CreateTextMesh("Text", buyFlatBtnObj.transform, "BUY FLAT ($5,500)", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform bfbTextRt = buyFlatButtonText.GetComponent<RectTransform>();
            bfbTextRt.anchorMin = Vector2.zero;
            bfbTextRt.anchorMax = Vector2.one;
            bfbTextRt.sizeDelta = Vector2.zero;

            GameObject giveRentBtnObj = CreateUIElement("GiveRentButton", flatSectionRoot.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 44), new Vector2(-12, 10));
            Image giveRentImg = giveRentBtnObj.AddComponent<Image>();
            giveRentImg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            giveRentButton = giveRentBtnObj.AddComponent<Button>();
            giveRentButtonText = CreateTextMesh("Text", giveRentBtnObj.transform, "RENT OUT FLAT", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform grbTextRt = giveRentButtonText.GetComponent<RectTransform>();
            grbTextRt.anchorMin = Vector2.zero;
            grbTextRt.anchorMax = Vector2.one;
            grbTextRt.sizeDelta = Vector2.zero;

            // 8c. Cafe & Restaurant Dining Section
            restaurantSectionRoot = CreateUIElement("RestaurantSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image restBg = restaurantSectionRoot.AddComponent<Image>();
            restBg.color = new Color(0.24f, 0.14f, 0.16f, 0.95f);

            TextMeshProUGUI restTitle = CreateTextMesh("RestTitle", restaurantSectionRoot.transform, "CAFE & RESTAURANT DINING", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.45f, 0.55f));
            RectTransform rtTitleRt = restTitle.GetComponent<RectTransform>();
            rtTitleRt.anchorMin = new Vector2(0, 1);
            rtTitleRt.anchorMax = new Vector2(1, 1);
            rtTitleRt.pivot = new Vector2(0, 1);
            rtTitleRt.anchoredPosition = new Vector2(15, -10);
            rtTitleRt.sizeDelta = new Vector2(-30, 22);

            diningStatusText = CreateTextMesh("DiningStatus", restaurantSectionRoot.transform, "Enjoy espresso or fine dining! Buy restaurant for VIP free dining & daily profit.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.85f, 0.85f));
            RectTransform dsRt = diningStatusText.GetComponent<RectTransform>();
            dsRt.anchorMin = new Vector2(0, 1);
            dsRt.anchorMax = new Vector2(1, 1);
            dsRt.pivot = new Vector2(0, 1);
            dsRt.anchoredPosition = new Vector2(15, -34);
            dsRt.sizeDelta = new Vector2(-30, 34);

            GameObject drinkBtnObj = CreateUIElement("DrinkButton", restaurantSectionRoot.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(-18, 44), new Vector2(12, 10));
            Image drinkImg = drinkBtnObj.AddComponent<Image>();
            drinkImg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            drinkButton = drinkBtnObj.AddComponent<Button>();
            drinkButtonText = CreateTextMesh("Text", drinkBtnObj.transform, "DRINK COFFEE ($12)", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            RectTransform dbTextRt = drinkButtonText.GetComponent<RectTransform>();
            dbTextRt.anchorMin = Vector2.zero;
            dbTextRt.anchorMax = Vector2.one;
            dbTextRt.sizeDelta = Vector2.zero;

            GameObject eatBtnObj = CreateUIElement("EatButton", restaurantSectionRoot.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 44), new Vector2(-12, 10));
            Image eatImg = eatBtnObj.AddComponent<Image>();
            eatImg.color = new Color(0.88f, 0.28f, 0.35f, 1f);
            eatButton = eatBtnObj.AddComponent<Button>();
            eatButtonText = CreateTextMesh("Text", eatBtnObj.transform, "EAT MEAL ($28)", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform ebTextRt = eatButtonText.GetComponent<RectTransform>();
            ebTextRt.anchorMin = Vector2.zero;
            ebTextRt.anchorMax = Vector2.one;
            ebTextRt.sizeDelta = Vector2.zero;

            // 8d. Stadium Section
            stadiumSectionRoot = CreateUIElement("StadiumSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image stadiumBg = stadiumSectionRoot.AddComponent<Image>();
            stadiumBg.color = new Color(0.18f, 0.16f, 0.26f, 0.95f);

            TextMeshProUGUI stadiumTitle = CreateTextMesh("StadiumTitle", stadiumSectionRoot.transform, "FOOTBALL MATCH RENT HOSTING", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.85f, 0.65f, 1f));
            RectTransform stTitleRt = stadiumTitle.GetComponent<RectTransform>();
            stTitleRt.anchorMin = new Vector2(0, 1);
            stTitleRt.anchorMax = new Vector2(1, 1);
            stTitleRt.pivot = new Vector2(0, 1);
            stTitleRt.anchoredPosition = new Vector2(15, -10);
            stTitleRt.sizeDelta = new Vector2(-30, 22);

            stadiumStatsText = CreateTextMesh("StadiumStats", stadiumSectionRoot.transform, "Matches Hosted: 0 | Total Match Revenue: $0", 12, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.9f, 0.9f));
            RectTransform ssRt = stadiumStatsText.GetComponent<RectTransform>();
            ssRt.anchorMin = new Vector2(0, 1);
            ssRt.anchorMax = new Vector2(1, 1);
            ssRt.pivot = new Vector2(0, 1);
            ssRt.anchoredPosition = new Vector2(15, -34);
            ssRt.sizeDelta = new Vector2(-30, 34);

            GameObject reqMatchBtnObj = CreateUIElement("RequestMatchButton", stadiumSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            Image reqMatchImg = reqMatchBtnObj.AddComponent<Image>();
            reqMatchImg.color = new Color(0.92f, 0.42f, 0.18f, 1f);
            requestMatchButton = reqMatchBtnObj.AddComponent<Button>();
            requestMatchButtonText = CreateTextMesh("Text", reqMatchBtnObj.transform, "REQUEST MATCH RENT OFFER NOW", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform rmbTextRt = requestMatchButtonText.GetComponent<RectTransform>();
            rmbTextRt.anchorMin = Vector2.zero;
            rmbTextRt.anchorMax = Vector2.one;
            rmbTextRt.sizeDelta = Vector2.zero;

            // 8e. Hospital Healthcare Section
            hospitalSectionRoot = CreateUIElement("HospitalSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image hospBg = hospitalSectionRoot.AddComponent<Image>();
            hospBg.color = new Color(0.24f, 0.12f, 0.14f, 0.95f);

            TextMeshProUGUI hospTitle = CreateTextMesh("HospTitle", hospitalSectionRoot.transform, "HOSPITAL EMERGENCY & HEALTHCARE", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.45f, 0.50f));
            RectTransform htRt = hospTitle.GetComponent<RectTransform>();
            htRt.anchorMin = new Vector2(0, 1);
            htRt.anchorMax = new Vector2(1, 1);
            htRt.pivot = new Vector2(0, 1);
            htRt.anchoredPosition = new Vector2(15, -10);
            htRt.sizeDelta = new Vector2(-30, 22);

            hospitalStatusText = CreateTextMesh("HospitalStatus", hospitalSectionRoot.transform, "Visit for full medical treatment (restoring 100 HP) when sick or exhausted.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.9f, 0.88f, 0.88f));
            RectTransform hsRt = hospitalStatusText.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0, 1);
            hsRt.anchorMax = new Vector2(1, 1);
            hsRt.pivot = new Vector2(0, 1);
            hsRt.anchoredPosition = new Vector2(15, -34);
            hsRt.sizeDelta = new Vector2(-30, 34);

            GameObject treatBtnObj = CreateUIElement("TreatmentButton", hospitalSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            treatmentButtonBg = treatBtnObj.AddComponent<Image>();
            treatmentButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            treatmentButton = treatBtnObj.AddComponent<Button>();
            treatmentButtonText = CreateTextMesh("Text", treatBtnObj.transform, "RECEIVE TREATMENT (RESTORE 100 HP)", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform tbTextRt = treatmentButtonText.GetComponent<RectTransform>();
            tbTextRt.anchorMin = Vector2.zero;
            tbTextRt.anchorMax = Vector2.one;
            tbTextRt.sizeDelta = Vector2.zero;

            // 8f. Factory Industrial Section
            factorySectionRoot = CreateUIElement("FactorySection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image factBg = factorySectionRoot.AddComponent<Image>();
            factBg.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);

            TextMeshProUGUI factTitle = CreateTextMesh("FactTitle", factorySectionRoot.transform, "INDUSTRIAL MANUFACTURING PRODUCTION", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.45f, 0.75f, 1f));
            RectTransform ftRt = factTitle.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0, 1);
            ftRt.anchorMax = new Vector2(1, 1);
            ftRt.pivot = new Vector2(0, 1);
            ftRt.anchoredPosition = new Vector2(15, -10);
            ftRt.sizeDelta = new Vector2(-30, 22);

            factoryStatusText = CreateTextMesh("FactoryStatus", factorySectionRoot.transform, "Heavy manufacturing plant. Completes production runs with large payout revenues.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.85f, 0.88f, 0.92f));
            RectTransform fsRt = factoryStatusText.GetComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0, 1);
            fsRt.anchorMax = new Vector2(1, 1);
            fsRt.pivot = new Vector2(0, 1);
            fsRt.anchoredPosition = new Vector2(15, -34);
            fsRt.sizeDelta = new Vector2(-30, 34);

            GameObject prodBtnObj = CreateUIElement("CollectProductionButton", factorySectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectProductionButtonBg = prodBtnObj.AddComponent<Image>();
            collectProductionButtonBg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            collectProductionButton = prodBtnObj.AddComponent<Button>();
            collectProductionButtonText = CreateTextMesh("Text", prodBtnObj.transform, "COLLECT PRODUCTION REVENUE", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pbTextRt = collectProductionButtonText.GetComponent<RectTransform>();
            pbTextRt.anchorMin = Vector2.zero;
            pbTextRt.anchorMax = Vector2.one;
            pbTextRt.sizeDelta = Vector2.zero;

            // 8g. Oil & Petroleum Station Section
            oilSectionRoot = CreateUIElement("OilSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image oilBg = oilSectionRoot.AddComponent<Image>();
            oilBg.color = new Color(0.22f, 0.18f, 0.12f, 0.95f);

            TextMeshProUGUI oilTitle = CreateTextMesh("OilTitle", oilSectionRoot.transform, "OIL & FUEL DISTRIBUTION SALES", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.25f));
            RectTransform otRt = oilTitle.GetComponent<RectTransform>();
            otRt.anchorMin = new Vector2(0, 1);
            otRt.anchorMax = new Vector2(1, 1);
            otRt.pivot = new Vector2(0, 1);
            otRt.anchoredPosition = new Vector2(15, -10);
            otRt.sizeDelta = new Vector2(-30, 22);

            oilStatusText = CreateTextMesh("OilStatus", oilSectionRoot.transform, "Automotive refueling center. Earns regular revenue payouts from vehicle traffic.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.92f, 0.90f, 0.85f));
            RectTransform osRt = oilStatusText.GetComponent<RectTransform>();
            osRt.anchorMin = new Vector2(0, 1);
            osRt.anchorMax = new Vector2(1, 1);
            osRt.pivot = new Vector2(0, 1);
            osRt.anchoredPosition = new Vector2(15, -34);
            osRt.sizeDelta = new Vector2(-30, 34);

            GameObject oilBtnObj = CreateUIElement("CollectOilButton", oilSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectOilButtonBg = oilBtnObj.AddComponent<Image>();
            collectOilButtonBg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            collectOilButton = oilBtnObj.AddComponent<Button>();
            collectOilButtonText = CreateTextMesh("Text", oilBtnObj.transform, "COLLECT OIL REVENUE", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            RectTransform obTextRt = collectOilButtonText.GetComponent<RectTransform>();
            obTextRt.anchorMin = Vector2.zero;
            obTextRt.anchorMax = Vector2.one;
            obTextRt.sizeDelta = Vector2.zero;

            // 8g2. Park Visitor Entry Fee Section
            parkSectionRoot = CreateUIElement("ParkSection", card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image parkBg = parkSectionRoot.AddComponent<Image>();
            parkBg.color = new Color(0.12f, 0.24f, 0.16f, 0.95f);

            TextMeshProUGUI parkTitle = CreateTextMesh("ParkTitle", parkSectionRoot.transform, "GRAND PARK VISITOR ADMISSION FEES", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.35f, 0.92f, 0.55f));
            RectTransform pktRt = parkTitle.GetComponent<RectTransform>();
            pktRt.anchorMin = new Vector2(0, 1);
            pktRt.anchorMax = new Vector2(1, 1);
            pktRt.pivot = new Vector2(0, 1);
            pktRt.anchoredPosition = new Vector2(15, -10);
            pktRt.sizeDelta = new Vector2(-30, 22);

            parkStatusText = CreateTextMesh("ParkStatus", parkSectionRoot.transform, "Scenic city park. Strolling citizens pay admission entry fees periodically.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.85f, 0.95f, 0.88f));
            RectTransform pksRt = parkStatusText.GetComponent<RectTransform>();
            pksRt.anchorMin = new Vector2(0, 1);
            pksRt.anchorMax = new Vector2(1, 1);
            pksRt.pivot = new Vector2(0, 1);
            pksRt.anchoredPosition = new Vector2(15, -34);
            pksRt.sizeDelta = new Vector2(-30, 34);

            GameObject parkBtnObj = CreateUIElement("CollectParkButton", parkSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectParkButtonBg = parkBtnObj.AddComponent<Image>();
            collectParkButtonBg.color = new Color(0.15f, 0.72f, 0.40f, 1f);
            collectParkButton = parkBtnObj.AddComponent<Button>();
            collectParkButtonText = CreateTextMesh("Text", parkBtnObj.transform, "COLLECT PARK ENTRY FEES", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pkbTextRt = collectParkButtonText.GetComponent<RectTransform>();
            pkbTextRt.anchorMin = Vector2.zero;
            pkbTextRt.anchorMax = Vector2.one;
            pkbTextRt.sizeDelta = Vector2.zero;

            // 8h. Building Maintenance Alert Section (Positioned at bottom of section slot)
            maintenanceSectionRoot = CreateUIElement("MaintenanceSection", card.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 72), new Vector2(0, 136));
            Image maintBg = maintenanceSectionRoot.AddComponent<Image>();
            maintBg.color = new Color(0.32f, 0.12f, 0.12f, 0.95f);

            maintenanceReasonText = CreateTextMesh("MaintenanceReason", maintenanceSectionRoot.transform, "⚠️ MAINTENANCE REQUIRED: -$0", 11, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.75f, 0.75f));
            RectTransform mrRt = maintenanceReasonText.GetComponent<RectTransform>();
            mrRt.anchorMin = new Vector2(0, 0.5f);
            mrRt.anchorMax = new Vector2(0.55f, 1);
            mrRt.pivot = new Vector2(0, 0.5f);
            mrRt.anchoredPosition = new Vector2(10, 0);
            mrRt.sizeDelta = new Vector2(-15, 0);

            GameObject payMaintBtnObj = CreateUIElement("PayMaintenanceButton", maintenanceSectionRoot.transform, new Vector2(0.55f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            payMaintenanceButtonBg = payMaintBtnObj.AddComponent<Image>();
            payMaintenanceButtonBg.color = new Color(0.90f, 0.35f, 0.15f, 1f);
            payMaintenanceButton = payMaintBtnObj.AddComponent<Button>();
            payMaintenanceButtonText = CreateTextMesh("Text", payMaintBtnObj.transform, "PAY MAINTENANCE", 11, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pmbTextRt = payMaintenanceButtonText.GetComponent<RectTransform>();
            pmbTextRt.anchorMin = Vector2.zero;
            pmbTextRt.anchorMax = Vector2.one;
            pmbTextRt.sizeDelta = Vector2.zero;
            maintenanceSectionRoot.SetActive(false);

            // 9. Collect Rent Button
            GameObject collectBtnObj = CreateUIElement("CollectRentButton", card.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 48), new Vector2(0, 78));
            Image collectBg = collectBtnObj.AddComponent<Image>();
            collectBg.color = new Color(0.18f, 0.65f, 0.92f, 1f); // Vibrant blue
            collectRentButton = collectBtnObj.AddComponent<Button>();
            collectRentButtonText = CreateTextMesh("Text", collectBtnObj.transform, "COLLECT DAILY RENT", 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform crTextRt = collectRentButtonText.GetComponent<RectTransform>();
            crTextRt.anchorMin = Vector2.zero;
            crTextRt.anchorMax = Vector2.one;
            crTextRt.sizeDelta = Vector2.zero;

            // 10. Buy Property Button
            GameObject buyBtnObj = CreateUIElement("BuyPropertyButton", card.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 52), new Vector2(0, 20));
            buyButtonBg = buyBtnObj.AddComponent<Image>();
            buyButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f); // Bright emerald green
            buyPropertyButton = buyBtnObj.AddComponent<Button>();
            buyButtonText = CreateTextMesh("Text", buyBtnObj.transform, "BUY PROPERTY ($25,000)", 15, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform bbTextRt = buyButtonText.GetComponent<RectTransform>();
            bbTextRt.anchorMin = Vector2.zero;
            bbTextRt.anchorMax = Vector2.one;
            bbTextRt.sizeDelta = Vector2.zero;

            // 11. Toast Feedback Banner (Floating near top of screen)
            toastRoot = CreateUIElement("FeedbackToast", parentCanvas, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(620, 50), new Vector2(0, -95));
            Image toastBg = toastRoot.AddComponent<Image>();
            toastBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            toastMessageText = CreateTextMesh("ToastText", toastRoot.transform, "Feedback message here", 15, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.3f));
            RectTransform toastTextRt = toastMessageText.GetComponent<RectTransform>();
            toastTextRt.anchorMin = Vector2.zero;
            toastTextRt.anchorMax = Vector2.one;
            toastTextRt.sizeDelta = Vector2.zero;
            toastRoot.SetActive(false);
        }

        private GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        private TextMeshProUGUI CreateTextMesh(string name, Transform parent, string text, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }
        #endregion
    }
}
