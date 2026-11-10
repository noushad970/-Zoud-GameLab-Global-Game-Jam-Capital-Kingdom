using EmpireBuilder.Core;
using EmpireBuilder.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class GamePlayHeaderUI : MonoBehaviour
    {
        [Header("Coin & Money Header")]
        [SerializeField] private Image coinIcon;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Button backToMenuButton;

        [Header("Scene Config")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public static GamePlayHeaderUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            UpdateMoneyDisplay();

            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.OnCashChanged += val => UpdateMoneyDisplay();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.OnCashChanged -= val => UpdateMoneyDisplay();
            }
        }

        public void UpdateMoneyDisplay()
        {
            if (moneyText == null) return;

            double cash = 10000.0;
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                cash = GameManager.Instance.Economy.Cash;
            }
            else if (SaveService.HasSaveFile())
            {
                var save = new SaveService().LoadGame();
                if (save != null) cash = save.cash;
            }

            moneyText.text = $"${cash:N0}";
        }

        private void OnBackToMenuClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}

