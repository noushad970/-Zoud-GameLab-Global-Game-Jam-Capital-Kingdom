#if UNITY_EDITOR
using System;
using EmpireBuilder.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.Editor
{
    /// <summary>
    /// Permanently generates and serializes static UI elements in the GamePlay scene hierarchy.
    /// This includes:
    /// 1. HeaderUI -> DateTimeBadge (with Day counter, In-Game Time, In-Game Date)
    /// 2. PropertyDetailModal -> Card -> BankSection (with Account Opening, Deposit, Withdraw, Quick buttons, TMP_InputField)
    /// 3. PropertyDetailModal -> Card -> HospitalSection, FactorySection, OilSection, ParkSection, MaintenanceSection
    /// 
    /// Because these GameObjects are permanently serialized in the scene hierarchy,
    /// developers can easily view, select, resize, restyle, move, or modify any element
    /// directly from the Unity Scene View and Hierarchy window!
    /// </summary>
    [InitializeOnLoad]
    public static class StaticUIHierarchyBuilder
    {
        private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
        private const string SpriteRoundedRect = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png";
        private const string FontLilita = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";

        static StaticUIHierarchyBuilder()
        {
            EditorApplication.update += CheckAndBuildOnUpdate;
        }

        private static void CheckAndBuildOnUpdate()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            if (!scene.name.Equals("GamePlay", StringComparison.OrdinalIgnoreCase)) return;

            // Check if BankSection already exists statically in the scene
            var modal = UnityEngine.Object.FindAnyObjectByType<PropertyDetailModalUI>();
            if (modal != null)
            {
                Transform card = modal.ModalCardTransform;
                if (card == null)
                {
                    Transform modalRoot = modal.transform.Find("PropertyDetailModal");
                    if (modalRoot != null) card = modalRoot.Find("ModalCard") ?? modalRoot.Find("Card");
                }
                if (card == null)
                {
                    card = modal.transform.Find("PropertyDetailModal/ModalCard") ?? modal.transform.Find("ModalCard");
                }

                if (card != null && card.Find("BankSection") != null)
                {
                    EditorApplication.update -= CheckAndBuildOnUpdate;
                    return;
                }
            }

            EditorApplication.update -= CheckAndBuildOnUpdate;
            Debug.Log("[StaticUIHierarchyBuilder] Auto-building static scene UI in hierarchy...");
            BuildAllStaticUI(false);
        }

        [MenuItem("Tools/Empire Builder/Build Static Scene UI (Header & Bank Modal)", false, 2)]
        public static void BuildStaticUIMenu()
        {
            BuildAllStaticUI(true);
        }

        public static void BuildStaticUIBatch()
        {
            Debug.Log("[StaticUIHierarchyBuilder] Batch mode execution started...");
            BuildAllStaticUI(false);
            Debug.Log("[StaticUIHierarchyBuilder] Batch mode execution finished successfully!");
        }

        public static void BuildAllStaticUI(bool interactive)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Equals("GamePlay", StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
            }

            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoundedRect);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);

            // 1. Build Static DateTimeBadge under HeaderUI
            BuildStaticHeaderUI(roundedSprite, fontAsset);

            // 2. Build Static Special Sections under Card in PropertyDetailModal
            BuildStaticModalSections(roundedSprite, fontAsset);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StaticUIHierarchyBuilder] Successfully built and saved static UI in GamePlay scene! (Saved: {saved})");
            if (interactive)
            {
                EditorUtility.DisplayDialog("Static UI Built", "All Header and Bank Modal UI elements are now permanently saved in the Scene Hierarchy!\nYou can now click and modify them directly in the Hierarchy and Scene View.", "OK");
            }
        }

        private static void BuildStaticHeaderUI(Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            var headerUI = UnityEngine.Object.FindAnyObjectByType<GamePlayHeaderUI>();
            if (headerUI == null)
            {
                Debug.LogWarning("[StaticUIHierarchyBuilder] GamePlayHeaderUI component not found in scene.");
                return;
            }

            Transform headerTr = headerUI.transform;
            Transform badgeTr = headerTr.Find("DateTimeBadge");
            GameObject badgeObj;

            if (badgeTr == null)
            {
                badgeObj = new GameObject("DateTimeBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(badgeObj, "Create DateTimeBadge");
                badgeObj.transform.SetParent(headerTr, false);
            }
            else
            {
                badgeObj = badgeTr.gameObject;
            }

            var badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRt.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(20f, 0f);
            badgeRt.sizeDelta = new Vector2(380f, 44f);

            var badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
            if (roundedSprite != null)
            {
                badgeImg.sprite = roundedSprite;
                badgeImg.type = Image.Type.Sliced;
            }

            Transform textTr = badgeObj.transform.Find("DateTimeText");
            GameObject textObj;
            if (textTr == null)
            {
                textObj = new GameObject("DateTimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(textObj, "Create DateTimeText");
                textObj.transform.SetParent(badgeObj.transform, false);
            }
            else
            {
                textObj = textTr.gameObject;
            }

            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = "<color=#FFD700>DAY 1 IN CITY</color>  <color=#556677>•</color>  <color=#FFFFFF>06:00 AM</color>  <color=#556677>•</color>  <color=#70C5FF>Oct 01, 2026</color>";
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Wire serialized fields on GamePlayHeaderUI
            SerializedObject so = new SerializedObject(headerUI);
            so.FindProperty("dateTimeBadgeRoot").objectReferenceValue = badgeObj;
            so.FindProperty("dateTimeText").objectReferenceValue = tmp;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(headerUI);
            Debug.Log("[StaticUIHierarchyBuilder] Header DateTimeBadge statically configured in hierarchy.");
        }

        private static void BuildStaticModalSections(Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            var modalUI = UnityEngine.Object.FindAnyObjectByType<PropertyDetailModalUI>();
            if (modalUI == null)
            {
                Debug.LogWarning("[StaticUIHierarchyBuilder] PropertyDetailModalUI not found in scene.");
                return;
            }

            Transform cardTr = modalUI.ModalCardTransform;
            if (cardTr == null)
            {
                Transform canvasTr = modalUI.transform;
                Transform modalRoot = canvasTr.Find("PropertyDetailModal");
                if (modalRoot != null)
                {
                    cardTr = modalRoot.Find("ModalCard") ?? modalRoot.Find("Card");
                }
                if (cardTr == null)
                {
                    cardTr = canvasTr.Find("PropertyDetailModal/ModalCard")
                          ?? canvasTr.Find("PropertyDetailModal/Card")
                          ?? canvasTr.Find("ModalCard");
                }
            }

            if (cardTr == null)
            {
                Debug.LogWarning("[StaticUIHierarchyBuilder] ModalCard GameObject not found under PropertyDetailModal.");
                return;
            }

            SerializedObject so = new SerializedObject(modalUI);

            // 1. Build BankSection
            BuildBankSection(cardTr, so, roundedSprite, fontAsset);

            // 2. Build HospitalSection
            BuildHospitalSection(cardTr, so, roundedSprite, fontAsset);

            // 3. Build FactorySection
            BuildFactorySection(cardTr, so, roundedSprite, fontAsset);

            // 4. Build OilSection
            BuildOilSection(cardTr, so, roundedSprite, fontAsset);

            // 5. Build ParkSection
            BuildParkSection(cardTr, so, roundedSprite, fontAsset);

            // 6. Build MaintenanceSection
            BuildMaintenanceSection(cardTr, so, roundedSprite, fontAsset);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(modalUI);
        }

        private static void BuildBankSection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("BankSection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("BankSection", cardTr, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 195), new Vector2(0, -250));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.10f, 0.14f, 0.22f, 0.98f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            // Title
            Transform tTr = root.transform.Find("BankTitle");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("BankTitle", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 22), new Vector2(15, -10));
            TextMeshProUGUI bTitle = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) bTitle.font = fontAsset;
            bTitle.text = "🏛️ CAPITAL NATIONAL BANK & SAVINGS";
            bTitle.fontSize = 13;
            bTitle.fontStyle = FontStyles.Bold;
            bTitle.alignment = TextAlignmentOptions.Left;
            bTitle.color = new Color(1f, 0.85f, 0.25f);

            // Status
            Transform sTr = root.transform.Find("BankStatus");
            GameObject sObj = sTr != null ? sTr.gameObject : CreateUIElement("BankStatus", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 48), new Vector2(15, -34));
            TextMeshProUGUI bStatus = EnsureComponent<TextMeshProUGUI>(sObj);
            if (fontAsset != null) bStatus.font = fontAsset;
            bStatus.text = "Open a savings account to store coins and earn daily compound interest.";
            bStatus.fontSize = 11;
            bStatus.color = new Color(0.90f, 0.92f, 0.96f);

            // BankClosedRoot
            Transform cTr = root.transform.Find("BankClosedRoot");
            GameObject closedRoot = cTr != null ? cTr.gameObject : CreateUIElement("BankClosedRoot", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, -25));

            Transform opTr = closedRoot.transform.Find("OpenBankButton");
            GameObject openBtnObj = opTr != null ? opTr.gameObject : CreateUIElement("OpenBankButton", closedRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 46), new Vector2(0, 15));
            Image openBg = EnsureComponent<Image>(openBtnObj);
            openBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedSprite != null) { openBg.sprite = roundedSprite; openBg.type = Image.Type.Sliced; }
            Button openBtn = EnsureComponent<Button>(openBtnObj);

            Transform opTextTr = openBtnObj.transform.Find("Text");
            GameObject opTextObj = opTextTr != null ? opTextTr.gameObject : CreateUIElement("Text", openBtnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI openBtnText = EnsureComponent<TextMeshProUGUI>(opTextObj);
            if (fontAsset != null) openBtnText.font = fontAsset;
            openBtnText.text = "🏦 OPEN BANK ACCOUNT (FEE: 100 COINS)";
            openBtnText.fontSize = 13;
            openBtnText.fontStyle = FontStyles.Bold;
            openBtnText.alignment = TextAlignmentOptions.Center;
            openBtnText.color = Color.white;

            // BankActiveRoot
            Transform aTr = root.transform.Find("BankActiveRoot");
            GameObject activeRoot = aTr != null ? aTr.gameObject : CreateUIElement("BankActiveRoot", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 108), new Vector2(0, 6));

            // QuickRow
            Transform qTr = activeRoot.transform.Find("QuickRow");
            GameObject quickRow = qTr != null ? qTr.gameObject : CreateUIElement("QuickRow", activeRoot.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-30, 26), new Vector2(0, 0));
            BuildQuickBtn("+$100", quickRow.transform, new Vector2(0, 0), new Vector2(0.23f, 1), roundedSprite, fontAsset);
            BuildQuickBtn("+$500", quickRow.transform, new Vector2(0.25f, 0), new Vector2(0.48f, 1), roundedSprite, fontAsset);
            BuildQuickBtn("+$1K", quickRow.transform, new Vector2(0.50f, 0), new Vector2(0.73f, 1), roundedSprite, fontAsset);
            BuildQuickBtn("MAX CASH", quickRow.transform, new Vector2(0.75f, 0), new Vector2(1f, 1), roundedSprite, fontAsset);

            // Input Field
            TMP_InputField inputField = BuildInputField("BankAmountInput", activeRoot.transform, new Vector2(0, 0), new Vector2(0.45f, 0), new Vector2(0, 0), new Vector2(0, 38), new Vector2(15, 34), "Amount...", roundedSprite, fontAsset);

            // Deposit Button
            Transform dpTr = activeRoot.transform.Find("DepositButton");
            GameObject depBtnObj = dpTr != null ? dpTr.gameObject : CreateUIElement("DepositButton", activeRoot.transform, new Vector2(0.48f, 0), new Vector2(0.73f, 0), new Vector2(0.5f, 0), new Vector2(0, 38), new Vector2(0, 34));
            Image depBg = EnsureComponent<Image>(depBtnObj);
            depBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedSprite != null) { depBg.sprite = roundedSprite; depBg.type = Image.Type.Sliced; }
            Button depBtn = EnsureComponent<Button>(depBtnObj);

            Transform dpTextTr = depBtnObj.transform.Find("Text");
            GameObject dpTextObj = dpTextTr != null ? dpTextTr.gameObject : CreateUIElement("Text", depBtnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI depText = EnsureComponent<TextMeshProUGUI>(dpTextObj);
            if (fontAsset != null) depText.font = fontAsset;
            depText.text = "📥 DEPOSIT";
            depText.fontSize = 12;
            depText.fontStyle = FontStyles.Bold;
            depText.alignment = TextAlignmentOptions.Center;
            depText.color = Color.white;

            // Withdraw Button
            Transform wtTr = activeRoot.transform.Find("WithdrawButton");
            GameObject wthBtnObj = wtTr != null ? wtTr.gameObject : CreateUIElement("WithdrawButton", activeRoot.transform, new Vector2(0.75f, 0), new Vector2(1f, 0), new Vector2(0.5f, 0), new Vector2(-15, 38), new Vector2(0, 34));
            Image wthBg = EnsureComponent<Image>(wthBtnObj);
            wthBg.color = new Color(0.92f, 0.45f, 0.15f, 1f);
            if (roundedSprite != null) { wthBg.sprite = roundedSprite; wthBg.type = Image.Type.Sliced; }
            Button wthBtn = EnsureComponent<Button>(wthBtnObj);

            Transform wtTextTr = wthBtnObj.transform.Find("Text");
            GameObject wtTextObj = wtTextTr != null ? wtTextTr.gameObject : CreateUIElement("Text", wthBtnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI wthText = EnsureComponent<TextMeshProUGUI>(wtTextObj);
            if (fontAsset != null) wthText.font = fontAsset;
            wthText.text = "📤 WITHDRAW";
            wthText.fontSize = 12;
            wthText.fontStyle = FontStyles.Bold;
            wthText.alignment = TextAlignmentOptions.Center;
            wthText.color = Color.white;

            // Withdraw All Button
            Transform waTr = activeRoot.transform.Find("WithdrawAllButton");
            GameObject wAllBtnObj = waTr != null ? waTr.gameObject : CreateUIElement("WithdrawAllButton", activeRoot.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 24), new Vector2(0, 2));
            Image wAllBg = EnsureComponent<Image>(wAllBtnObj);
            wAllBg.color = new Color(0.24f, 0.30f, 0.40f, 0.90f);
            if (roundedSprite != null) { wAllBg.sprite = roundedSprite; wAllBg.type = Image.Type.Sliced; }
            Button wAllBtn = EnsureComponent<Button>(wAllBtnObj);

            Transform waTextTr = wAllBtnObj.transform.Find("Text");
            GameObject waTextObj = waTextTr != null ? waTextTr.gameObject : CreateUIElement("Text", wAllBtnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI wAllText = EnsureComponent<TextMeshProUGUI>(waTextObj);
            if (fontAsset != null) wAllText.font = fontAsset;
            wAllText.text = "WITHDRAW ALL TO POCKET CASH";
            wAllText.fontSize = 11;
            wAllText.fontStyle = FontStyles.Bold;
            wAllText.alignment = TextAlignmentOptions.Center;
            wAllText.color = new Color(0.95f, 0.95f, 0.95f);

            // Default editor visibility: BankSection inactive so standard view shows
            root.SetActive(false);

            // Wire Serialized Properties
            so.FindProperty("bankSectionRoot").objectReferenceValue = root;
            so.FindProperty("bankStatusText").objectReferenceValue = bStatus;
            so.FindProperty("bankClosedRoot").objectReferenceValue = closedRoot;
            so.FindProperty("openBankButton").objectReferenceValue = openBtn;
            so.FindProperty("openBankButtonText").objectReferenceValue = openBtnText;
            so.FindProperty("openBankButtonBg").objectReferenceValue = openBg;
            so.FindProperty("bankActiveRoot").objectReferenceValue = activeRoot;
            so.FindProperty("bankDepositInputField").objectReferenceValue = inputField;
            so.FindProperty("depositButton").objectReferenceValue = depBtn;
            so.FindProperty("depositButtonText").objectReferenceValue = depText;
            so.FindProperty("depositButtonBg").objectReferenceValue = depBg;
            so.FindProperty("withdrawButton").objectReferenceValue = wthBtn;
            so.FindProperty("withdrawButtonText").objectReferenceValue = wthText;
            so.FindProperty("withdrawButtonBg").objectReferenceValue = wthBg;
            so.FindProperty("withdrawAllButton").objectReferenceValue = wAllBtn;
            so.FindProperty("withdrawAllButtonText").objectReferenceValue = wAllText;

            Debug.Log("[StaticUIHierarchyBuilder] BankSection statically configured in hierarchy.");
        }

        private static void BuildHospitalSection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("HospitalSection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("HospitalSection", cardTr, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 165), new Vector2(0, -260));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform tTr = root.transform.Find("HospTitle");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("HospTitle", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 22), new Vector2(15, -10));
            TextMeshProUGUI title = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) title.font = fontAsset;
            title.text = "🏥 MEDICAL HEALTHCARE & EMERGENCY TREATMENT";
            title.fontSize = 13;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.42f, 0.46f);

            Transform sTr = root.transform.Find("HospitalStatus");
            GameObject sObj = sTr != null ? sTr.gameObject : CreateUIElement("HospitalStatus", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 60), new Vector2(15, -34));
            TextMeshProUGUI status = EnsureComponent<TextMeshProUGUI>(sObj);
            if (fontAsset != null) status.font = fontAsset;
            status.text = "Visit for medical treatment when sick or exhausted.";
            status.fontSize = 11;

            Transform btnTr = root.transform.Find("TreatmentButton");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : CreateUIElement("TreatmentButton", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 48), new Vector2(0, 12));
            Image btnBg = EnsureComponent<Image>(btnObj);
            btnBg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (roundedSprite != null) { btnBg.sprite = roundedSprite; btnBg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI btnTxt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) btnTxt.font = fontAsset;
            btnTxt.text = "🏥 RECEIVE TREATMENT (RESTORE 100 HP)";
            btnTxt.fontSize = 13;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;

            root.SetActive(false);

            so.FindProperty("hospitalSectionRoot").objectReferenceValue = root;
            so.FindProperty("hospitalStatusText").objectReferenceValue = status;
            so.FindProperty("treatmentButton").objectReferenceValue = btn;
            so.FindProperty("treatmentButtonText").objectReferenceValue = btnTxt;
            so.FindProperty("treatmentButtonBg").objectReferenceValue = btnBg;
        }

        private static void BuildFactorySection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("FactorySection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("FactorySection", cardTr, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.15f, 0.18f, 0.24f, 0.95f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform tTr = root.transform.Find("FactTitle");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("FactTitle", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 22), new Vector2(15, -10));
            TextMeshProUGUI title = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) title.font = fontAsset;
            title.text = "INDUSTRIAL MANUFACTURING PRODUCTION";
            title.fontSize = 13;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.45f, 0.75f, 1f);

            Transform sTr = root.transform.Find("FactoryStatus");
            GameObject sObj = sTr != null ? sTr.gameObject : CreateUIElement("FactoryStatus", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 34), new Vector2(15, -34));
            TextMeshProUGUI status = EnsureComponent<TextMeshProUGUI>(sObj);
            if (fontAsset != null) status.font = fontAsset;
            status.text = "Heavy manufacturing plant. Completes production runs with large payout revenues.";
            status.fontSize = 11;

            Transform btnTr = root.transform.Find("CollectProductionButton");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : CreateUIElement("CollectProductionButton", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            Image btnBg = EnsureComponent<Image>(btnObj);
            btnBg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            if (roundedSprite != null) { btnBg.sprite = roundedSprite; btnBg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI btnTxt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) btnTxt.font = fontAsset;
            btnTxt.text = "COLLECT PRODUCTION REVENUE";
            btnTxt.fontSize = 13;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;

            root.SetActive(false);

            so.FindProperty("factorySectionRoot").objectReferenceValue = root;
            so.FindProperty("factoryStatusText").objectReferenceValue = status;
            so.FindProperty("collectProductionButton").objectReferenceValue = btn;
            so.FindProperty("collectProductionButtonText").objectReferenceValue = btnTxt;
            so.FindProperty("collectProductionButtonBg").objectReferenceValue = btnBg;
        }

        private static void BuildOilSection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("OilSection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("OilSection", cardTr, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.22f, 0.18f, 0.12f, 0.95f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform tTr = root.transform.Find("OilTitle");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("OilTitle", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 22), new Vector2(15, -10));
            TextMeshProUGUI title = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) title.font = fontAsset;
            title.text = "OIL & FUEL DISTRIBUTION SALES";
            title.fontSize = 13;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.82f, 0.25f);

            Transform sTr = root.transform.Find("OilStatus");
            GameObject sObj = sTr != null ? sTr.gameObject : CreateUIElement("OilStatus", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 34), new Vector2(15, -34));
            TextMeshProUGUI status = EnsureComponent<TextMeshProUGUI>(sObj);
            if (fontAsset != null) status.font = fontAsset;
            status.text = "Commercial fueling station. Earn revenue from citywide vehicle fuel consumption.";
            status.fontSize = 11;

            Transform btnTr = root.transform.Find("CollectOilButton");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : CreateUIElement("CollectOilButton", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            Image btnBg = EnsureComponent<Image>(btnObj);
            btnBg.color = new Color(0.95f, 0.65f, 0.12f, 1f);
            if (roundedSprite != null) { btnBg.sprite = roundedSprite; btnBg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI btnTxt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) btnTxt.font = fontAsset;
            btnTxt.text = "COLLECT OIL REVENUE";
            btnTxt.fontSize = 13;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;

            root.SetActive(false);

            so.FindProperty("oilSectionRoot").objectReferenceValue = root;
            so.FindProperty("oilStatusText").objectReferenceValue = status;
            so.FindProperty("collectOilButton").objectReferenceValue = btn;
            so.FindProperty("collectOilButtonText").objectReferenceValue = btnTxt;
            so.FindProperty("collectOilButtonBg").objectReferenceValue = btnBg;
        }

        private static void BuildParkSection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("ParkSection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("ParkSection", cardTr, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 155), new Vector2(0, -260));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.12f, 0.22f, 0.15f, 0.95f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform tTr = root.transform.Find("ParkTitle");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("ParkTitle", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 22), new Vector2(15, -10));
            TextMeshProUGUI title = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) title.font = fontAsset;
            title.text = "MUNICIPAL CENTRAL PARK OPERATIONS";
            title.fontSize = 13;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.45f, 0.90f, 0.55f);

            Transform sTr = root.transform.Find("ParkStatus");
            GameObject sObj = sTr != null ? sTr.gameObject : CreateUIElement("ParkStatus", root.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(-30, 34), new Vector2(15, -34));
            TextMeshProUGUI status = EnsureComponent<TextMeshProUGUI>(sObj);
            if (fontAsset != null) status.font = fontAsset;
            status.text = "Central botanical park. Earn visitor entry fees from citizens walking through.";
            status.fontSize = 11;

            Transform btnTr = root.transform.Find("CollectParkButton");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : CreateUIElement("CollectParkButton", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            Image btnBg = EnsureComponent<Image>(btnObj);
            btnBg.color = new Color(0.15f, 0.72f, 0.40f, 1f);
            if (roundedSprite != null) { btnBg.sprite = roundedSprite; btnBg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI btnTxt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) btnTxt.font = fontAsset;
            btnTxt.text = "COLLECT PARK ENTRY FEES";
            btnTxt.fontSize = 13;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;

            root.SetActive(false);

            so.FindProperty("parkSectionRoot").objectReferenceValue = root;
            so.FindProperty("parkStatusText").objectReferenceValue = status;
            so.FindProperty("collectParkButton").objectReferenceValue = btn;
            so.FindProperty("collectParkButtonText").objectReferenceValue = btnTxt;
            so.FindProperty("collectParkButtonBg").objectReferenceValue = btnBg;
        }

        private static void BuildMaintenanceSection(Transform cardTr, SerializedObject so, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = cardTr.Find("MaintenanceSection");
            GameObject root = existing != null ? existing.gameObject : CreateUIElement("MaintenanceSection", cardTr, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 72), new Vector2(0, 136));
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.32f, 0.12f, 0.12f, 0.95f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform rTr = root.transform.Find("MaintenanceReason");
            GameObject rObj = rTr != null ? rTr.gameObject : CreateUIElement("MaintenanceReason", root.transform, new Vector2(0, 0.5f), new Vector2(0.55f, 1), new Vector2(0, 0.5f), new Vector2(-15, 0), new Vector2(10, 0));
            TextMeshProUGUI reason = EnsureComponent<TextMeshProUGUI>(rObj);
            if (fontAsset != null) reason.font = fontAsset;
            reason.text = "⚠️ MAINTENANCE REQUIRED: -$0";
            reason.fontSize = 11;
            reason.fontStyle = FontStyles.Bold;
            reason.color = new Color(1f, 0.75f, 0.75f);

            Transform btnTr = root.transform.Find("PayMaintenanceButton");
            GameObject btnObj = btnTr != null ? btnTr.gameObject : CreateUIElement("PayMaintenanceButton", root.transform, new Vector2(0.55f, 0.1f), new Vector2(0.98f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image btnBg = EnsureComponent<Image>(btnObj);
            btnBg.color = new Color(0.90f, 0.35f, 0.15f, 1f);
            if (roundedSprite != null) { btnBg.sprite = roundedSprite; btnBg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI btnTxt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) btnTxt.font = fontAsset;
            btnTxt.text = "PAY MAINTENANCE";
            btnTxt.fontSize = 11;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;

            root.SetActive(false);

            so.FindProperty("maintenanceSectionRoot").objectReferenceValue = root;
            so.FindProperty("maintenanceReasonText").objectReferenceValue = reason;
            so.FindProperty("payMaintenanceButton").objectReferenceValue = btn;
            so.FindProperty("payMaintenanceButtonText").objectReferenceValue = btnTxt;
            so.FindProperty("payMaintenanceButtonBg").objectReferenceValue = btnBg;
        }

        private static Button BuildQuickBtn(string label, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = parent.Find("QuickBtn_" + label);
            GameObject btnObj = existing != null ? existing.gameObject : CreateUIElement("QuickBtn_" + label, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image bg = EnsureComponent<Image>(btnObj);
            bg.color = new Color(0.18f, 0.24f, 0.35f, 0.95f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }
            Button btn = EnsureComponent<Button>(btnObj);

            Transform txtTr = btnObj.transform.Find("Text");
            GameObject txtObj = txtTr != null ? txtTr.gameObject : CreateUIElement("Text", btnObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI txt = EnsureComponent<TextMeshProUGUI>(txtObj);
            if (fontAsset != null) txt.font = fontAsset;
            txt.text = label;
            txt.fontSize = 11;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            return btn;
        }

        private static TMP_InputField BuildInputField(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos, string placeholderText, Sprite roundedSprite, TMP_FontAsset fontAsset)
        {
            Transform existing = parent.Find(name);
            GameObject root = existing != null ? existing.gameObject : CreateUIElement(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPos);
            Image bg = EnsureComponent<Image>(root);
            bg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
            if (roundedSprite != null) { bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; }

            Transform taTr = root.transform.Find("TextArea");
            GameObject textArea = taTr != null ? taTr.gameObject : CreateUIElement("TextArea", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-16, -6), Vector2.zero);
            EnsureComponent<RectMask2D>(textArea);

            Transform phTr = textArea.transform.Find("Placeholder");
            GameObject phObj = phTr != null ? phTr.gameObject : CreateUIElement("Placeholder", textArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI placeholder = EnsureComponent<TextMeshProUGUI>(phObj);
            if (fontAsset != null) placeholder.font = fontAsset;
            placeholder.text = placeholderText;
            placeholder.fontSize = 12;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.color = new Color(0.55f, 0.60f, 0.70f, 0.7f);

            Transform tTr = textArea.transform.Find("Text");
            GameObject tObj = tTr != null ? tTr.gameObject : CreateUIElement("Text", textArea.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = EnsureComponent<TextMeshProUGUI>(tObj);
            if (fontAsset != null) text.font = fontAsset;
            text.text = "";
            text.fontSize = 13;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            TMP_InputField inputField = EnsureComponent<TMP_InputField>(root);
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.characterLimit = 9;

            return inputField;
        }

        private static GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = Undo.AddComponent<T>(go);
            return comp;
        }
    }
}
#endif
