using EmpireBuilder.Localization;
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
        [SerializeField] private TextMeshProUGUI warningTitleText;
        [SerializeField] private TextMeshProUGUI warningBodyText;
        [SerializeField] private TextMeshProUGUI warningConfirmText;
        [SerializeField] private TextMeshProUGUI warningCancelText;

        [Header("Navigation Target Scene")]
        [SerializeField] private string gamePlaySceneName = "GamePlay";

        [Header("Language Controls")]
        [SerializeField] private Button quickLanguageButton;
        [SerializeField] private TextMeshProUGUI quickLanguageText;
        [SerializeField] private GameObject languageSection;
        [SerializeField] private Button optEnglishButton;
        [SerializeField] private Button optArabicButton;
        [SerializeField] private TextMeshProUGUI optEnglishText;
        [SerializeField] private TextMeshProUGUI optArabicText;

        private void Awake()
        {
            LocalizationManager.InitializeIfNeeded();
        }

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

            EnsureLanguageSettingsUI();
            EnsureQuickLanguageButton();

            RefreshMenuState();
            UpdateLocalizedTexts();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
            RefreshMenuState();
            UpdateLocalizedTexts();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;
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

            double displayCash = 5000.0; // Default starting capital
            if (SaveService.HasSaveFile())
            {
                var save = new SaveService().LoadGame();
                if (save != null)
                {
                    displayCash = save.cash;
                }
            }
            else if (WebGLSaveManager.HasSaveData())
            {
                var save = WebGLSaveManager.PeekSaveData();
                if (save != null)
                {
                    displayCash = save.cash;
                }
            }
            else
            {
                PlayerPrefs.SetFloat("SavedPlayerCash", 5000f);
            }

            moneyText.text = $"${displayCash:N0}";
        }

        private void UpdateLocalizedTexts()
        {
            if (startGameButton != null)
            {
                var t = startGameButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = LocalizationManager.Get("menu_start_game");
            }

            if (resumeButton != null)
            {
                var t = resumeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = LocalizationManager.Get("menu_resume_game");
            }

            if (optionButton != null)
            {
                var t = optionButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = LocalizationManager.Get("menu_options");
            }

            if (aboutButton != null)
            {
                var t = aboutButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = LocalizationManager.Get("menu_about");
            }

            if (exitButton != null)
            {
                var t = exitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = LocalizationManager.Get("menu_exit");
            }

            // Warning dialog texts
            if (warningTitleText != null) warningTitleText.text = LocalizationManager.Get("modal_reset_title");
            if (warningBodyText != null) warningBodyText.text = LocalizationManager.Get("modal_reset_message");
            if (warningConfirmText != null) warningConfirmText.text = LocalizationManager.Get("modal_reset_confirm");
            if (warningCancelText != null) warningCancelText.text = LocalizationManager.Get("modal_reset_cancel");

            // Update language toggle button
            if (quickLanguageText != null)
            {
                quickLanguageText.text = LocalizationManager.CurrentLanguage == GameLanguage.Arabic ? "English" : "العربية";
            }

            UpdateLanguageButtonsHighlight();
        }

        private void EnsureQuickLanguageButton()
        {
            if (quickLanguageButton == null)
            {
                Transform existing = transform.Find("QuickLanguageButton");
                if (existing == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        existing = canvas.transform.Find("QuickLanguageButton");
                    }
                }

                if (existing != null)
                {
                    quickLanguageButton = existing.GetComponent<Button>();
                    quickLanguageText = existing.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (quickLanguageButton != null)
            {
                if (quickLanguageText == null)
                {
                    quickLanguageText = quickLanguageButton.GetComponentInChildren<TextMeshProUGUI>();
                }

                quickLanguageButton.onClick.RemoveAllListeners();
                quickLanguageButton.onClick.AddListener(() =>
                {
                    LocalizationManager.ToggleLanguage();
                });
                return;
            }

            Canvas c = GetComponentInParent<Canvas>();
            if (c == null) return;

            GameObject btnObj = new GameObject("QuickLanguageButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(c.transform, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(40, -35);
            rt.sizeDelta = new Vector2(160, 50);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            quickLanguageButton = btnObj.GetComponent<Button>();
            quickLanguageButton.onClick.RemoveAllListeners();
            quickLanguageButton.onClick.AddListener(() =>
            {
                LocalizationManager.ToggleLanguage();
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            quickLanguageText = txtObj.GetComponent<TextMeshProUGUI>();
            quickLanguageText.text = LocalizationManager.CurrentLanguage == GameLanguage.Arabic ? "English" : "العربية";
            quickLanguageText.fontSize = 15;
            quickLanguageText.fontStyle = FontStyles.Bold;
            quickLanguageText.alignment = TextAlignmentOptions.Center;
            quickLanguageText.color = new Color(1f, 0.85f, 0.3f);
        }

        private void EnsureLanguageSettingsUI()
        {
            if (optEnglishButton != null && optArabicButton != null)
            {
                if (optEnglishText == null) optEnglishText = optEnglishButton.GetComponentInChildren<TextMeshProUGUI>();
                if (optArabicText == null) optArabicText = optArabicButton.GetComponentInChildren<TextMeshProUGUI>();
                BindLanguageButtons();
                return;
            }

            if (optionPanel == null) return;

            Transform existing = optionPanel.transform.Find("LanguageSection");
            if (existing != null)
            {
                languageSection = existing.gameObject;
                optEnglishButton = existing.Find("ButtonsRow/EnglishButton")?.GetComponent<Button>() ?? existing.Find("EnglishButton")?.GetComponent<Button>();
                optArabicButton = existing.Find("ButtonsRow/ArabicButton")?.GetComponent<Button>() ?? existing.Find("ArabicButton")?.GetComponent<Button>();
                optEnglishText = optEnglishButton?.GetComponentInChildren<TextMeshProUGUI>();
                optArabicText = optArabicButton?.GetComponentInChildren<TextMeshProUGUI>();
                BindLanguageButtons();
                return;
            }

            // Create Language Settings Section inside optionPanel as a fallback
            GameObject section = new GameObject("LanguageSection", typeof(RectTransform));
            section.transform.SetParent(optionPanel.transform, false);
            languageSection = section;

            RectTransform secRt = section.GetComponent<RectTransform>();
            secRt.anchorMin = new Vector2(0.5f, 0.5f);
            secRt.anchorMax = new Vector2(0.5f, 0.5f);
            secRt.pivot = new Vector2(0.5f, 0.5f);
            secRt.anchoredPosition = new Vector2(0, -30);
            secRt.sizeDelta = new Vector2(440, 120);

            // Label
            GameObject labelObj = new GameObject("LanguageLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(section.transform, false);
            var lblRt = labelObj.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 1);
            lblRt.anchorMax = new Vector2(1, 1);
            lblRt.pivot = new Vector2(0.5f, 1);
            lblRt.anchoredPosition = new Vector2(0, 0);
            lblRt.sizeDelta = new Vector2(440, 30);

            var lblTmp = labelObj.GetComponent<TextMeshProUGUI>();
            lblTmp.text = "LANGUAGE / اللغة";
            lblTmp.fontSize = 15;
            lblTmp.fontStyle = FontStyles.Bold;
            lblTmp.alignment = TextAlignmentOptions.Center;
            lblTmp.color = new Color(0.95f, 0.85f, 0.35f);

            // Row for buttons
            GameObject row = new GameObject("ButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(section.transform, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 0);
            rowRt.anchorMax = new Vector2(1, 0);
            rowRt.pivot = new Vector2(0.5f, 0);
            rowRt.anchoredPosition = new Vector2(0, 10);
            rowRt.sizeDelta = new Vector2(440, 50);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // English Button
            GameObject enBtnObj = new GameObject("EnglishButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            enBtnObj.transform.SetParent(row.transform, false);
            optEnglishButton = enBtnObj.GetComponent<Button>();

            GameObject enTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            enTxtObj.transform.SetParent(enBtnObj.transform, false);
            var enTxtRt = enTxtObj.GetComponent<RectTransform>();
            enTxtRt.anchorMin = Vector2.zero;
            enTxtRt.anchorMax = Vector2.one;
            enTxtRt.sizeDelta = Vector2.zero;
            optEnglishText = enTxtObj.GetComponent<TextMeshProUGUI>();
            optEnglishText.text = "English";
            optEnglishText.fontSize = 14;
            optEnglishText.fontStyle = FontStyles.Bold;
            optEnglishText.alignment = TextAlignmentOptions.Center;
            optEnglishText.color = Color.white;

            // Arabic Button
            GameObject arBtnObj = new GameObject("ArabicButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            arBtnObj.transform.SetParent(row.transform, false);
            optArabicButton = arBtnObj.GetComponent<Button>();

            GameObject arTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            arTxtObj.transform.SetParent(arBtnObj.transform, false);
            var arTxtRt = arTxtObj.GetComponent<RectTransform>();
            arTxtRt.anchorMin = Vector2.zero;
            arTxtRt.anchorMax = Vector2.one;
            arTxtRt.sizeDelta = Vector2.zero;
            optArabicText = arTxtObj.GetComponent<TextMeshProUGUI>();
            optArabicText.text = ArabicFixer.Fix("العربية");
            optArabicText.fontSize = 15;
            optArabicText.fontStyle = FontStyles.Bold;
            optArabicText.alignment = TextAlignmentOptions.Center;
            optArabicText.color = Color.white;

            BindLanguageButtons();
        }

        private void BindLanguageButtons()
        {
            if (optEnglishButton != null)
            {
                optEnglishButton.onClick.RemoveAllListeners();
                optEnglishButton.onClick.AddListener(() =>
                {
                    LocalizationManager.SetLanguage(GameLanguage.English);
                });
            }

            if (optArabicButton != null)
            {
                optArabicButton.onClick.RemoveAllListeners();
                optArabicButton.onClick.AddListener(() =>
                {
                    LocalizationManager.SetLanguage(GameLanguage.Arabic);
                });
            }

            UpdateLanguageButtonsHighlight();
        }

        private void UpdateLanguageButtonsHighlight()
        {
            bool isAr = LocalizationManager.CurrentLanguage == GameLanguage.Arabic;

            if (optEnglishButton != null)
            {
                var img = optEnglishButton.GetComponent<Image>();
                if (img != null) img.color = !isAr ? new Color(0.12f, 0.65f, 0.35f, 1f) : new Color(0.20f, 0.24f, 0.32f, 0.9f);
            }

            if (optArabicButton != null)
            {
                var img = optArabicButton.GetComponent<Image>();
                if (img != null) img.color = isAr ? new Color(0.12f, 0.65f, 0.35f, 1f) : new Color(0.20f, 0.24f, 0.32f, 0.9f);
            }
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
                UpdateLocalizedTexts();
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

            // Thoroughly wipe existing save data and start fresh with $5,000
            SaveService.DeleteSave();
            WebGLSaveManager.ClearSaveData();
            PlayerPrefs.SetFloat("SavedPlayerCash", 5000f);
            PlayerPrefs.SetInt("CurrentDay", 1);
            PlayerPrefs.SetInt("LoadOnStart", 0);
            PlayerPrefs.Save();

            Debug.Log("[MainMenuUI] Starting brand new game with 5,000 coins.");
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

            warningTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            warningTitleText.text = LocalizationManager.Get("modal_reset_title");
            warningTitleText.fontSize = 24;
            warningTitleText.fontStyle = FontStyles.Bold;
            warningTitleText.alignment = TextAlignmentOptions.Center;
            warningTitleText.color = new Color(0.98f, 0.75f, 0.15f, 1f); // Vibrant Warning Gold

            // Body Message
            GameObject bodyObj = new GameObject("MessageText");
            bodyObj.transform.SetParent(cardObj.transform, false);
            RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0.5f);
            bodyRt.anchorMax = new Vector2(1, 0.5f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(0, 15);
            bodyRt.sizeDelta = new Vector2(-60, 140);

            warningBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            warningBodyText.text = LocalizationManager.Get("modal_reset_message");
            warningBodyText.fontSize = 15;
            warningBodyText.lineSpacing = 16f;
            warningBodyText.alignment = TextAlignmentOptions.Center;
            warningBodyText.color = new Color(0.92f, 0.94f, 0.96f, 1f);

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
            warningCancelText = cancelTxtObj.AddComponent<TextMeshProUGUI>();
            warningCancelText.text = LocalizationManager.Get("modal_reset_cancel");
            warningCancelText.fontSize = 14;
            warningCancelText.fontStyle = FontStyles.Bold;
            warningCancelText.alignment = TextAlignmentOptions.Center;
            warningCancelText.color = Color.white;

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
            warningConfirmText = confirmTxtObj.AddComponent<TextMeshProUGUI>();
            warningConfirmText.text = LocalizationManager.Get("modal_reset_confirm");
            warningConfirmText.fontSize = 14;
            warningConfirmText.fontStyle = FontStyles.Bold;
            warningConfirmText.alignment = TextAlignmentOptions.Center;
            warningConfirmText.color = Color.white;
        }

        private void OnOptionClicked()
        {
            if (optionPanel != null)
            {
                optionPanel.SetActive(true);
                EnsureLanguageSettingsUI();
                UpdateLanguageButtonsHighlight();
            }
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
