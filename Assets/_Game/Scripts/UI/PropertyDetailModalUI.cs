using System;
using System.Collections;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Localization;
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

        public RectTransform ModalCardTransform => modalCardTransform;
        public GameObject PanelRoot => panelRoot;

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

        [Header("Bank Operations Section (Special)")]
        [SerializeField] private GameObject bankSectionRoot;
        [SerializeField] private TextMeshProUGUI bankStatusText;
        [SerializeField] private GameObject bankClosedRoot;
        [SerializeField] private Button openBankButton;
        [SerializeField] private TextMeshProUGUI openBankButtonText;
        [SerializeField] private Image openBankButtonBg;
        [SerializeField] private GameObject bankActiveRoot;
        [SerializeField] private TMP_InputField bankDepositInputField;
        [SerializeField] private Button depositButton;
        [SerializeField] private TextMeshProUGUI depositButtonText;
        [SerializeField] private Image depositButtonBg;
        [SerializeField] private Button withdrawButton;
        [SerializeField] private TextMeshProUGUI withdrawButtonText;
        [SerializeField] private Image withdrawButtonBg;
        [SerializeField] private Button withdrawAllButton;
        [SerializeField] private TextMeshProUGUI withdrawAllButtonText;

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
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
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
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
            if (currentBuilding != null)
            {
                currentBuilding.OnStateChanged -= HandleBuildingStateChanged;
            }
        }

        private void HandleLanguageChanged()
        {
            if (panelRoot != null && panelRoot.activeSelf && currentBuilding != null)
            {
                RefreshUI();
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
            if (card == null && modalCardTransform != null)
            {
                card = modalCardTransform;
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
                else CreateHospitalSection(card);
            }
            if (hospitalSectionRoot != null)
            {
                if (hospitalStatusText == null)
                    hospitalStatusText = hospitalSectionRoot.transform.Find("HospitalStatus")?.GetComponent<TextMeshProUGUI>();
                if (treatmentButton == null)
                    treatmentButton = hospitalSectionRoot.transform.Find("TreatmentButton")?.GetComponent<Button>();
                if (treatmentButton != null)
                {
                    if (treatmentButtonText == null)
                        treatmentButtonText = treatmentButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    if (treatmentButtonBg == null)
                        treatmentButtonBg = treatmentButton.GetComponent<Image>();

                    treatmentButton.onClick.RemoveAllListeners();
                    treatmentButton.onClick.AddListener(OnTreatmentClicked);
                }
            }

            if (factorySectionRoot == null && card != null)
            {
                var t = card.Find("FactorySection");
                if (t != null) factorySectionRoot = t.gameObject;
                else CreateFactorySection(card);
            }
            if (factorySectionRoot != null)
            {
                if (factoryStatusText == null)
                    factoryStatusText = factorySectionRoot.transform.Find("FactoryStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectProductionButton == null)
                    collectProductionButton = factorySectionRoot.transform.Find("CollectProductionButton")?.GetComponent<Button>();
                if (collectProductionButtonText == null && collectProductionButton != null)
                    collectProductionButtonText = collectProductionButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (collectProductionButtonBg == null && collectProductionButton != null)
                    collectProductionButtonBg = collectProductionButton.GetComponent<Image>();
                if (collectProductionButton != null)
                {
                    collectProductionButton.onClick.RemoveAllListeners();
                    collectProductionButton.onClick.AddListener(OnCollectProductionClicked);
                }
            }

            if (oilSectionRoot == null && card != null)
            {
                var t = card.Find("OilSection");
                if (t != null) oilSectionRoot = t.gameObject;
                else CreateOilSection(card);
            }
            if (oilSectionRoot != null)
            {
                if (oilStatusText == null)
                    oilStatusText = oilSectionRoot.transform.Find("OilStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectOilButton == null)
                    collectOilButton = oilSectionRoot.transform.Find("CollectOilButton")?.GetComponent<Button>();
                if (collectOilButtonText == null && collectOilButton != null)
                    collectOilButtonText = collectOilButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (collectOilButtonBg == null && collectOilButton != null)
                    collectOilButtonBg = collectOilButton.GetComponent<Image>();
                if (collectOilButton != null)
                {
                    collectOilButton.onClick.RemoveAllListeners();
                    collectOilButton.onClick.AddListener(OnCollectOilClicked);
                }
            }

            if (parkSectionRoot == null && card != null)
            {
                var t = card.Find("ParkSection");
                if (t != null) parkSectionRoot = t.gameObject;
                else CreateParkSection(card);
            }
            if (parkSectionRoot != null)
            {
                if (parkStatusText == null)
                    parkStatusText = parkSectionRoot.transform.Find("ParkStatus")?.GetComponent<TextMeshProUGUI>();
                if (collectParkButton == null)
                    collectParkButton = parkSectionRoot.transform.Find("CollectParkButton")?.GetComponent<Button>();
                if (collectParkButtonText == null && collectParkButton != null)
                    collectParkButtonText = collectParkButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (collectParkButtonBg == null && collectParkButton != null)
                    collectParkButtonBg = collectParkButton.GetComponent<Image>();
                if (collectParkButton != null)
                {
                    collectParkButton.onClick.RemoveAllListeners();
                    collectParkButton.onClick.AddListener(OnCollectParkRevenueClicked);
                }
            }

            if (bankSectionRoot == null && card != null)
            {
                var t = card.Find("BankSection");
                if (t != null) bankSectionRoot = t.gameObject;
                else CreateBankSection(card);
            }
            if (bankSectionRoot != null)
            {
                if (bankStatusText == null)
                    bankStatusText = bankSectionRoot.transform.Find("BankStatus")?.GetComponent<TextMeshProUGUI>();
                if (bankClosedRoot == null)
                    bankClosedRoot = bankSectionRoot.transform.Find("BankClosedRoot")?.gameObject;
                if (bankActiveRoot == null)
                    bankActiveRoot = bankSectionRoot.transform.Find("BankActiveRoot")?.gameObject;

                if (openBankButton == null && bankClosedRoot != null)
                    openBankButton = bankClosedRoot.transform.Find("OpenBankButton")?.GetComponent<Button>();
                if (openBankButton != null)
                {
                    if (openBankButtonText == null)
                        openBankButtonText = openBankButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                    if (openBankButtonBg == null)
                        openBankButtonBg = openBankButton.GetComponent<Image>();
                    openBankButton.onClick.RemoveAllListeners();
                    openBankButton.onClick.AddListener(OnOpenBankAccountClicked);
                }

                if (bankActiveRoot != null)
                {
                    if (bankDepositInputField == null)
                        bankDepositInputField = bankActiveRoot.transform.Find("BankAmountInput")?.GetComponent<TMP_InputField>();
                    if (depositButton == null)
                        depositButton = bankActiveRoot.transform.Find("DepositButton")?.GetComponent<Button>();
                    if (depositButton != null)
                    {
                        if (depositButtonText == null)
                            depositButtonText = depositButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (depositButtonBg == null)
                            depositButtonBg = depositButton.GetComponent<Image>();
                        depositButton.onClick.RemoveAllListeners();
                        depositButton.onClick.AddListener(OnDepositClicked);
                    }

                    if (withdrawButton == null)
                        withdrawButton = bankActiveRoot.transform.Find("WithdrawButton")?.GetComponent<Button>();
                    if (withdrawButton != null)
                    {
                        if (withdrawButtonText == null)
                            withdrawButtonText = withdrawButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        if (withdrawButtonBg == null)
                            withdrawButtonBg = withdrawButton.GetComponent<Image>();
                        withdrawButton.onClick.RemoveAllListeners();
                        withdrawButton.onClick.AddListener(OnWithdrawClicked);
                    }

                    if (withdrawAllButton == null)
                        withdrawAllButton = bankActiveRoot.transform.Find("WithdrawAllButton")?.GetComponent<Button>();
                    if (withdrawAllButton != null)
                    {
                        if (withdrawAllButtonText == null)
                            withdrawAllButtonText = withdrawAllButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                        withdrawAllButton.onClick.RemoveAllListeners();
                        withdrawAllButton.onClick.AddListener(OnWithdrawAllClicked);
                    }
                }
            }

            if (maintenanceSectionRoot == null && card != null)
            {
                var t = card.Find("MaintenanceSection");
                if (t != null) maintenanceSectionRoot = t.gameObject;
                else CreateMaintenanceSection(card);
            }
            if (maintenanceSectionRoot != null)
            {
                if (maintenanceReasonText == null)
                    maintenanceReasonText = maintenanceSectionRoot.transform.Find("MaintenanceReason")?.GetComponent<TextMeshProUGUI>();
                if (payMaintenanceButton == null)
                    payMaintenanceButton = maintenanceSectionRoot.transform.Find("PayMaintenanceButton")?.GetComponent<Button>();
                if (payMaintenanceButtonText == null && payMaintenanceButton != null)
                    payMaintenanceButtonText = payMaintenanceButton.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
                if (payMaintenanceButtonBg == null && payMaintenanceButton != null)
                    payMaintenanceButtonBg = payMaintenanceButton.GetComponent<Image>();
                if (payMaintenanceButton != null)
                {
                    payMaintenanceButton.onClick.RemoveAllListeners();
                    payMaintenanceButton.onClick.AddListener(OnPayMaintenanceClicked);
                }
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
                string bName = LocalizationManager.GetBuildingName(currentBuilding.PropertyName, currentBuilding.PropertyType);
                LocalizationManager.ApplyTo(propertyTitleText, bName);
            }

            if (descriptionText != null)
            {
                string bDesc = LocalizationManager.GetBuildingDescription(currentBuilding.Description, currentBuilding.PropertyType);
                LocalizationManager.ApplyTo(descriptionText, bDesc);
            }

            // 2. Category Badge
            if (categoryBadgeText != null)
            {
                string catName = LocalizationManager.GetPropertyTypeName(currentBuilding.PropertyType);
                LocalizationManager.ApplyTo(categoryBadgeText, catName);
            }

            if (categoryBadgeBg != null)
            {
                categoryBadgeBg.color = GetCategoryColor(currentBuilding.PropertyType);
            }

            // 3. Status Badge (Owned vs For Sale vs Flats Owned vs Civic)
            if (statusBadgeText != null)
            {
                string sText;
                if (currentBuilding.IsCivic)
                {
                    sText = LocalizationManager.Get("prop_civic_status");
                }
                else if (currentBuilding.IsOwned)
                {
                    sText = LocalizationManager.Get("prop_owned_by_you");
                }
                else if (currentBuilding.FlatsOwned > 0)
                {
                    sText = LocalizationManager.Get("prop_flats_owned_status", currentBuilding.FlatsOwned, currentBuilding.TotalFlats);
                }
                else
                {
                    sText = LocalizationManager.Get("prop_available_sale");
                }
                LocalizationManager.ApplyTo(statusBadgeText, sText);
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
                string pText = currentBuilding.IsCivic
                    ? LocalizationManager.Get("prop_civic_price")
                    : (LocalizationManager.CurrentLanguage == GameLanguage.Arabic ? $"{currentBuilding.PurchasePrice:N0}$" : $"${currentBuilding.PurchasePrice:N0}");
                LocalizationManager.ApplyTo(priceValueText, pText);
            }

            if (dailyIncomeValueText != null)
            {
                string incText = currentBuilding.IsCivic
                    ? LocalizationManager.Get("prop_civic_income")
                    : LocalizationManager.Get("prop_daily_rent_per_day", currentBuilding.DailyRentIncome);
                LocalizationManager.ApplyTo(dailyIncomeValueText, incText);
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
                    if (buyButtonText != null) LocalizationManager.ApplyLocalized(buyButtonText, "prop_not_for_sale");
                    if (buyButtonBg != null) buyButtonBg.color = new Color(0.25f, 0.30f, 0.40f, 0.7f);
                }
                else if (currentBuilding.IsOwned)
                {
                    buyPropertyButton.interactable = false;
                    if (buyButtonText != null) LocalizationManager.ApplyLocalized(buyButtonText, "prop_already_owned");
                    if (buyButtonBg != null) buyButtonBg.color = new Color(0.3f, 0.35f, 0.45f, 0.7f);
                }
                else
                {
                    buyPropertyButton.interactable = canAfford;
                    if (buyButtonText != null)
                    {
                        string label = currentBuilding.FlatsOwned > 0
                            ? LocalizationManager.Get("prop_buy_rest_button", effectiveBuildingPrice)
                            : LocalizationManager.Get("prop_buy_button_cash", effectiveBuildingPrice);
                        string finalBtn = canAfford ? label : LocalizationManager.Get("prop_insufficient_funds_btn", effectiveBuildingPrice);
                        LocalizationManager.ApplyTo(buyButtonText, finalBtn);
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
                        string cText = canCollect
                            ? LocalizationManager.Get("prop_collect_rent_amount", totalDue)
                            : (totalDue > 0 ? LocalizationManager.Get("prop_rent_collected_today") : LocalizationManager.Get("prop_no_rent_due"));
                        LocalizationManager.ApplyTo(collectRentButtonText, cText);
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
                    string fStats = LocalizationManager.Get("flat_stats_label", currentBuilding.FlatsOwned, currentBuilding.TotalFlats, currentBuilding.FlatsRentedOut);
                    LocalizationManager.ApplyTo(flatsCountText, fStats);
                }
                if (flatIncomeText != null)
                {
                    string fInc = LocalizationManager.Get("flat_income_label", currentBuilding.TotalDailyFlatRentIncome);
                    LocalizationManager.ApplyTo(flatIncomeText, fInc);
                }
                if (buyFlatButton != null)
                {
                    bool canAffordFlat = currentCash >= currentBuilding.FlatPurchasePrice;
                    bool hasUnbought = currentBuilding.FlatsOwned < currentBuilding.TotalFlats;
                    buyFlatButton.interactable = hasUnbought && canAffordFlat;
                    if (buyFlatButtonText != null)
                    {
                        string bfLabel = hasUnbought
                            ? LocalizationManager.Get("flat_buy_unit_btn", currentBuilding.FlatsOwned + 1, currentBuilding.FlatPurchasePrice)
                            : LocalizationManager.Get("flat_all_owned");
                        LocalizationManager.ApplyTo(buyFlatButtonText, bfLabel);
                    }
                }
                if (giveRentButton != null)
                {
                    bool canGiveRent = currentBuilding.FlatsRentedOut < currentBuilding.FlatsOwned;
                    giveRentButton.interactable = canGiveRent;
                    if (giveRentButtonText != null)
                    {
                        string grLabel = canGiveRent
                            ? LocalizationManager.Get("flat_lease_unit_btn", currentBuilding.FlatsRentedOut + 1, currentBuilding.FlatDailyRent)
                            : (currentBuilding.FlatsOwned == 0 ? LocalizationManager.Get("flat_buy_first") : LocalizationManager.Get("flat_all_leased"));
                        LocalizationManager.ApplyTo(giveRentButtonText, grLabel);
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
                    string dStatus = currentBuilding.IsOwned
                        ? LocalizationManager.Get("dining_vip_status", foodRestore, drinkRestore)
                        : LocalizationManager.Get("dining_regular_status", foodRestore, drinkRestore);
                    LocalizationManager.ApplyTo(diningStatusText, dStatus);
                }
                if (drinkButton != null)
                {
                    if (drinkButtonText != null)
                    {
                        string drkLabel = currentBuilding.IsOwned
                            ? LocalizationManager.Get("dining_drink_free", drinkRestore)
                            : LocalizationManager.Get("dining_drink_paid", drinkRestore, currentBuilding.DrinkPrice);
                        LocalizationManager.ApplyTo(drinkButtonText, drkLabel);
                    }
                }
                if (eatButton != null)
                {
                    if (eatButtonText != null)
                    {
                        string eatLabel = currentBuilding.IsOwned
                            ? LocalizationManager.Get("dining_eat_free", foodRestore)
                            : LocalizationManager.Get("dining_eat_paid", foodRestore, currentBuilding.FoodPrice);
                        LocalizationManager.ApplyTo(eatButtonText, eatLabel);
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
                    string feeLabel = LocalizationManager.Get("taxi_fee_label", currentBuilding.TaxiRentalFee);
                    LocalizationManager.ApplyTo(taxiRentalFeeText, feeLabel);
                }

                if (taxiFareRevenueText != null)
                {
                    string faresRange = LocalizationManager.Get("taxi_fares_range");
                    LocalizationManager.ApplyTo(taxiFareRevenueText, faresRange);
                }

                if (taxiNetProfitText != null)
                {
                    string actFleet = LocalizationManager.Get("taxi_active_fleet", currentBuilding.CurrentlyRentedTaxis, currentBuilding.MaxRentableTaxis);
                    LocalizationManager.ApplyTo(taxiNetProfitText, actFleet);
                }

                if (taxiFleetStatusText != null)
                {
                    string rentStatus = LocalizationManager.Get("taxi_rented_status", currentBuilding.CurrentlyRentedTaxis, currentBuilding.MaxRentableTaxis);
                    LocalizationManager.ApplyTo(taxiFleetStatusText, rentStatus);
                }

                if (rentTaxiButton != null)
                {
                    bool maxReached = currentBuilding.CurrentlyRentedTaxis >= currentBuilding.MaxRentableTaxis;
                    bool canAffordTaxi = currentCash >= currentBuilding.TaxiRentalFee;

                    rentTaxiButton.interactable = !maxReached && canAffordTaxi;
                    if (rentTaxiButtonText != null)
                    {
                        string rtText;
                        if (maxReached)
                        {
                            rtText = LocalizationManager.Get("taxi_max_active", currentBuilding.MaxRentableTaxis);
                        }
                        else
                        {
                            rtText = canAffordTaxi
                                ? LocalizationManager.Get("taxi_rent_btn", currentBuilding.CurrentlyRentedTaxis + 1, currentBuilding.TaxiRentalFee)
                                : LocalizationManager.Get("taxi_need_cash", currentBuilding.TaxiRentalFee);
                        }
                        LocalizationManager.ApplyTo(rentTaxiButtonText, rtText);
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
                    string matchTerm = isTennis ? LocalizationManager.Get("tennis_match_term") : LocalizationManager.Get("stadium_match_term");
                    string sStats = LocalizationManager.Get("stadium_stats_label", matchTerm, currentBuilding.MatchesHosted, currentBuilding.TotalMatchRevenue);
                    LocalizationManager.ApplyTo(stadiumStatsText, sStats);
                }
                if (requestMatchButton != null)
                {
                    requestMatchButton.interactable = currentBuilding.IsOwned;
                    if (requestMatchButtonText != null)
                    {
                        string venueTerm = isTennis ? LocalizationManager.Get("tennis_venue_term") : LocalizationManager.Get("stadium_venue_term");
                        string reqLabel = currentBuilding.IsOwned
                            ? LocalizationManager.Get("stadium_request_offer_btn")
                            : LocalizationManager.Get("stadium_buy_to_host_btn", venueTerm);
                        LocalizationManager.ApplyTo(requestMatchButtonText, reqLabel);
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
                bool needsHeal = isIll || curHp < maxHp - 0.5f;

                var diag = currentBuilding.GetHospitalTreatmentDiagnosis();
                double treatmentCost = diag.fee;
                bool canAffordTreatment = currentBuilding.IsOwned || currentCash >= treatmentCost;

                if (hospitalStatusText != null)
                {
                    string hStatus;
                    if (!needsHeal)
                    {
                        hStatus = LocalizationManager.Get("hosp_healthy_status");
                    }
                    else if (currentBuilding.IsOwned)
                    {
                        hStatus = LocalizationManager.Get("hosp_owner_status", diag.hexColor, diag.severityBadge, diag.conditionName, curHp, maxHp);
                    }
                    else
                    {
                        hStatus = LocalizationManager.Get("hosp_treatment_cost_status", diag.hexColor, diag.severityBadge, diag.conditionName, curHp, maxHp, treatmentCost);
                    }
                    LocalizationManager.ApplyTo(hospitalStatusText, hStatus);
                }

                if (treatmentButton != null)
                {
                    treatmentButton.interactable = needsHeal && canAffordTreatment;
                    if (treatmentButtonText != null)
                    {
                        string tbLabel;
                        if (!needsHeal)
                        {
                            tbLabel = LocalizationManager.Get("hosp_healthy_btn");
                        }
                        else if (currentBuilding.IsOwned)
                        {
                            tbLabel = LocalizationManager.Get("hosp_vip_treat_btn");
                        }
                        else
                        {
                            tbLabel = canAffordTreatment
                                ? LocalizationManager.Get("hosp_pay_treat_btn", treatmentCost)
                                : LocalizationManager.Get("hosp_need_cash_treat", treatmentCost);
                        }
                        LocalizationManager.ApplyTo(treatmentButtonText, tbLabel);
                    }

                    if (treatmentButtonBg != null)
                    {
                        if (!needsHeal || !canAffordTreatment)
                        {
                            treatmentButtonBg.color = new Color(0.40f, 0.44f, 0.48f, 0.85f);
                        }
                        else if (diag.severityBadge.Contains("CRITICAL") || diag.severityBadge.Contains("SERIOUS"))
                        {
                            treatmentButtonBg.color = new Color(0.92f, 0.22f, 0.26f, 1f); // Emergency Red
                        }
                        else
                        {
                            treatmentButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f); // Medical Emerald Green
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
                    string fStatus = LocalizationManager.Get("fact_stats_label", currentBuilding.ProductionBatchesCompleted, currentBuilding.TotalProductionRevenueCollected, currentBuilding.PendingProductionEarnings);
                    LocalizationManager.ApplyTo(factoryStatusText, fStatus);
                }
                if (collectProductionButton != null)
                {
                    bool hasEarnings = currentBuilding.PendingProductionEarnings > 0;
                    collectProductionButton.interactable = hasEarnings;
                    if (collectProductionButtonText != null)
                    {
                        string cpLabel = hasEarnings
                            ? LocalizationManager.Get("fact_collect_btn", currentBuilding.PendingProductionEarnings)
                            : (currentBuilding.IsOwned ? LocalizationManager.Get("fact_none_ready") : LocalizationManager.Get("fact_buy_to_earn"));
                        LocalizationManager.ApplyTo(collectProductionButtonText, cpLabel);
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
                    string oStatus = LocalizationManager.Get("oil_stats_label", currentBuilding.PendingOilRevenue);
                    LocalizationManager.ApplyTo(oilStatusText, oStatus);
                }
                if (collectOilButton != null)
                {
                    bool hasOilRev = currentBuilding.PendingOilRevenue > 0;
                    collectOilButton.interactable = hasOilRev;
                    if (collectOilButtonText != null)
                    {
                        string coLabel = hasOilRev
                            ? LocalizationManager.Get("oil_collect_btn", currentBuilding.PendingOilRevenue)
                            : (currentBuilding.IsOwned ? LocalizationManager.Get("oil_none_ready") : LocalizationManager.Get("oil_buy_to_earn"));
                        LocalizationManager.ApplyTo(collectOilButtonText, coLabel);
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
                    string pkStatus = LocalizationManager.Get("park_stats_label", currentBuilding.ParkVisitorsServed, currentBuilding.TotalParkRevenueCollected, currentBuilding.PendingParkRevenue);
                    LocalizationManager.ApplyTo(parkStatusText, pkStatus);
                }
                if (collectParkButton != null)
                {
                    bool hasParkRev = currentBuilding.PendingParkRevenue > 0;
                    collectParkButton.interactable = hasParkRev;
                    if (collectParkButtonText != null)
                    {
                        string cpkLabel = hasParkRev
                            ? LocalizationManager.Get("park_collect_btn", currentBuilding.PendingParkRevenue)
                            : (currentBuilding.IsOwned ? LocalizationManager.Get("park_none_ready") : LocalizationManager.Get("park_buy_to_earn"));
                        LocalizationManager.ApplyTo(collectParkButtonText, cpkLabel);
                    }
                    if (collectParkButtonBg != null)
                    {
                        collectParkButtonBg.color = hasParkRev
                            ? new Color(0.15f, 0.72f, 0.40f, 1f)
                            : new Color(0.45f, 0.45f, 0.45f, 0.8f);
                    }
                }
            }

            // 13c. Bank Operations Section
            bool isBank = currentBuilding.PropertyType == PropertyType.Bank;
            if (bankSectionRoot != null)
            {
                bankSectionRoot.SetActive(isBank);
            }
            if (isBank)
            {
                // Municipal bank cannot be purchased as private real estate and has no rental units
                if (buyPropertyButton != null) buyPropertyButton.gameObject.SetActive(false);
                if (collectRentButton != null) collectRentButton.gameObject.SetActive(false);
                if (flatSectionRoot != null) flatSectionRoot.SetActive(false);

                var bank = EmpireBuilder.Economy.BankService.Instance;
                bool hasAccount = bank != null && bank.HasBankAccount;
                double bankBal = bank != null ? bank.BankBalance : 0.0;
                double totalInt = bank != null ? bank.TotalInterestEarned : 0.0;

                if (bankClosedRoot != null) bankClosedRoot.SetActive(!hasAccount);
                if (bankActiveRoot != null) bankActiveRoot.SetActive(hasAccount);

                if (bankStatusText != null)
                {
                    string bStatus = !hasAccount
                        ? LocalizationManager.Get("bank_status_closed")
                        : LocalizationManager.Get("bank_status_active", bankBal, totalInt);
                    LocalizationManager.ApplyTo(bankStatusText, bStatus);
                }

                if (openBankButton != null)
                {
                    bool canAffordOpen = currentCash >= EmpireBuilder.Economy.BankService.AccountOpeningFee;
                    openBankButton.interactable = canAffordOpen;
                    if (openBankButtonText != null)
                    {
                        string opLabel = canAffordOpen
                            ? LocalizationManager.Get("bank_open_account_btn")
                            : LocalizationManager.Get("bank_need_coins_open");
                        LocalizationManager.ApplyTo(openBankButtonText, opLabel);
                    }
                    if (openBankButtonBg != null)
                    {
                        openBankButtonBg.color = canAffordOpen
                            ? new Color(0.12f, 0.78f, 0.38f, 1f)
                            : new Color(0.45f, 0.45f, 0.50f, 0.8f);
                    }
                }

                if (depositButton != null)
                {
                    depositButton.interactable = currentCash >= 1.0;
                    var dTxt = depositButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (dTxt != null) LocalizationManager.ApplyLocalized(dTxt, "bank_deposit_btn");
                }
                if (withdrawButton != null)
                {
                    withdrawButton.interactable = bankBal >= 1.0;
                    var wTxt = withdrawButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (wTxt != null) LocalizationManager.ApplyLocalized(wTxt, "bank_withdraw_btn");
                }
                if (withdrawAllButton != null)
                {
                    withdrawAllButton.interactable = bankBal >= 0.01;
                    var waTxt = withdrawAllButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (waTxt != null) LocalizationManager.ApplyLocalized(waTxt, "bank_withdraw_all_btn");
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
                    string mText = LocalizationManager.Get("maint_required_title", currentBuilding.PendingMaintenanceCost) + $"\n{LocalizationManager.FormatRaw(currentBuilding.MaintenanceReason)}";
                    LocalizationManager.ApplyTo(maintenanceReasonText, mText);
                }
                if (payMaintenanceButton != null)
                {
                    bool canAffordMaint = currentCash >= currentBuilding.PendingMaintenanceCost;
                    payMaintenanceButton.interactable = canAffordMaint;
                    if (payMaintenanceButtonText != null)
                    {
                        string pmLabel = canAffordMaint
                            ? LocalizationManager.Get("maint_pay_button", currentBuilding.PendingMaintenanceCost)
                            : LocalizationManager.Get("maint_insufficient_funds", currentBuilding.PendingMaintenanceCost);
                        LocalizationManager.ApplyTo(payMaintenanceButtonText, pmLabel);
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
                ShowToast(LocalizationManager.Get("prop_purchased_success", currentBuilding.PropertyName));
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                GameSoundManager.Instance?.PlayError();
                ShowToast(LocalizationManager.Get("prop_insufficient_funds"));
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
                ShowToast(LocalizationManager.Get("prop_rent_collected", amount, currentBuilding.PropertyName));
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

            LocalizationManager.ApplyTo(toastMessageText, message);
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
            return PlayerPrefs.GetFloat("SavedPlayerCash", 5000f);
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
            CreateHospitalSection(card.transform);

            // 8f. Factory Industrial Section
            CreateFactorySection(card.transform);

            // 8g. Oil & Petroleum Station Section
            CreateOilSection(card.transform);

            // 8g2. Park Visitor Entry Fee Section
            CreateParkSection(card.transform);

            // 8g3. Bank Operations Section
            CreateBankSection(card.transform);

            // 8h. Building Maintenance Alert Section (Positioned at bottom of section slot)
            CreateMaintenanceSection(card.transform);

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

        private void CreateHospitalSection(Transform card)
        {
            if (card == null) return;
            hospitalSectionRoot = CreateUIElement("HospitalSection", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 165), new Vector2(0, -260));
            Image hospBg = hospitalSectionRoot.AddComponent<Image>();
            hospBg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);
            if (roundedBoxSprite != null)
            {
                hospBg.sprite = roundedBoxSprite;
                hospBg.type = Image.Type.Sliced;
            }

            // Title
            TextMeshProUGUI hospTitle = CreateTextMesh("HospTitle", hospitalSectionRoot.transform, "MEDICAL HEALTHCARE & EMERGENCY TREATMENT", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.42f, 0.46f));
            RectTransform htRt = hospTitle.GetComponent<RectTransform>();
            htRt.anchorMin = new Vector2(0, 1);
            htRt.anchorMax = new Vector2(1, 1);
            htRt.pivot = new Vector2(0, 1);
            htRt.anchoredPosition = new Vector2(15, -10);
            htRt.sizeDelta = new Vector2(-30, 22);

            // Status text
            hospitalStatusText = CreateTextMesh("HospitalStatus", hospitalSectionRoot.transform, "Visit for medical treatment when sick or exhausted.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.92f, 0.92f, 0.94f));
            RectTransform hsRt = hospitalStatusText.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0, 1);
            hsRt.anchorMax = new Vector2(1, 1);
            hsRt.pivot = new Vector2(0, 1);
            hsRt.anchoredPosition = new Vector2(15, -34);
            hsRt.sizeDelta = new Vector2(-30, 60);

            // Dedicated Treatment Button
            GameObject treatBtnObj = CreateUIElement("TreatmentButton", hospitalSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 48), new Vector2(0, 12));
            treatmentButtonBg = treatBtnObj.AddComponent<Image>();
            treatmentButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedBoxSprite != null)
            {
                treatmentButtonBg.sprite = roundedBoxSprite;
                treatmentButtonBg.type = Image.Type.Sliced;
            }
            treatmentButton = treatBtnObj.AddComponent<Button>();
            treatmentButtonText = CreateTextMesh("Text", treatBtnObj.transform, "RECEIVE TREATMENT (RESTORE 100 HP)", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform tbTextRt = treatmentButtonText.GetComponent<RectTransform>();
            tbTextRt.anchorMin = Vector2.zero;
            tbTextRt.anchorMax = Vector2.one;
            tbTextRt.sizeDelta = Vector2.zero;

            treatmentButton.onClick.RemoveAllListeners();
            treatmentButton.onClick.AddListener(OnTreatmentClicked);
        }

        private void CreateFactorySection(Transform card)
        {
            if (card == null) return;
            factorySectionRoot = CreateUIElement("FactorySection", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image factBg = factorySectionRoot.AddComponent<Image>();
            factBg.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);
            if (roundedBoxSprite != null) { factBg.sprite = roundedBoxSprite; factBg.type = Image.Type.Sliced; }

            TextMeshProUGUI factTitle = CreateTextMesh("FactTitle", factorySectionRoot.transform, "INDUSTRIAL MANUFACTURING PRODUCTION", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.45f, 0.75f, 1f));
            RectTransform ftRt = factTitle.GetComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0, 1); ftRt.anchorMax = new Vector2(1, 1); ftRt.pivot = new Vector2(0, 1);
            ftRt.anchoredPosition = new Vector2(15, -10); ftRt.sizeDelta = new Vector2(-30, 22);

            factoryStatusText = CreateTextMesh("FactoryStatus", factorySectionRoot.transform, "Heavy manufacturing plant. Completes production runs with large payout revenues.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.85f, 0.88f, 0.92f));
            RectTransform fsRt = factoryStatusText.GetComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0, 1); fsRt.anchorMax = new Vector2(1, 1); fsRt.pivot = new Vector2(0, 1);
            fsRt.anchoredPosition = new Vector2(15, -34); fsRt.sizeDelta = new Vector2(-30, 34);

            GameObject prodBtnObj = CreateUIElement("CollectProductionButton", factorySectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectProductionButtonBg = prodBtnObj.AddComponent<Image>();
            collectProductionButtonBg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            if (roundedBoxSprite != null) { collectProductionButtonBg.sprite = roundedBoxSprite; collectProductionButtonBg.type = Image.Type.Sliced; }
            collectProductionButton = prodBtnObj.AddComponent<Button>();
            collectProductionButtonText = CreateTextMesh("Text", prodBtnObj.transform, "COLLECT PRODUCTION REVENUE", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pbTextRt = collectProductionButtonText.GetComponent<RectTransform>();
            pbTextRt.anchorMin = Vector2.zero; pbTextRt.anchorMax = Vector2.one; pbTextRt.sizeDelta = Vector2.zero;
            collectProductionButton.onClick.RemoveAllListeners();
            collectProductionButton.onClick.AddListener(OnCollectProductionClicked);
        }

        private void CreateOilSection(Transform card)
        {
            if (card == null) return;
            oilSectionRoot = CreateUIElement("OilSection", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image oilBg = oilSectionRoot.AddComponent<Image>();
            oilBg.color = new Color(0.22f, 0.18f, 0.12f, 0.95f);
            if (roundedBoxSprite != null) { oilBg.sprite = roundedBoxSprite; oilBg.type = Image.Type.Sliced; }

            TextMeshProUGUI oilTitle = CreateTextMesh("OilTitle", oilSectionRoot.transform, "OIL & FUEL DISTRIBUTION SALES", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.82f, 0.25f));
            RectTransform otRt = oilTitle.GetComponent<RectTransform>();
            otRt.anchorMin = new Vector2(0, 1); otRt.anchorMax = new Vector2(1, 1); otRt.pivot = new Vector2(0, 1);
            otRt.anchoredPosition = new Vector2(15, -10); otRt.sizeDelta = new Vector2(-30, 22);

            oilStatusText = CreateTextMesh("OilStatus", oilSectionRoot.transform, "Automotive refueling center. Earns regular revenue payouts from vehicle traffic.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.92f, 0.90f, 0.85f));
            RectTransform osRt = oilStatusText.GetComponent<RectTransform>();
            osRt.anchorMin = new Vector2(0, 1); osRt.anchorMax = new Vector2(1, 1); osRt.pivot = new Vector2(0, 1);
            osRt.anchoredPosition = new Vector2(15, -34); osRt.sizeDelta = new Vector2(-30, 34);

            GameObject oilBtnObj = CreateUIElement("CollectOilButton", oilSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectOilButtonBg = oilBtnObj.AddComponent<Image>();
            collectOilButtonBg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            if (roundedBoxSprite != null) { collectOilButtonBg.sprite = roundedBoxSprite; collectOilButtonBg.type = Image.Type.Sliced; }
            collectOilButton = oilBtnObj.AddComponent<Button>();
            collectOilButtonText = CreateTextMesh("Text", oilBtnObj.transform, "COLLECT OIL REVENUE", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            RectTransform obTextRt = collectOilButtonText.GetComponent<RectTransform>();
            obTextRt.anchorMin = Vector2.zero; obTextRt.anchorMax = Vector2.one; obTextRt.sizeDelta = Vector2.zero;
            collectOilButton.onClick.RemoveAllListeners();
            collectOilButton.onClick.AddListener(OnCollectOilClicked);
        }

        private void CreateParkSection(Transform card)
        {
            if (card == null) return;
            parkSectionRoot = CreateUIElement("ParkSection", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image parkBg = parkSectionRoot.AddComponent<Image>();
            parkBg.color = new Color(0.12f, 0.24f, 0.16f, 0.95f);
            if (roundedBoxSprite != null) { parkBg.sprite = roundedBoxSprite; parkBg.type = Image.Type.Sliced; }

            TextMeshProUGUI parkTitle = CreateTextMesh("ParkTitle", parkSectionRoot.transform, "GRAND PARK VISITOR ADMISSION FEES", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.35f, 0.92f, 0.55f));
            RectTransform pktRt = parkTitle.GetComponent<RectTransform>();
            pktRt.anchorMin = new Vector2(0, 1); pktRt.anchorMax = new Vector2(1, 1); pktRt.pivot = new Vector2(0, 1);
            pktRt.anchoredPosition = new Vector2(15, -10); pktRt.sizeDelta = new Vector2(-30, 22);

            parkStatusText = CreateTextMesh("ParkStatus", parkSectionRoot.transform, "Scenic city park. Strolling citizens pay admission entry fees periodically.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.85f, 0.95f, 0.88f));
            RectTransform pksRt = parkStatusText.GetComponent<RectTransform>();
            pksRt.anchorMin = new Vector2(0, 1); pksRt.anchorMax = new Vector2(1, 1); pksRt.pivot = new Vector2(0, 1);
            pksRt.anchoredPosition = new Vector2(15, -34); pksRt.sizeDelta = new Vector2(-30, 34);

            GameObject parkBtnObj = CreateUIElement("CollectParkButton", parkSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            collectParkButtonBg = parkBtnObj.AddComponent<Image>();
            collectParkButtonBg.color = new Color(0.15f, 0.72f, 0.40f, 1f);
            if (roundedBoxSprite != null) { collectParkButtonBg.sprite = roundedBoxSprite; collectParkButtonBg.type = Image.Type.Sliced; }
            collectParkButton = parkBtnObj.AddComponent<Button>();
            collectParkButtonText = CreateTextMesh("Text", parkBtnObj.transform, "COLLECT PARK ENTRY FEES", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pkbTextRt = collectParkButtonText.GetComponent<RectTransform>();
            pkbTextRt.anchorMin = Vector2.zero; pkbTextRt.anchorMax = Vector2.one; pkbTextRt.sizeDelta = Vector2.zero;
            collectParkButton.onClick.RemoveAllListeners();
            collectParkButton.onClick.AddListener(OnCollectParkRevenueClicked);
        }

        private void CreateMaintenanceSection(Transform card)
        {
            if (card == null) return;
            maintenanceSectionRoot = CreateUIElement("MaintenanceSection", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 72), new Vector2(0, 136));
            Image maintBg = maintenanceSectionRoot.AddComponent<Image>();
            maintBg.color = new Color(0.32f, 0.12f, 0.12f, 0.95f);
            if (roundedBoxSprite != null) { maintBg.sprite = roundedBoxSprite; maintBg.type = Image.Type.Sliced; }

            maintenanceReasonText = CreateTextMesh("MaintenanceReason", maintenanceSectionRoot.transform, "MAINTENANCE REQUIRED: -$0", 11, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.75f, 0.75f));
            RectTransform mrRt = maintenanceReasonText.GetComponent<RectTransform>();
            mrRt.anchorMin = new Vector2(0, 0.5f); mrRt.anchorMax = new Vector2(0.55f, 1); mrRt.pivot = new Vector2(0, 0.5f);
            mrRt.anchoredPosition = new Vector2(10, 0); mrRt.sizeDelta = new Vector2(-15, 0);

            GameObject payMaintBtnObj = CreateUIElement("PayMaintenanceButton", maintenanceSectionRoot.transform, new Vector2(0.55f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            payMaintenanceButtonBg = payMaintBtnObj.AddComponent<Image>();
            payMaintenanceButtonBg.color = new Color(0.90f, 0.35f, 0.15f, 1f);
            if (roundedBoxSprite != null) { payMaintenanceButtonBg.sprite = roundedBoxSprite; payMaintenanceButtonBg.type = Image.Type.Sliced; }
            payMaintenanceButton = payMaintBtnObj.AddComponent<Button>();
            payMaintenanceButtonText = CreateTextMesh("Text", payMaintBtnObj.transform, "PAY MAINTENANCE", 11, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform pmbTextRt = payMaintenanceButtonText.GetComponent<RectTransform>();
            pmbTextRt.anchorMin = Vector2.zero; pmbTextRt.anchorMax = Vector2.one; pmbTextRt.sizeDelta = Vector2.zero;
            payMaintenanceButton.onClick.RemoveAllListeners();
            payMaintenanceButton.onClick.AddListener(OnPayMaintenanceClicked);
            maintenanceSectionRoot.SetActive(false);
        }

        private void CreateBankSection(Transform card)
        {
            if (card == null) return;
            bankSectionRoot = CreateUIElement("BankSection", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 195), new Vector2(0, -250));
            Image bankBg = bankSectionRoot.AddComponent<Image>();
            bankBg.color = new Color(0.10f, 0.14f, 0.22f, 0.98f);
            if (roundedBoxSprite != null) { bankBg.sprite = roundedBoxSprite; bankBg.type = Image.Type.Sliced; }

            // Title
            TextMeshProUGUI bTitle = CreateTextMesh("BankTitle", bankSectionRoot.transform, "CAPITAL NATIONAL BANK & SAVINGS", 13, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.85f, 0.25f));
            RectTransform btRt = bTitle.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0, 1); btRt.anchorMax = new Vector2(1, 1); btRt.pivot = new Vector2(0, 1);
            btRt.anchoredPosition = new Vector2(15, -10); btRt.sizeDelta = new Vector2(-30, 22);

            // Status Text
            bankStatusText = CreateTextMesh("BankStatus", bankSectionRoot.transform, "Open a savings account to store coins and earn daily compound interest.", 11, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.90f, 0.92f, 0.96f));
            RectTransform bsRt = bankStatusText.GetComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0, 1); bsRt.anchorMax = new Vector2(1, 1); bsRt.pivot = new Vector2(0, 1);
            bsRt.anchoredPosition = new Vector2(15, -34); bsRt.sizeDelta = new Vector2(-30, 48);

            // 1. Account Closed Sub-Panel (For opening account)
            bankClosedRoot = CreateUIElement("BankClosedRoot", bankSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, -25));
            GameObject openBtnObj = CreateUIElement("OpenBankButton", bankClosedRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 46), new Vector2(0, 15));
            openBankButtonBg = openBtnObj.AddComponent<Image>();
            openBankButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedBoxSprite != null) { openBankButtonBg.sprite = roundedBoxSprite; openBankButtonBg.type = Image.Type.Sliced; }
            openBankButton = openBtnObj.AddComponent<Button>();
            openBankButtonText = CreateTextMesh("Text", openBtnObj.transform, "OPEN BANK ACCOUNT (FEE: 100 COINS)", 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform obTextRt = openBankButtonText.GetComponent<RectTransform>();
            obTextRt.anchorMin = Vector2.zero; obTextRt.anchorMax = Vector2.one; obTextRt.sizeDelta = Vector2.zero;
            openBankButton.onClick.RemoveAllListeners();
            openBankButton.onClick.AddListener(OnOpenBankAccountClicked);

            // 2. Account Active Sub-Panel (For deposits, withdrawals, and quick buttons)
            bankActiveRoot = CreateUIElement("BankActiveRoot", bankSectionRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 108), new Vector2(0, 6));

            // Quick Amount Buttons Row
            GameObject quickRow = CreateUIElement("QuickRow", bankActiveRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-30, 26), new Vector2(0, 0));
            CreateQuickButton("+$100", quickRow.transform, new Vector2(0, 0), new Vector2(0.23f, 1), () => OnQuickDepositClicked(100));
            CreateQuickButton("+$500", quickRow.transform, new Vector2(0.25f, 0), new Vector2(0.48f, 1), () => OnQuickDepositClicked(500));
            CreateQuickButton("+$1K", quickRow.transform, new Vector2(0.50f, 0), new Vector2(0.73f, 1), () => OnQuickDepositClicked(1000));
            CreateQuickButton("MAX CASH", quickRow.transform, new Vector2(0.75f, 0), new Vector2(1f, 1), OnQuickDepositMaxCash);

            // Input Field Row
            bankDepositInputField = CreateInputField("BankAmountInput", bankActiveRoot.transform, new Vector2(0, 0), new Vector2(0.45f, 0), new Vector2(0, 0), new Vector2(0, 38), new Vector2(15, 34), "Amount...");

            // Deposit Button
            GameObject depBtnObj = CreateUIElement("DepositButton", bankActiveRoot.transform, new Vector2(0.48f, 0), new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(0, 34));
            depositButtonBg = depBtnObj.AddComponent<Image>();
            depositButtonBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedBoxSprite != null) { depositButtonBg.sprite = roundedBoxSprite; depositButtonBg.type = Image.Type.Sliced; }
            depositButton = depBtnObj.AddComponent<Button>();
            depositButtonText = CreateTextMesh("Text", depBtnObj.transform, "DEPOSIT", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform dbTextRt = depositButtonText.GetComponent<RectTransform>();
            dbTextRt.anchorMin = Vector2.zero; dbTextRt.anchorMax = Vector2.one; dbTextRt.sizeDelta = Vector2.zero;
            depositButton.onClick.RemoveAllListeners();
            depositButton.onClick.AddListener(OnDepositClicked);

            // Withdraw Button
            GameObject wthBtnObj = CreateUIElement("WithdrawButton", bankActiveRoot.transform, new Vector2(0.75f, 0), new Vector2(1f, 0), new Vector2(0.5f, 0), new Vector2(-15, 38), new Vector2(0, 34));
            withdrawButtonBg = wthBtnObj.AddComponent<Image>();
            withdrawButtonBg.color = new Color(0.92f, 0.45f, 0.15f, 1f);
            if (roundedBoxSprite != null) { withdrawButtonBg.sprite = roundedBoxSprite; withdrawButtonBg.type = Image.Type.Sliced; }
            withdrawButton = wthBtnObj.AddComponent<Button>();
            withdrawButtonText = CreateTextMesh("Text", wthBtnObj.transform, "WITHDRAW", 12, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform wbTextRt = withdrawButtonText.GetComponent<RectTransform>();
            wbTextRt.anchorMin = Vector2.zero; wbTextRt.anchorMax = Vector2.one; wbTextRt.sizeDelta = Vector2.zero;
            withdrawButton.onClick.RemoveAllListeners();
            withdrawButton.onClick.AddListener(OnWithdrawClicked);

            // Withdraw All Button (bottom compact row)
            GameObject wAllBtnObj = CreateUIElement("WithdrawAllButton", bankActiveRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 24), new Vector2(0, 2));
            Image wAllImg = wAllBtnObj.AddComponent<Image>();
            wAllImg.color = new Color(0.24f, 0.30f, 0.40f, 0.90f);
            if (roundedBoxSprite != null) { wAllImg.sprite = roundedBoxSprite; wAllImg.type = Image.Type.Sliced; }
            withdrawAllButton = wAllBtnObj.AddComponent<Button>();
            withdrawAllButtonText = CreateTextMesh("Text", wAllBtnObj.transform, "WITHDRAW ALL TO POCKET CASH", 11, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.95f, 0.95f));
            RectTransform wabTextRt = withdrawAllButtonText.GetComponent<RectTransform>();
            wabTextRt.anchorMin = Vector2.zero; wabTextRt.anchorMax = Vector2.one; wabTextRt.sizeDelta = Vector2.zero;
            withdrawAllButton.onClick.RemoveAllListeners();
            withdrawAllButton.onClick.AddListener(OnWithdrawAllClicked);

            bankSectionRoot.SetActive(false);
        }

        private Button CreateQuickButton(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = CreateUIElement("QuickBtn_" + label, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.24f, 0.35f, 0.95f);
            if (roundedBoxSprite != null) { bg.sprite = roundedBoxSprite; bg.type = Image.Type.Sliced; }
            Button btn = btnObj.AddComponent<Button>();
            TextMeshProUGUI txt = CreateTextMesh("Text", btnObj.transform, label, 11, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            RectTransform trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            btn.onClick.AddListener(action);
            return btn;
        }

        private TMP_InputField CreateInputField(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos, string placeholderText)
        {
            GameObject root = CreateUIElement(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPos);
            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            if (roundedBoxSprite != null)
            {
                bg.sprite = roundedBoxSprite;
                bg.type = Image.Type.Sliced;
            }

            GameObject textArea = CreateUIElement("TextArea", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-16, -6), Vector2.zero);
            textArea.AddComponent<RectMask2D>();

            TextMeshProUGUI placeholder = CreateTextMesh("Placeholder", textArea.transform, placeholderText, 12, FontStyles.Italic, TextAlignmentOptions.Left, new Color(0.55f, 0.60f, 0.70f, 0.7f));
            RectTransform phRt = placeholder.rectTransform;
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = CreateTextMesh("Text", textArea.transform, "", 13, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
            RectTransform tRt = text.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            TMP_InputField inputField = root.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.characterLimit = 9;

            return inputField;
        }

        private void OnOpenBankAccountClicked()
        {
            if (EmpireBuilder.Economy.BankService.Instance == null) return;
            if (EmpireBuilder.Economy.BankService.Instance.TryOpenAccount(out string msg))
            {
                GameSoundManager.Instance?.PlayBuy(currentBuilding != null ? currentBuilding.transform.position : transform.position);
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

        private void OnDepositClicked()
        {
            if (EmpireBuilder.Economy.BankService.Instance == null) return;
            if (bankDepositInputField == null) return;

            string input = bankDepositInputField.text.Trim();
            if (!double.TryParse(input, out double amount) || amount <= 0)
            {
                ShowToast("Please enter a valid deposit amount greater than 0!");
                GameSoundManager.Instance?.PlayError();
                return;
            }

            if (EmpireBuilder.Economy.BankService.Instance.TryDeposit(amount, out string msg))
            {
                GameSoundManager.Instance?.PlayCollect();
                bankDepositInputField.text = "";
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

        private void OnWithdrawClicked()
        {
            if (EmpireBuilder.Economy.BankService.Instance == null) return;
            if (bankDepositInputField == null) return;

            string input = bankDepositInputField.text.Trim();
            if (!double.TryParse(input, out double amount) || amount <= 0)
            {
                ShowToast("Please enter a valid amount to withdraw!");
                GameSoundManager.Instance?.PlayError();
                return;
            }

            if (EmpireBuilder.Economy.BankService.Instance.TryWithdraw(amount, out string msg))
            {
                GameSoundManager.Instance?.PlayCollect();
                bankDepositInputField.text = "";
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

        private void OnWithdrawAllClicked()
        {
            if (EmpireBuilder.Economy.BankService.Instance == null) return;
            double all = EmpireBuilder.Economy.BankService.Instance.BankBalance;
            if (all <= 0.01)
            {
                ShowToast("Your bank balance is currently empty!");
                return;
            }

            if (EmpireBuilder.Economy.BankService.Instance.TryWithdraw(all, out string msg))
            {
                GameSoundManager.Instance?.PlayCollect();
                if (bankDepositInputField != null) bankDepositInputField.text = "";
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

        private void OnQuickDepositClicked(double amt)
        {
            if (bankDepositInputField != null)
            {
                bankDepositInputField.text = ((int)amt).ToString();
            }
        }

        private void OnQuickDepositMaxCash()
        {
            if (bankDepositInputField != null)
            {
                double cash = GetPlayerCash();
                bankDepositInputField.text = ((int)cash).ToString();
            }
        }
        #endregion
    }
}
