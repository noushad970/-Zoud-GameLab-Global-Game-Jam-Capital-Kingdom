#if UNITY_EDITOR
using System;
using EmpireBuilder.Localization;
using EmpireBuilder.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.Editor
{
    /// <summary>
    /// Permanently generates and serializes static Language UI elements in the MainMenu scene hierarchy.
    /// This includes:
    /// 1. MainMenuCanvas -> OptionModal -> LanguageSection (LanguageLabel, ButtonsRow, EnglishButton, ArabicButton)
    /// 2. MainMenuCanvas -> QuickLanguageButton (with Text child)
    /// 3. Wires all serialized references directly to MainMenuUI on MainMenuController.
    /// 
    /// Because these GameObjects are serialized in the scene hierarchy,
    /// developers can easily select, reposition, restyle, scale, or edit them
    /// directly from Unity's Scene View and Hierarchy window!
    /// </summary>
    [InitializeOnLoad]
    public static class StaticMainMenuUIBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string SpriteRoundedRect = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png";
        private const string FontLilita = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";
        private const string FontTahoma = "Assets/Resources/Fonts/Tahoma SDF.asset";

        static StaticMainMenuUIBuilder()
        {
            EditorApplication.update += CheckAndBuildOnUpdate;
        }

        private static void CheckAndBuildOnUpdate()
        {
            EditorApplication.update -= CheckAndBuildOnUpdate;

            try
            {
                if (System.IO.File.Exists(MainMenuScenePath))
                {
                    string sceneText = System.IO.File.ReadAllText(MainMenuScenePath);
                    if (sceneText.Contains("LanguageSection") && sceneText.Contains("QuickLanguageButton"))
                    {
                        return;
                    }
                }
            }
            catch
            {
                // Ignore file read error and proceed
            }

            Debug.Log("[StaticMainMenuUIBuilder] Auto-building static MainMenu Language UI in hierarchy...");
            BuildStaticMainMenuUI(false);
        }

        [MenuItem("Tools/Empire Builder/Build Static Main Menu Language UI", false, 3)]
        public static void BuildStaticMainMenuUIMenu()
        {
            BuildStaticMainMenuUI(true);
        }

        public static void BuildStaticMainMenuUIBatch()
        {
            Debug.Log("[StaticMainMenuUIBuilder] Batch mode execution started...");
            BuildStaticMainMenuUI(false);
            Debug.Log("[StaticMainMenuUIBuilder] Batch mode execution finished successfully!");
        }

        public static void BuildStaticMainMenuUI(bool interactive)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string prevPath = activeScene.path;
            bool wasOtherScene = !activeScene.name.Equals("MainMenu", StringComparison.OrdinalIgnoreCase);

            var scene = activeScene;
            if (wasOtherScene)
            {
                scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            }

            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoundedRect);
            var fontLilita = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);
            var fontTahoma = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontTahoma);
            var defaultFont = fontLilita != null ? fontLilita : fontTahoma;

            var menuUI = UnityEngine.Object.FindAnyObjectByType<MainMenuUI>();
            if (menuUI == null)
            {
                Debug.LogWarning("[StaticMainMenuUIBuilder] MainMenuUI component not found in MainMenu scene.");
                return;
            }

            Canvas canvas = menuUI.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[StaticMainMenuUIBuilder] Canvas not found in MainMenu scene.");
                return;
            }

            Transform canvasTr = canvas.transform;
            Transform optModalTr = canvasTr.Find("OptionModal");

            SerializedObject soMenuUI = new SerializedObject(menuUI);

            // 1. Build QuickLanguageButton under canvas
            Button quickButtonComp = null;
            TextMeshProUGUI quickTextComp = null;
            Transform quickBtnTr = canvasTr.Find("QuickLanguageButton");
            if (quickBtnTr == null)
            {
                GameObject qbObj = new GameObject("QuickLanguageButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                qbObj.transform.SetParent(canvasTr, false);
                quickBtnTr = qbObj.transform;

                RectTransform qbRt = qbObj.GetComponent<RectTransform>();
                qbRt.anchorMin = new Vector2(0, 1);
                qbRt.anchorMax = new Vector2(0, 1);
                qbRt.pivot = new Vector2(0, 1);
                qbRt.anchoredPosition = new Vector2(40, -35);
                qbRt.sizeDelta = new Vector2(160, 50);

                Image qbImg = qbObj.GetComponent<Image>();
                if (roundedSprite != null)
                {
                    qbImg.sprite = roundedSprite;
                    qbImg.type = Image.Type.Sliced;
                }
                qbImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                quickButtonComp = qbObj.GetComponent<Button>();

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(qbObj.transform, false);
                RectTransform tRt = txtObj.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.sizeDelta = Vector2.zero;

                quickTextComp = txtObj.GetComponent<TextMeshProUGUI>();
                if (fontTahoma != null) quickTextComp.font = fontTahoma;
                else if (defaultFont != null) quickTextComp.font = defaultFont;
                quickTextComp.text = "العربية";
                quickTextComp.fontSize = 16;
                quickTextComp.fontStyle = FontStyles.Bold;
                quickTextComp.alignment = TextAlignmentOptions.Center;
                quickTextComp.color = new Color(1f, 0.85f, 0.3f, 1f);

                Undo.RegisterCreatedObjectUndo(qbObj, "Create QuickLanguageButton");
            }
            else
            {
                quickButtonComp = quickBtnTr.GetComponent<Button>();
                quickTextComp = quickBtnTr.GetComponentInChildren<TextMeshProUGUI>();
            }

            // 2. Build LanguageSection inside OptionModal
            GameObject langSectionObj = null;
            Button englishBtnComp = null;
            Button arabicBtnComp = null;
            TextMeshProUGUI englishTxtComp = null;
            TextMeshProUGUI arabicTxtComp = null;

            if (optModalTr != null)
            {
                // Resize OptionModal to fit language section cleanly
                RectTransform optModalRt = optModalTr.GetComponent<RectTransform>();
                if (optModalRt != null)
                {
                    optModalRt.sizeDelta = new Vector2(520, 440);
                }

                // Adjust existing children layout
                Transform optTitle = optModalTr.Find("OptTitle");
                if (optTitle != null)
                {
                    var rt = optTitle.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0, 160);
                }

                Transform volLabel = optModalTr.Find("VolLabel");
                if (volLabel != null)
                {
                    var rt = volLabel.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0, 105);
                }

                Transform volSlider = optModalTr.Find("VolumeSlider");
                if (volSlider != null)
                {
                    var rt = volSlider.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0, 70);
                }

                Transform closeBtn = optModalTr.Find("CloseOptionButton");
                if (closeBtn != null)
                {
                    var rt = closeBtn.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0, -160);
                }

                Transform secTr = optModalTr.Find("LanguageSection");
                if (secTr == null)
                {
                    langSectionObj = new GameObject("LanguageSection", typeof(RectTransform));
                    langSectionObj.transform.SetParent(optModalTr, false);
                    secTr = langSectionObj.transform;

                    RectTransform secRt = langSectionObj.GetComponent<RectTransform>();
                    secRt.anchorMin = new Vector2(0.5f, 0.5f);
                    secRt.anchorMax = new Vector2(0.5f, 0.5f);
                    secRt.pivot = new Vector2(0.5f, 0.5f);
                    secRt.anchoredPosition = new Vector2(0, -30);
                    secRt.sizeDelta = new Vector2(440, 120);

                    // LanguageLabel
                    GameObject lblObj = new GameObject("LanguageLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    lblObj.transform.SetParent(secTr, false);
                    RectTransform lblRt = lblObj.GetComponent<RectTransform>();
                    lblRt.anchorMin = new Vector2(0, 1);
                    lblRt.anchorMax = new Vector2(1, 1);
                    lblRt.pivot = new Vector2(0.5f, 1);
                    lblRt.anchoredPosition = new Vector2(0, 0);
                    lblRt.sizeDelta = new Vector2(440, 30);

                    var lblTmp = lblObj.GetComponent<TextMeshProUGUI>();
                    if (fontTahoma != null) lblTmp.font = fontTahoma;
                    else if (defaultFont != null) lblTmp.font = defaultFont;
                    lblTmp.text = "LANGUAGE / اللغة";
                    lblTmp.fontSize = 15;
                    lblTmp.fontStyle = FontStyles.Bold;
                    lblTmp.alignment = TextAlignmentOptions.Center;
                    lblTmp.color = new Color(0.95f, 0.85f, 0.35f);

                    // ButtonsRow
                    GameObject rowObj = new GameObject("ButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    rowObj.transform.SetParent(secTr, false);
                    RectTransform rowRt = rowObj.GetComponent<RectTransform>();
                    rowRt.anchorMin = new Vector2(0, 0);
                    rowRt.anchorMax = new Vector2(1, 0);
                    rowRt.pivot = new Vector2(0.5f, 0);
                    rowRt.anchoredPosition = new Vector2(0, 10);
                    rowRt.sizeDelta = new Vector2(440, 50);

                    var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 16;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = true;
                    hlg.childForceExpandHeight = true;

                    // English Button
                    GameObject enBtnObj = new GameObject("EnglishButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    enBtnObj.transform.SetParent(rowObj.transform, false);
                    Image enImg = enBtnObj.GetComponent<Image>();
                    if (roundedSprite != null)
                    {
                        enImg.sprite = roundedSprite;
                        enImg.type = Image.Type.Sliced;
                    }
                    enImg.color = new Color(0.12f, 0.65f, 0.35f, 1f);
                    englishBtnComp = enBtnObj.GetComponent<Button>();

                    GameObject enTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    enTxtObj.transform.SetParent(enBtnObj.transform, false);
                    RectTransform enTxtRt = enTxtObj.GetComponent<RectTransform>();
                    enTxtRt.anchorMin = Vector2.zero;
                    enTxtRt.anchorMax = Vector2.one;
                    enTxtRt.sizeDelta = Vector2.zero;
                    englishTxtComp = enTxtObj.GetComponent<TextMeshProUGUI>();
                    if (defaultFont != null) englishTxtComp.font = defaultFont;
                    englishTxtComp.text = "English";
                    englishTxtComp.fontSize = 14;
                    englishTxtComp.fontStyle = FontStyles.Bold;
                    englishTxtComp.alignment = TextAlignmentOptions.Center;
                    englishTxtComp.color = Color.white;

                    // Arabic Button
                    GameObject arBtnObj = new GameObject("ArabicButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                    arBtnObj.transform.SetParent(rowObj.transform, false);
                    Image arImg = arBtnObj.GetComponent<Image>();
                    if (roundedSprite != null)
                    {
                        arImg.sprite = roundedSprite;
                        arImg.type = Image.Type.Sliced;
                    }
                    arImg.color = new Color(0.20f, 0.24f, 0.32f, 0.9f);
                    arabicBtnComp = arBtnObj.GetComponent<Button>();

                    GameObject arTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    arTxtObj.transform.SetParent(arBtnObj.transform, false);
                    RectTransform arTxtRt = arTxtObj.GetComponent<RectTransform>();
                    arTxtRt.anchorMin = Vector2.zero;
                    arTxtRt.anchorMax = Vector2.one;
                    arTxtRt.sizeDelta = Vector2.zero;
                    arabicTxtComp = arTxtObj.GetComponent<TextMeshProUGUI>();
                    if (fontTahoma != null) arabicTxtComp.font = fontTahoma;
                    else if (defaultFont != null) arabicTxtComp.font = defaultFont;
                    arabicTxtComp.text = "العربية";
                    arabicTxtComp.fontSize = 15;
                    arabicTxtComp.fontStyle = FontStyles.Bold;
                    arabicTxtComp.alignment = TextAlignmentOptions.Center;
                    arabicTxtComp.color = Color.white;

                    Undo.RegisterCreatedObjectUndo(langSectionObj, "Create LanguageSection");
                }
                else
                {
                    langSectionObj = secTr.gameObject;
                    englishBtnComp = secTr.Find("ButtonsRow/EnglishButton")?.GetComponent<Button>() ?? secTr.Find("EnglishButton")?.GetComponent<Button>();
                    arabicBtnComp = secTr.Find("ButtonsRow/ArabicButton")?.GetComponent<Button>() ?? secTr.Find("ArabicButton")?.GetComponent<Button>();
                    englishTxtComp = englishBtnComp?.GetComponentInChildren<TextMeshProUGUI>();
                    arabicTxtComp = arabicBtnComp?.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            // 3. Connect serialized properties on MainMenuUI
            soMenuUI.Update();

            var propQuickBtn = soMenuUI.FindProperty("quickLanguageButton");
            if (propQuickBtn != null && quickButtonComp != null) propQuickBtn.objectReferenceValue = quickButtonComp;

            var propQuickTxt = soMenuUI.FindProperty("quickLanguageText");
            if (propQuickTxt != null && quickTextComp != null) propQuickTxt.objectReferenceValue = quickTextComp;

            var propLangSec = soMenuUI.FindProperty("languageSection");
            if (propLangSec != null && langSectionObj != null) propLangSec.objectReferenceValue = langSectionObj;

            var propOptEnBtn = soMenuUI.FindProperty("optEnglishButton");
            if (propOptEnBtn != null && englishBtnComp != null) propOptEnBtn.objectReferenceValue = englishBtnComp;

            var propOptArBtn = soMenuUI.FindProperty("optArabicButton");
            if (propOptArBtn != null && arabicBtnComp != null) propOptArBtn.objectReferenceValue = arabicBtnComp;

            var propOptEnTxt = soMenuUI.FindProperty("optEnglishText");
            if (propOptEnTxt != null && englishTxtComp != null) propOptEnTxt.objectReferenceValue = englishTxtComp;

            var propOptArTxt = soMenuUI.FindProperty("optArabicText");
            if (propOptArTxt != null && arabicTxtComp != null) propOptArTxt.objectReferenceValue = arabicTxtComp;

            soMenuUI.ApplyModifiedProperties();
            EditorUtility.SetDirty(menuUI);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StaticMainMenuUIBuilder] Successfully built and saved static Language UI in MainMenu scene! (Saved: {saved})");

            if (wasOtherScene && !string.IsNullOrEmpty(prevPath) && System.IO.File.Exists(prevPath))
            {
                EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
            }

            if (interactive)
            {
                EditorUtility.DisplayDialog("Static Main Menu UI Built",
                    "Language Selection UI (English & Arabic buttons) and QuickLanguageButton are now permanently saved in the MainMenu scene hierarchy!\n\nYou can select and modify them directly in the Unity Hierarchy and Scene View.",
                    "OK");
            }
        }
    }
}
#endif
