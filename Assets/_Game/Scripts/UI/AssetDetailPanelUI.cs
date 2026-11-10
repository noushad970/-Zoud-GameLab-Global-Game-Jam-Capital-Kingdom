using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using EmpireBuilder.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class AssetDetailPanelUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Information Labels")]
        [SerializeField] private TextMeshProUGUI assetTitleText;
        [SerializeField] private TextMeshProUGUI assetTypeText;
        [SerializeField] private TextMeshProUGUI conditionText;
        [SerializeField] private Slider conditionSlider;
        [SerializeField] private TextMeshProUGUI grossIncomeText;
        [SerializeField] private TextMeshProUGUI operatingCostText;
        [SerializeField] private TextMeshProUGUI netDailyText;
        [SerializeField] private TextMeshProUGUI paybackPeriodText;

        [Header("Action Buttons")]
        [SerializeField] private Button buyButton;
        [SerializeField] private TextMeshProUGUI buyButtonText;
        [SerializeField] private Button repairButton;
        [SerializeField] private TextMeshProUGUI repairButtonText;
        [SerializeField] private Button sellButton;
        [SerializeField] private TextMeshProUGUI sellButtonText;
        [SerializeField] private Button closeButton;

        private WorldAssetSlot currentSlot;
        private OwnedAsset currentAsset;
        private AssetDefinition currentDef;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
            if (repairButton != null) repairButton.onClick.AddListener(OnRepairClicked);
            if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);

            WorldAssetSlot.OnSlotSelected += InspectSlot;

            Close();
        }

        private void OnDestroy()
        {
            WorldAssetSlot.OnSlotSelected -= InspectSlot;
        }

        public void InspectSlot(WorldAssetSlot slot)
        {
            if (slot == null) return;
            currentSlot = slot;
            currentAsset = slot.BoundAsset;
            currentDef = slot.defaultPurchasableAsset;

            RefreshView();
            panelRoot.SetActive(true);
        }

        public void InspectAssetDirect(OwnedAsset asset, AssetDefinition def)
        {
            currentSlot = null;
            currentAsset = asset;
            currentDef = def;

            RefreshView();
            panelRoot.SetActive(true);
        }

        private void RefreshView()
        {
            if (currentDef == null) return;

            if (assetTitleText != null)
            {
                assetTitleText.text = currentAsset != null ? currentAsset.customName : currentDef.displayName;
            }

            if (assetTypeText != null)
            {
                assetTypeText.text = $"Category: {currentDef.assetType}";
            }

            // Condition
            float cond = currentAsset != null ? currentAsset.condition : currentDef.startingCondition;
            if (conditionText != null) conditionText.text = $"Condition: {cond:F0}%";
            if (conditionSlider != null) conditionSlider.value = cond / 100f;

            // Financial Metrics
            if (grossIncomeText != null) grossIncomeText.text = $"Gross Revenue: ${currentDef.grossIncomePerDay:N0}/day";
            if (operatingCostText != null) operatingCostText.text = $"Operating Costs: ${currentDef.operatingCostPerDay:N0}/day";
            if (netDailyText != null)
            {
                double net = currentDef.ExpectedNetDailyIncome;
                string sign = net >= 0 ? "+" : "-";
                netDailyText.text = $"Net Cash Flow: {sign}${System.Math.Abs(net):N0}/day";
                netDailyText.color = net >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
            }

            if (paybackPeriodText != null)
            {
                paybackPeriodText.text = $"Estimated Payback: {currentDef.GetPaybackString()}";
            }

            // Buttons state
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (GameManager.Instance == null) return;

            bool isOwned = currentAsset != null && currentAsset.state != AssetState.Sold;

            // Buy Button
            if (buyButton != null)
            {
                buyButton.gameObject.SetActive(!isOwned);
                if (!isOwned)
                {
                    double price = currentDef.purchasePrice;
                    bool canAfford = GameManager.Instance.Economy.CanAfford(price);
                    buyButton.interactable = canAfford;
                    if (buyButtonText != null) buyButtonText.text = $"Buy (${price:N0})";
                }
            }

            // Repair Button
            if (repairButton != null)
            {
                repairButton.gameObject.SetActive(isOwned);
                if (isOwned)
                {
                    double repairCost = GameManager.Instance.Maintenance.CalculateRepairCost(currentAsset, currentDef);
                    bool needsRepair = currentAsset.condition < 100f;
                    repairButton.interactable = needsRepair && GameManager.Instance.Economy.CanAfford(repairCost);
                    if (repairButtonText != null)
                    {
                        repairButtonText.text = needsRepair ? $"Repair (${repairCost:N0})" : "Repaired (100%)";
                    }
                }
            }

            // Sell Button
            if (sellButton != null)
            {
                sellButton.gameObject.SetActive(isOwned);
                if (isOwned)
                {
                    double saleVal = (currentAsset.currentMarketValue * (currentAsset.condition / 100.0)) * 0.7;
                    if (sellButtonText != null) sellButtonText.text = $"Sell (${saleVal:N0})";
                }
            }
        }

        private void OnBuyClicked()
        {
            if (GameManager.Instance == null || currentDef == null) return;

            if (GameManager.Instance.Economy.TryPurchaseAsset(currentDef, GameManager.Instance.Simulation.CurrentDay, out var newAsset))
            {
                GameManager.Instance.Assets.AddAsset(newAsset);
                if (currentSlot != null)
                {
                    currentSlot.BindAsset(newAsset);
                }
                currentAsset = newAsset;
                RefreshView();
            }
        }

        private void OnRepairClicked()
        {
            if (GameManager.Instance == null || currentAsset == null || currentDef == null) return;

            if (GameManager.Instance.Maintenance.TryRepairAsset(currentAsset, currentDef, GameManager.Instance.Economy, GameManager.Instance.Simulation.CurrentDay))
            {
                RefreshView();
            }
        }

        private void OnSellClicked()
        {
            if (GameManager.Instance == null || currentAsset == null || currentDef == null) return;

            if (GameManager.Instance.Economy.TrySellAsset(currentAsset, currentDef, GameManager.Instance.Simulation.CurrentDay))
            {
                if (currentSlot != null)
                {
                    currentSlot.ClearAsset();
                }
                currentAsset = null;
                Close();
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}

