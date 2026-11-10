using System;
using System.Collections;
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

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            RefreshUI();
        }

        public void Close()
        {
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

            // 3. Status Badge (Owned vs For Sale)
            if (statusBadgeText != null)
            {
                statusBadgeText.text = currentBuilding.IsOwned ? "OWNED BY YOU" : "AVAILABLE FOR SALE";
            }

            if (statusBadgeBg != null)
            {
                statusBadgeBg.color = currentBuilding.IsOwned
                    ? new Color(0.18f, 0.75f, 0.44f, 1f) // Emerald Green
                    : new Color(0.96f, 0.65f, 0.15f, 1f); // Warm Gold
            }

            // 4. Financial Stats
            if (priceValueText != null)
            {
                priceValueText.text = $"${currentBuilding.PurchasePrice:N0}";
            }

            if (dailyIncomeValueText != null)
            {
                dailyIncomeValueText.text = $"+${currentBuilding.DailyRentIncome:N0} / day";
            }

            // 5. Buy Property Button State
            double currentCash = GetPlayerCash();
            bool canAfford = currentCash >= currentBuilding.PurchasePrice;

            if (buyPropertyButton != null)
            {
                if (currentBuilding.IsOwned)
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
                        buyButtonText.text = canAfford
                            ? $"BUY PROPERTY (${currentBuilding.PurchasePrice:N0})"
                            : $"INSUFFICIENT FUNDS (${currentBuilding.PurchasePrice:N0})";
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
                bool showCollect = currentBuilding.IsOwned;
                collectRentButton.gameObject.SetActive(showCollect);
                if (showCollect)
                {
                    collectRentButton.interactable = currentBuilding.RentAvailableToCollect;
                    if (collectRentButtonText != null)
                    {
                        collectRentButtonText.text = currentBuilding.RentAvailableToCollect
                            ? $"COLLECT RENT (+${currentBuilding.DailyRentIncome:N0})"
                            : "RENT COLLECTED TODAY";
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
            bool isDining = currentBuilding.PropertyType == PropertyType.Restaurant;
            if (restaurantSectionRoot != null)
            {
                restaurantSectionRoot.SetActive(isDining);
            }
            if (isDining)
            {
                if (diningStatusText != null)
                {
                    diningStatusText.text = currentBuilding.IsOwned
                        ? "VIP Owner Status: Free dining & drinks! Customer dining earns you daily profits."
                        : "Relax with fine coffee and gourmet dishes, or buy the establishment!";
                }
                if (drinkButton != null)
                {
                    if (drinkButtonText != null)
                    {
                        drinkButtonText.text = currentBuilding.IsOwned
                            ? $"DRINK {currentBuilding.DrinkName.ToUpper()} (FREE - OWNER)"
                            : $"DRINK {currentBuilding.DrinkName.ToUpper()} (${currentBuilding.DrinkPrice:N0})";
                    }
                }
                if (eatButton != null)
                {
                    if (eatButtonText != null)
                    {
                        eatButtonText.text = currentBuilding.IsOwned
                            ? $"EAT {currentBuilding.FoodName.ToUpper()} (FREE - OWNER)"
                            : $"EAT {currentBuilding.FoodName.ToUpper()} (${currentBuilding.FoodPrice:N0})";
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

            // 10. Stadium Section
            bool isStadium = currentBuilding.PropertyType == PropertyType.Stadium;
            if (stadiumSectionRoot != null)
            {
                stadiumSectionRoot.SetActive(isStadium);
            }
            if (isStadium)
            {
                if (stadiumStatsText != null)
                {
                    stadiumStatsText.text = $"Matches Hosted: {currentBuilding.MatchesHosted}  |  Total Match Rent: ${currentBuilding.TotalMatchRevenue:N0}";
                }
                if (requestMatchButton != null)
                {
                    requestMatchButton.interactable = currentBuilding.IsOwned;
                    if (requestMatchButtonText != null)
                    {
                        requestMatchButtonText.text = currentBuilding.IsOwned
                            ? "REQUEST MATCH RENT OFFER NOW"
                            : "BUY STADIUM TO HOST MATCHES";
                    }
                }
            }
        }

        private void OnBuyFlatClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.BuyFlat(out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                ShowToast(msg);
            }
        }

        private void OnGiveRentClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.GiveRentForFlat(out string msg))
            {
                ShowToast(msg);
                RefreshUI();
            }
            else
            {
                ShowToast(msg);
            }
        }

        private void OnDrinkClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.DrinkBeverage(out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                ShowToast(msg);
            }
        }

        private void OnEatClicked()
        {
            if (currentBuilding == null) return;
            if (currentBuilding.EatFood(out string msg))
            {
                ShowToast(msg);
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
                ShowToast(msg);
            }
        }

        private void OnRequestMatchClicked()
        {
            if (currentBuilding == null) return;
            currentBuilding.TriggerExhibitionMatchRequest();
        }

        private void OnBuyClicked()
        {
            if (currentBuilding == null) return;

            bool success = currentBuilding.BuyProperty();
            if (success)
            {
                ShowToast($"Congratulations! You purchased {currentBuilding.PropertyName}!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
            else
            {
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
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
            }
        }

        private void OnCollectRentClicked()
        {
            if (currentBuilding == null) return;

            if (currentBuilding.CollectRent(out double amount))
            {
                ShowToast($"Collected +${amount:N0} in property rent income!");
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
                RefreshUI();
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
                case PropertyType.Stadium:
                    return new Color(0.92f, 0.42f, 0.18f, 1f); // Vibrant Coral
                case PropertyType.TaxiStation:
                    return new Color(0.98f, 0.74f, 0.12f, 1f); // Taxi Yellow
                case PropertyType.Restaurant:
                    return new Color(0.88f, 0.28f, 0.35f, 1f); // Crimson
                case PropertyType.Apartment:
                    return new Color(0.18f, 0.65f, 0.92f, 1f); // Cyan Blue
                case PropertyType.House:
                    return new Color(0.28f, 0.75f, 0.42f, 1f); // Forest Green
                case PropertyType.Hospital:
                    return new Color(0.90f, 0.22f, 0.25f, 1f); // Red
                case PropertyType.Market:
                    return new Color(0.95f, 0.58f, 0.15f, 1f); // Amber
                case PropertyType.Commercial:
                    return new Color(0.55f, 0.38f, 0.88f, 1f); // Royal Purple
                case PropertyType.Industrial:
                    return new Color(0.48f, 0.54f, 0.62f, 1f); // Slate
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
