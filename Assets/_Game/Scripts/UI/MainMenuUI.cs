using EmpireBuilder.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Money Header")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private Image coinIcon;

        [Header("Menu Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button exitButton;

        [Header("Option / Settings Panel")]
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button closeOptionButton;

        [Header("About Panel")]
        [SerializeField] private GameObject aboutPanel;
        [SerializeField] private Button closeAboutButton;

        [Header("Navigation Target Scene")]
        [SerializeField] private string gamePlaySceneName = "GamePlay";

        private void Start()
        {
            // Bind buttons
            if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (optionButton != null) optionButton.onClick.AddListener(OnOptionClicked);
            if (closeOptionButton != null) closeOptionButton.onClick.AddListener(OnCloseOptionClicked);

            if (aboutButton != null) aboutButton.onClick.AddListener(OnAboutClicked);
            if (closeAboutButton != null) closeAboutButton.onClick.AddListener(OnCloseAboutClicked);

            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            if (optionPanel != null) optionPanel.SetActive(false);
            if (aboutPanel != null) aboutPanel.SetActive(false);

            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(val => AudioListener.volume = val);
            }

            RefreshMenuState();
        }

        private void OnEnable()
        {
            RefreshMenuState();
        }

        public void RefreshMenuState()
        {
            bool hasSave = SaveService.HasSaveFile();
            if (resumeButton != null)
            {
                resumeButton.interactable = hasSave;
            }

            UpdateMoneyHeader();
        }

        private void UpdateMoneyHeader()
        {
            if (moneyText == null) return;

            double displayCash = 10000.0; // Default starting capital
            if (SaveService.HasSaveFile())
            {
                var save = new SaveService().LoadGame();
                if (save != null)
                {
                    displayCash = save.cash;
                }
            }

            moneyText.text = $"${displayCash:N0}";
        }

        private void OnStartGameClicked()
        {
            // Fresh new game
            PlayerPrefs.SetInt("LoadOnStart", 0);
            WebGLSaveManager.ClearSaveData();
            PlayerPrefs.Save();
            SceneManager.LoadScene(gamePlaySceneName);
        }

        private void OnResumeClicked()
        {
            // Resume existing game
            PlayerPrefs.SetInt("LoadOnStart", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(gamePlaySceneName);
        }

        private void OnOptionClicked()
        {
            if (optionPanel != null) optionPanel.SetActive(true);
        }

        private void OnCloseOptionClicked()
        {
            if (optionPanel != null) optionPanel.SetActive(false);
        }

        private void OnAboutClicked()
        {
            if (aboutPanel != null) aboutPanel.SetActive(true);
        }

        private void OnCloseAboutClicked()
        {
            if (aboutPanel != null) aboutPanel.SetActive(false);
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
