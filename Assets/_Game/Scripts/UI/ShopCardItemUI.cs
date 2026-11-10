using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class ShopCardItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI grossIncomeText;
        [SerializeField] private TextMeshProUGUI operatingCostText;
        [SerializeField] private TextMeshProUGUI netIncomeText;
        [SerializeField] private TextMeshProUGUI paybackText;
        [SerializeField] private Button buyButton;

        private AssetDefinition definition;
        private AssetShopModalUI parentModal;

        public void Setup(AssetDefinition def, AssetShopModalUI modal)
        {
            definition = def;
            parentModal = modal;

            if (nameText != null) nameText.text = def.displayName;
            if (iconImage != null && def.icon != null) iconImage.sprite = def.icon;
            if (priceText != null) priceText.text = $"${def.purchasePrice:N0}";
            if (grossIncomeText != null) grossIncomeText.text = $"Gross: ${def.grossIncomePerDay:N0}/day";
            if (operatingCostText != null) operatingCostText.text = $"Upkeep: ${def.operatingCostPerDay:N0}/day";

            if (netIncomeText != null)
            {
                double net = def.ExpectedNetDailyIncome;
                string sign = net >= 0 ? "+" : "-";
                netIncomeText.text = $"Net: {sign}${System.Math.Abs(net):N0}/day";
                netIncomeText.color = net >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
            }

            if (paybackText != null)
            {
                paybackText.text = $"Payback: {def.GetPaybackString()}";
            }

            if (buyButton != null)
            {
                bool canAfford = GameManager.Instance != null && GameManager.Instance.Economy.CanAfford(def.purchasePrice);
                buyButton.interactable = canAfford;
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        private void OnBuyClicked()
        {
            if (GameManager.Instance == null || definition == null) return;

            if (GameManager.Instance.Economy.TryPurchaseAsset(definition, GameManager.Instance.Simulation.CurrentDay, out var newAsset))
            {
                GameManager.Instance.Assets.AddAsset(newAsset);
                if (parentModal != null)
                {
                    parentModal.PopulateCards();
                }
            }
        }
    }
}

