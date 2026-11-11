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
        [SerializeField] private Button guideButton;

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

            EnsureGuideButton();
            EnsureWebGLSaveManager();
            EnsureCityNoticeBar();
            EnsureGameTutorialModal();

            UpdateMoneyDisplay();

            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.OnCashChanged += val => UpdateMoneyDisplay();
            }
        }

        private void EnsureGuideButton()
        {
            if (guideButton == null)
            {
                var tr = transform.Find("GuideButton");
                if (tr != null) guideButton = tr.GetComponent<Button>();
            }

            if (guideButton == null && backToMenuButton != null)
            {
                // Instantiate a clone of backToMenuButton styled with blue tint or copied layout
                var guideObj = Instantiate(backToMenuButton.gameObject, transform, false);
                guideObj.name = "GuideButton";
                var rt = guideObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-260, 0);
                rt.sizeDelta = new Vector2(130, 52);

                var txt = guideObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "GUIDE 💡";

                var img = guideObj.GetComponent<Image>();
                if (img != null) img.color = new Color(0.2f, 0.55f, 0.95f);

                guideButton = guideObj.GetComponent<Button>();
            }

            if (guideButton != null)
            {
                guideButton.onClick.RemoveAllListeners();
                guideButton.onClick.AddListener(() =>
                {
                    if (GameTutorialManager.Instance != null)
                    {
                        GameTutorialManager.Instance.ShowTutorial(0);
                    }
                });
            }
        }

        private void EnsureWebGLSaveManager()
        {
            if (WebGLSaveManager.Instance == null && FindAnyObjectByType<WebGLSaveManager>() == null)
            {
                var obj = new GameObject("WebGLSaveManager", typeof(WebGLSaveManager));
                Debug.Log("[GamePlayHeaderUI] Initialized WebGLSaveManager in scene.");
            }
        }

        private void EnsureCityNoticeBar()
        {
            if (CityNoticeBarUI.Instance != null) return;

            Transform canvasTr = transform.parent; // GamePlayCanvas
            if (canvasTr == null) return;

            var existing = canvasTr.Find("CityNoticeBar");
            if (existing != null)
            {
                var ui = existing.GetComponent<CityNoticeBarUI>();
                if (ui != null) return;
            }

            var noticeBarObj = new GameObject("CityNoticeBar", typeof(RectTransform));
            noticeBarObj.transform.SetParent(canvasTr, false);
            var nbRt = noticeBarObj.GetComponent<RectTransform>();
            nbRt.anchorMin = new Vector2(1, 1);
            nbRt.anchorMax = new Vector2(1, 1);
            nbRt.pivot = new Vector2(1, 1);
            nbRt.anchoredPosition = new Vector2(-20, -100);
            nbRt.sizeDelta = new Vector2(360, 380);

            // Header banner
            var nbHeader = new GameObject("NoticeHeader", typeof(RectTransform), typeof(Image));
            nbHeader.transform.SetParent(noticeBarObj.transform, false);
            var hRt = nbHeader.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(1, 1);
            hRt.pivot = new Vector2(0.5f, 1);
            hRt.anchoredPosition = Vector2.zero;
            hRt.sizeDelta = new Vector2(0, 32);
            var hImg = nbHeader.GetComponent<Image>();
            hImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);

            // Pulsating dot
            var dotObj = new GameObject("StatusDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(nbHeader.transform, false);
            var dRt = dotObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 0.5f);
            dRt.anchorMax = new Vector2(0, 0.5f);
            dRt.pivot = new Vector2(0, 0.5f);
            dRt.anchoredPosition = new Vector2(10, 0);
            dRt.sizeDelta = new Vector2(10, 10);
            var dotImg = dotObj.GetComponent<Image>();
            dotImg.color = Color.green;

            // Title
            var titleObj = new GameObject("NoticeTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(nbHeader.transform, false);
            var nbTitle = titleObj.GetComponent<TextMeshProUGUI>();
            nbTitle.text = "CITY OCCURRENCES";
            nbTitle.fontSize = 12;
            nbTitle.fontStyle = FontStyles.Bold;
            nbTitle.color = new Color(0.9f, 0.95f, 1f);
            nbTitle.alignment = TextAlignmentOptions.Left;
            var tRt = nbTitle.rectTransform;
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(0.7f, 1);
            tRt.pivot = new Vector2(0, 0.5f);
            tRt.anchoredPosition = new Vector2(26, 0);
            tRt.sizeDelta = Vector2.zero;

            // Count
            var countObj = new GameObject("NoticeCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObj.transform.SetParent(nbHeader.transform, false);
            var nbCount = countObj.GetComponent<TextMeshProUGUI>();
            nbCount.text = "0 ALERTS";
            nbCount.fontSize = 11;
            nbCount.fontStyle = FontStyles.Bold;
            nbCount.color = new Color(0.95f, 0.80f, 0.25f);
            nbCount.alignment = TextAlignmentOptions.Right;
            var cntRt = nbCount.rectTransform;
            cntRt.anchorMin = new Vector2(0.7f, 0);
            cntRt.anchorMax = new Vector2(1, 1);
            cntRt.pivot = new Vector2(1, 0.5f);
            cntRt.anchoredPosition = new Vector2(-10, 0);
            cntRt.sizeDelta = Vector2.zero;

            // Container for dynamic cards
            var nbContainerObj = new GameObject("NoticeContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            nbContainerObj.transform.SetParent(noticeBarObj.transform, false);
            var cRt = nbContainerObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 0);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.anchoredPosition = new Vector2(0, -36);
            cRt.sizeDelta = new Vector2(0, -36);

            var vlg = nbContainerObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            noticeBarObj.AddComponent<CityNoticeBarUI>();
        }

        private void EnsureGameTutorialModal()
        {
            if (GameTutorialManager.Instance != null) return;

            Transform canvasTr = transform.parent; // GamePlayCanvas
            if (canvasTr == null) return;

            var existing = canvasTr.Find("GameTutorialModal");
            if (existing != null)
            {
                var mgr = existing.GetComponent<GameTutorialManager>();
                if (mgr != null) return;
            }

            var tutModalRoot = new GameObject("GameTutorialModal", typeof(RectTransform));
            tutModalRoot.transform.SetParent(canvasTr, false);
            var tutRt = tutModalRoot.GetComponent<RectTransform>();
            tutRt.anchorMin = Vector2.zero;
            tutRt.anchorMax = Vector2.one;
            tutRt.sizeDelta = Vector2.zero;

            // Overlay
            var overlayObj = new GameObject("DimmedOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlayObj.transform.SetParent(tutModalRoot.transform, false);
            var ovRt = overlayObj.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero;
            ovRt.anchorMax = Vector2.one;
            ovRt.sizeDelta = Vector2.zero;
            overlayObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.70f);

            // Card
            var tutCard = new GameObject("TutorialCard", typeof(RectTransform), typeof(Image));
            tutCard.transform.SetParent(tutModalRoot.transform, false);
            var cRt = tutCard.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(620, 480);
            tutCard.GetComponent<Image>().color = new Color(0.11f, 0.15f, 0.22f, 0.98f);

            // Close X
            if (backToMenuButton != null)
            {
                var closeXBtn = Instantiate(backToMenuButton.gameObject, tutCard.transform, false);
                closeXBtn.name = "CloseXButton";
                var xRt = closeXBtn.GetComponent<RectTransform>();
                xRt.anchorMin = new Vector2(1, 1);
                xRt.anchorMax = new Vector2(1, 1);
                xRt.pivot = new Vector2(1, 1);
                xRt.anchoredPosition = new Vector2(-14, -14);
                xRt.sizeDelta = new Vector2(36, 36);
                var xTmp = closeXBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (xTmp != null) xTmp.text = "✕";
            }

            // Icon Badge
            var badgeObj = new GameObject("IconBadge", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(tutCard.transform, false);
            var bRt = badgeObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 1);
            bRt.anchorMax = new Vector2(0, 1);
            bRt.pivot = new Vector2(0, 1);
            bRt.anchoredPosition = new Vector2(24, -20);
            bRt.sizeDelta = new Vector2(54, 54);
            var bImg = badgeObj.GetComponent<Image>();
            bImg.color = new Color(0.18f, 0.52f, 0.95f);

            var emojiObj = new GameObject("Emoji", typeof(RectTransform), typeof(TextMeshProUGUI));
            emojiObj.transform.SetParent(badgeObj.transform, false);
            var emojiTmp = emojiObj.GetComponent<TextMeshProUGUI>();
            emojiTmp.text = "👑";
            emojiTmp.fontSize = 28;
            emojiTmp.alignment = TextAlignmentOptions.Center;
            var eRt = emojiTmp.rectTransform;
            eRt.anchorMin = Vector2.zero;
            eRt.anchorMax = Vector2.one;
            eRt.sizeDelta = Vector2.zero;

            // Step counter
            var stepCountObj = new GameObject("StepCount", typeof(RectTransform), typeof(TextMeshProUGUI));
            stepCountObj.transform.SetParent(tutCard.transform, false);
            var stepCountTmp = stepCountObj.GetComponent<TextMeshProUGUI>();
            stepCountTmp.text = "STEP 1 OF 7";
            stepCountTmp.fontSize = 12;
            stepCountTmp.fontStyle = FontStyles.Bold;
            stepCountTmp.color = new Color(0.95f, 0.80f, 0.25f);
            var scRt = stepCountTmp.rectTransform;
            scRt.anchorMin = new Vector2(0, 1);
            scRt.anchorMax = new Vector2(1, 1);
            scRt.pivot = new Vector2(0, 1);
            scRt.anchoredPosition = new Vector2(90, -22);
            scRt.sizeDelta = new Vector2(-150, 22);

            // Title
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(tutCard.transform, false);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "WELCOME TO CAPITAL KINGDOM";
            titleTmp.fontSize = 19;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            var tRt = titleTmp.rectTransform;
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0, 1);
            tRt.anchoredPosition = new Vector2(90, -46);
            tRt.sizeDelta = new Vector2(-150, 32);

            // Divider
            var divObj = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            divObj.transform.SetParent(tutCard.transform, false);
            var divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 1);
            divRt.anchorMax = new Vector2(1, 1);
            divRt.pivot = new Vector2(0.5f, 1);
            divRt.anchoredPosition = new Vector2(0, -84);
            divRt.sizeDelta = new Vector2(-48, 2);
            divObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            // Description Body
            var bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObj.transform.SetParent(tutCard.transform, false);
            var bodyTmp = bodyObj.GetComponent<TextMeshProUGUI>();
            bodyTmp.text = "Loading tutorial...";
            bodyTmp.fontSize = 16;
            bodyTmp.lineSpacing = 14;
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            bodyTmp.color = new Color(0.92f, 0.94f, 0.97f);
            var bdyRt = bodyTmp.rectTransform;
            bdyRt.anchorMin = new Vector2(0, 0);
            bdyRt.anchorMax = new Vector2(1, 1);
            bdyRt.pivot = new Vector2(0.5f, 0.5f);
            bdyRt.anchoredPosition = new Vector2(0, -10);
            bdyRt.sizeDelta = new Vector2(-54, -170);

            // Bottom Navigation Bar
            var bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(tutCard.transform, false);
            var bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0, 0);
            bbRt.anchorMax = new Vector2(1, 0);
            bbRt.pivot = new Vector2(0.5f, 0);
            bbRt.anchoredPosition = new Vector2(0, 16);
            bbRt.sizeDelta = new Vector2(-48, 56);

            // Back button
            if (backToMenuButton != null)
            {
                var backBtn = Instantiate(backToMenuButton.gameObject, bottomBar.transform, false);
                backBtn.name = "BackButton";
                var bkRt = backBtn.GetComponent<RectTransform>();
                bkRt.anchorMin = new Vector2(0, 0.5f);
                bkRt.anchorMax = new Vector2(0, 0.5f);
                bkRt.pivot = new Vector2(0, 0.5f);
                bkRt.anchoredPosition = new Vector2(10, 0);
                bkRt.sizeDelta = new Vector2(130, 48);
                var bkTmp = backBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (bkTmp != null) bkTmp.text = "◀ BACK";

                // Skip button
                var skipBtn = Instantiate(backToMenuButton.gameObject, bottomBar.transform, false);
                skipBtn.name = "SkipButton";
                var skRt = skipBtn.GetComponent<RectTransform>();
                skRt.anchorMin = new Vector2(0.5f, 0.5f);
                skRt.anchorMax = new Vector2(0.5f, 0.5f);
                skRt.pivot = new Vector2(0.5f, 0.5f);
                skRt.anchoredPosition = new Vector2(-20, 0);
                skRt.sizeDelta = new Vector2(140, 44);
                var skTmp = skipBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (skTmp != null) skTmp.text = "SKIP GUIDE";

                // Next button
                var nextBtn = Instantiate(backToMenuButton.gameObject, bottomBar.transform, false);
                nextBtn.name = "NextButton";
                var nxtRt = nextBtn.GetComponent<RectTransform>();
                nxtRt.anchorMin = new Vector2(1, 0.5f);
                nxtRt.anchorMax = new Vector2(1, 0.5f);
                nxtRt.pivot = new Vector2(1, 0.5f);
                nxtRt.anchoredPosition = new Vector2(-10, 0);
                nxtRt.sizeDelta = new Vector2(170, 50);
                var nxtTmp = nextBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (nxtTmp != null) nxtTmp.text = "NEXT ➔";
                var nxtImg = nextBtn.GetComponent<Image>();
                if (nxtImg != null) nxtImg.color = new Color(0.18f, 0.80f, 0.44f);
            }

            tutModalRoot.AddComponent<GameTutorialManager>();
            tutModalRoot.SetActive(false);
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
            // Auto save game state before returning to Main Menu
            WebGLSaveManager.Instance?.SaveGame();
            PlayerPrefs.Save();

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}

