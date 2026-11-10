using EmpireBuilder.Core;
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

            if (headerTitleText != null) headerTitleText.text = $"Day {report.dayNumber} Financial Summary";
            if (grossRevenueText != null) grossRevenueText.text = $"+${report.grossRevenue:N0}";
            if (operatingCostsText != null) operatingCostsText.text = $"-${report.operatingExpenses:N0}";
            if (repairCostsText != null) repairCostsText.text = $"-${report.maintenanceCosts:N0}";

            if (netCashChangeText != null)
            {
                string sign = report.netCashChange >= 0 ? "+" : "-";
                netCashChangeText.text = $"{sign}${System.Math.Abs(report.netCashChange):N0}";
                netCashChangeText.color = report.netCashChange >= 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.25f);
            }

            if (endingCashText != null) endingCashText.text = $"Cash on Hand: ${report.endingCash:N0}";
            if (netWorthText != null) netWorthText.text = $"Total Net Worth: ${report.endingNetWorth:N0}";
            if (financialLessonText != null) financialLessonText.text = $"💡 Lesson: {report.financialLesson}";

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

