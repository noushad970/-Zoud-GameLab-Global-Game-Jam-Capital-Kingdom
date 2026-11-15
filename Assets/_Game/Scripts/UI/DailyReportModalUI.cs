using EmpireBuilder.Core;
using EmpireBuilder.Localization;
using EmpireBuilder.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class DailyReportModalUI : MonoBehaviour
    {
        [Header("Modal Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI grossRevenueText;
        [SerializeField] private TextMeshProUGUI operatingCostsText;
        [SerializeField] private TextMeshProUGUI repairCostsText;
        [SerializeField] private TextMeshProUGUI netCashChangeText;
        [SerializeField] private TextMeshProUGUI endingCashText;
        [SerializeField] private TextMeshProUGUI netWorthText;
        [SerializeField] private TextMeshProUGUI financialLessonText;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip reportChime;

        private void Start()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(Close);
            }

            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted += ShowReport;
            }

            // Hidden by default
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted -= ShowReport;
            }
        }

        public void ShowReport(DailyReport report)
        {
            if (report == null) return;

            if (headerTitleText != null)
            {
                LocalizationManager.ApplyLocalized(headerTitleText, "report_title", report.dayNumber);
            }
            if (grossRevenueText != null)
            {
                LocalizationManager.ApplyLocalized(grossRevenueText, "report_gross", report.grossRevenue);
            }
            if (operatingCostsText != null)
            {
                LocalizationManager.ApplyLocalized(operatingCostsText, "report_operating", report.operatingExpenses);
            }
            if (repairCostsText != null)
            {
                LocalizationManager.ApplyLocalized(repairCostsText, "report_repair", report.maintenanceCosts);
            }

            if (netCashChangeText != null)
            {
                string sign = report.netCashChange >= 0 ? "+" : "-";
                LocalizationManager.ApplyLocalized(netCashChangeText, "report_net", sign, System.Math.Abs(report.netCashChange));
                netCashChangeText.color = report.netCashChange >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
            }

            if (endingCashText != null)
            {
                LocalizationManager.ApplyLocalized(endingCashText, "report_cash", report.endingCash);
            }
            if (netWorthText != null)
            {
                LocalizationManager.ApplyLocalized(netWorthText, "report_networth", report.endingNetWorth);
            }
            if (financialLessonText != null)
            {
                LocalizationManager.ApplyLocalized(financialLessonText, "report_lesson", report.financialLesson);
            }
            if (continueButton != null)
            {
                var btnText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    LocalizationManager.ApplyLocalized(btnText, "report_continue_btn");
                }
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            if (audioSource != null && reportChime != null)
            {
                audioSource.PlayOneShot(reportChime);
            }
        }

        public void Close()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }
    }
}

