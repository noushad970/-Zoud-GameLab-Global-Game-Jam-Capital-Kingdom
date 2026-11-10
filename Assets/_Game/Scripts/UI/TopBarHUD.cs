using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class TopBarHUD : MonoBehaviour
    {
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI netWorthText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI dailyCashFlowText;

        [Header("Buttons")]
        [SerializeField] private Button advanceDayButton;
        [SerializeField] private Button openShopButton;

        [Header("Modals References")]
        [SerializeField] private AssetShopModalUI shopModal;

        private void Start()
        {
            if (advanceDayButton != null)
            {
                advanceDayButton.onClick.AddListener(OnAdvanceDayClicked);
            }

            if (openShopButton != null)
            {
                openShopButton.onClick.AddListener(OnOpenShopClicked);
            }

            SubscribeToEvents();
            RefreshHUD();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.Economy.OnCashChanged += UpdateCashDisplay;
            GameManager.Instance.Economy.OnNetWorthChanged += UpdateNetWorthDisplay;
            GameManager.Instance.Simulation.OnDayCompleted += HandleDayCompleted;
        }

        private void UnsubscribeEvents()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.Economy.OnCashChanged -= UpdateCashDisplay;
            GameManager.Instance.Economy.OnNetWorthChanged -= UpdateNetWorthDisplay;
            GameManager.Instance.Simulation.OnDayCompleted -= HandleDayCompleted;
        }

        public void RefreshHUD()
        {
            if (GameManager.Instance == null) return;

            UpdateCashDisplay(GameManager.Instance.Economy.Cash);
            UpdateNetWorthDisplay(GameManager.Instance.Economy.NetWorth);
            UpdateDayDisplay(GameManager.Instance.Simulation.CurrentDay);
            UpdateDailyFlowDisplay();
        }

        private void UpdateCashDisplay(double cash)
        {
            if (cashText != null)
            {
                cashText.text = $"${cash:N0}";
            }
        }

        private void UpdateNetWorthDisplay(double netWorth)
        {
            if (netWorthText != null)
            {
                netWorthText.text = $"Net Worth: ${netWorth:N0}";
            }
        }

        private void UpdateDayDisplay(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Day {day}";
            }
        }

        private void UpdateDailyFlowDisplay()
        {
            if (dailyCashFlowText == null || GameManager.Instance == null) return;

            double projectedNet = 0;
            foreach (var asset in GameManager.Instance.Assets.OwnedAssets)
            {
                if (asset.CanOperate())
                {
                    var def = GameManager.Instance.Assets.GetDefinition(asset.definitionId);
                    if (def != null)
                    {
                        projectedNet += def.ExpectedNetDailyIncome;
                    }
                }
            }

            string sign = projectedNet >= 0 ? "+" : "-";
            dailyCashFlowText.text = $"{sign}${System.Math.Abs(projectedNet):N0}/day";
            dailyCashFlowText.color = projectedNet >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
        }

        private void HandleDayCompleted(DailyReport report)
        {
            UpdateDayDisplay(GameManager.Instance.Simulation.CurrentDay);
            UpdateDailyFlowDisplay();
        }

        private void OnAdvanceDayClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Simulation.AdvanceDay();
            }
        }

        private void OnOpenShopClicked()
        {
            if (shopModal != null)
            {
                shopModal.Open();
            }
        }
    }
}

