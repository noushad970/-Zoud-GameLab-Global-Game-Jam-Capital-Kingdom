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

        [Header("Reset Warning Modal")]
        [SerializeField] private GameObject warningDialogPanel;
        [SerializeField] private Button confirmNewGameButton;
        [SerializeField] private Button cancelNewGameButton;

        [Header("Navigation Target Scene")]
        [SerializeField] private string gamePlaySceneName = "GamePlay";

        private void Start()
        {
            // Bind buttons
            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (optionButton != null)
            {
                optionButton.onClick.RemoveAllListeners();
                optionButton.onClick.AddListener(OnOptionClicked);
            }

            if (closeOptionButton != null)
            {
                closeOptionButton.onClick.RemoveAllListeners();
                closeOptionButton.onClick.AddListener(OnCloseOptionClicked);
            }

            if (aboutButton != null)
            {
                aboutButton.onClick.RemoveAllListeners();
                aboutButton.onClick.AddListener(OnAboutClicked);
            }

            if (closeAboutButton != null)
            {
                closeAboutButton.onClick.RemoveAllListeners();
                closeAboutButton.onClick.AddListener(OnCloseAboutClicked);
            }

            if (confirmNewGameButton != null)
            {
                confirmNewGameButton.onClick.RemoveAllListeners();
                confirmNewGameButton.onClick.AddListener(ConfirmStartNewGame);
            }

            if (cancelNewGameButton != null)
            {
                cancelNewGameButton.onClick.RemoveAllListeners();
                cancelNewGameButton.onClick.AddListener(HideWarningDialog);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
            }

            if (optionPanel != null) optionPanel.SetActive(false);
            if (aboutPanel != null) aboutPanel.SetActive(false);
            if (warningDialogPanel != null) warningDialogPanel.SetActive(false);

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
            bool hasSave = SaveService.HasSaveFile() || WebGLSaveManager.HasSaveData();
            if (resumeButton != null)
            {
                resumeButton.interactable = hasSave;
                var canvasGroup = resumeButton.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = hasSave ? 1f : 0.5f;
                }
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
            else
            {
                PlayerPrefs.SetFloat("SavedPlayerCash", 20000f);
            }

            moneyText.text = $"${displayCash:N0}";
        }

        private void OnStartGameClicked()
        {
            bool hasExistingSave = SaveService.HasSaveFile() || WebGLSaveManager.HasSaveData();
            if (hasExistingSave)
            {
                ShowWarningDialog();
            }
            else
            {
                ConfirmStartNewGame();
            }
        }

        private void ShowWarningDialog()
        {
            if (warningDialogPanel == null)
            {
                CreateDynamicWarningDialog();
            }

            if (warningDialogPanel != null)
            {
                warningDialogPanel.SetActive(true);
            }
        }

        private void HideWarningDialog()
        {
            if (warningDialogPanel != null)
            {
                warningDialogPanel.SetActive(false);
            }
        }

        private void ConfirmStartNewGame()
        {
            HideWarningDialog();

            // Thoroughly wipe existing save data and start fresh with $10,000
            SaveService.DeleteSave();
            WebGLSaveManager.ClearSaveData();
            PlayerPrefs.SetFloat("SavedPlayerCash", 10000f);
            PlayerPrefs.SetInt("CurrentDay", 1);
            PlayerPrefs.SetInt("LoadOnStart", 0);
            PlayerPrefs.Save();

            Debug.Log("[MainMenuUI] Starting brand new game with 10,000 coins.");
            SceneManager.LoadScene(gamePlaySceneName);
        }

        private void OnResumeClicked()
        {
            // Resume existing game from last saved stage
            PlayerPrefs.SetInt("LoadOnStart", 1);
            PlayerPrefs.Save();

            Debug.Log("[MainMenuUI] Resuming game from last saved stage.");
            SceneManager.LoadScene(gamePlaySceneName);
        }

        private void CreateDynamicWarningDialog()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Fullscreen backdrop
            warningDialogPanel = new GameObject("StartNewGame_WarningModal");
            warningDialogPanel.transform.SetParent(canvas.transform, false);
            RectTransform rootRt = warningDialogPanel.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            Image backdropImg = warningDialogPanel.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.78f);
            backdropImg.raycastTarget = true;

            // Centered Card
            GameObject cardObj = new GameObject("DialogCard");
            cardObj.transform.SetParent(warningDialogPanel.transform, false);
            RectTransform cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(560, 340);
            cardRt.anchoredPosition = Vector2.zero;

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(0.11f, 0.13f, 0.18f, 0.98f); // Deep navy cartoon card

            // Card Header / Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0.5f, 1);
            titleRt.anchoredPosition = new Vector2(0, -20);
            titleRt.sizeDelta = new Vector2(-40, 50);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "⚠️ START NEW GAME?";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.98f, 0.75f, 0.15f, 1f); // Vibrant Warning Gold

            // Body Message
            GameObject bodyObj = new GameObject("MessageText");
            bodyObj.transform.SetParent(cardObj.transform, false);
            RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0.5f);
            bodyRt.anchorMax = new Vector2(1, 0.5f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(0, 15);
            bodyRt.sizeDelta = new Vector2(-60, 140);

            TextMeshProUGUI bodyTmp = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = "<b>Previous saved city progress found!</b>\n\nStarting a new game will <color=#FF4444>permanently erase</color> your existing city data, buildings, and properties, resetting your balance to <b>$10,000 coins</b>.\n\nAre you sure you want to proceed?";
            bodyTmp.fontSize = 17;
            bodyTmp.lineSpacing = 18f;
            bodyTmp.alignment = TextAlignmentOptions.Center;
            bodyTmp.color = new Color(0.92f, 0.94f, 0.96f, 1f);

            // Button Container
            GameObject btnRow = new GameObject("ButtonsRow");
            btnRow.transform.SetParent(cardObj.transform, false);
            RectTransform rowRt = btnRow.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 0);
            rowRt.anchorMax = new Vector2(1, 0);
            rowRt.pivot = new Vector2(0.5f, 0);
            rowRt.anchoredPosition = new Vector2(0, 24);
            rowRt.sizeDelta = new Vector2(-50, 52);

            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Cancel Button
            GameObject cancelObj = new GameObject("CancelButton");
            cancelObj.transform.SetParent(btnRow.transform, false);
            Image cancelImg = cancelObj.AddComponent<Image>();
            cancelImg.color = new Color(0.24f, 0.28f, 0.36f, 1f); // Slate Blue
            cancelNewGameButton = cancelObj.AddComponent<Button>();
            cancelNewGameButton.onClick.AddListener(HideWarningDialog);

            GameObject cancelTxtObj = new GameObject("Text");
            cancelTxtObj.transform.SetParent(cancelObj.transform, false);
            RectTransform cTxtRt = cancelTxtObj.AddComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.offsetMin = Vector2.zero;
            cTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI cancelTmp = cancelTxtObj.AddComponent<TextMeshProUGUI>();
            cancelTmp.text = "KEEP SAVED GAME";
            cancelTmp.fontSize = 16;
            cancelTmp.fontStyle = FontStyles.Bold;
            cancelTmp.alignment = TextAlignmentOptions.Center;
            cancelTmp.color = Color.white;

            // Confirm Button
            GameObject confirmObj = new GameObject("ConfirmButton");
            confirmObj.transform.SetParent(btnRow.transform, false);
            Image confirmImg = confirmObj.AddComponent<Image>();
            confirmImg.color = new Color(0.85f, 0.22f, 0.22f, 1f); // Crimson Warning Red
            confirmNewGameButton = confirmObj.AddComponent<Button>();
            confirmNewGameButton.onClick.AddListener(ConfirmStartNewGame);

            GameObject confirmTxtObj = new GameObject("Text");
            confirmTxtObj.transform.SetParent(confirmObj.transform, false);
            RectTransform cfTxtRt = confirmTxtObj.AddComponent<RectTransform>();
            cfTxtRt.anchorMin = Vector2.zero;
            cfTxtRt.anchorMax = Vector2.one;
            cfTxtRt.offsetMin = Vector2.zero;
            cfTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI confirmTmp = confirmTxtObj.AddComponent<TextMeshProUGUI>();
            confirmTmp.text = "YES, START OVER ($10,000)";
            confirmTmp.fontSize = 16;
            confirmTmp.fontStyle = FontStyles.Bold;
            confirmTmp.alignment = TextAlignmentOptions.Center;
            confirmTmp.color = Color.white;
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
