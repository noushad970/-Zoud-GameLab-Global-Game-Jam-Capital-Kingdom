#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using EmpireBuilder.Assets;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Events;
using EmpireBuilder.Maintenance;
using EmpireBuilder.SaveLoad;
using EmpireBuilder.Simulation;
using EmpireBuilder.UI;
using EmpireBuilder.World;
using EmpireBuilder.Traffic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmpireBuilder.Editor
{
    public static class EmpireBuilderSetupTool
    {
        private const string DefPath = "Assets/_Game/Data/AssetDefinitions";
        private const string EventPath = "Assets/_Game/Data/Events";
        
        // Scene Paths in Assets/Scenes
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";
        private const string SourceSuburbanCityScene = "Assets/Toon Suburban Pack/Scenes/Sample Scene.unity";

        // Existing GUI sprites from GUIPackCartoon
        private const string SpriteBgHome = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Backgrounds/Home Scene/Home Scene - Background.png";
        private const string SpriteBtnGreen = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Green.png";
        private const string SpriteBtnBlue = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Blue.png";
        private const string SpriteBtnOrange = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Orange.png";
        private const string SpriteBtnTeal = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Teal.png";
        private const string SpriteBtnRed = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Red.png";
        private const string SpriteBtnDark = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Buttons/Rectangles/Gray.png";
        private const string SpritePanelBg = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 512px.png";
        private const string SpriteIconCoin = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png";
        private const string SpriteIconShop = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Commerce/Shop.png";
        private const string SpriteIconTool = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Tools/Hammer.png";
        private const string SpriteIconHeart = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Heart - Health/Heart - Red.png";
        private const string SpriteRoundedRect = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png";

        // Existing Audio & Fonts
        private const string AudioCoins = "Assets/Assets No/GUIPackCartoon/Demo/Sounds/Coins.wav";
        private const string AudioButton = "Assets/Assets No/GUIPackCartoon/Demo/Sounds/Button.wav";
        private const string FontLilita = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";

        // Existing 3D Models / Prefabs
        private const string PrefabTaxi = "Assets/Assets No/Low Poly Simple Urban City 3D Asset Pack/Prefabs/Vehicles/Emergency_Vehicles/Taxi.prefab";
        private const string PrefabHouse = "Assets/Assets No/Toon Suburban Pack/Prefabs/Buildings/House Presets/TSP_House_01A.prefab";
        private const string PrefabShop = "Assets/Assets No/Toon Suburban Pack/Prefabs/Buildings/Shop Presets/TSP_Shop_01A.prefab";

        [MenuItem("Tools/Empire Builder/1. Generate Data (Assets & Events)", false, 1)]
        public static void GenerateData()
        {
            EnsureDirectories();
            GenerateAssetDefinitions();
            GenerateGameEvents();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EmpireBuilderSetupTool] Data assets successfully generated!");
        }

        [MenuItem("Tools/Empire Builder/2. Create Main Menu & GamePlay Scenes (with Clash of Clans Camera)", false, 2)]
        public static void CreateMenuAndGamePlayScenes()
        {
            EnsureDirectories();
            GenerateData();
            BuildMainMenuScene();
            BuildGamePlayScene();
            UpdateBuildSettings();
            Debug.Log("[EmpireBuilderSetupTool] MainMenu and GamePlay scenes successfully built with Clash of Clans camera system!");
        }

        [MenuItem("Tools/Empire Builder/3. Setup AI Pedestrians in GamePlay Scene", false, 3)]
        public static void SetupAIPedestriansInScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);

            // Check if CityPedestrianManager exists
            var manager = Object.FindFirstObjectByType<CityPedestrianManager>();
            if (manager == null)
            {
                var mgrObj = new GameObject("CityPedestrianManager");
                manager = mgrObj.AddComponent<CityPedestrianManager>();
            }

            // Load character prefabs
            var femalePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/prefabs/TT_demo_female.prefab");
            var malePrefabA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/prefabs/TT_demo_male_A.prefab");
            var malePrefabB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Assets No/Tiny_Toony_City_Demo/PolygonBlacksmith/TT_Citizens_demo/prefabs/TT_demo_male_B.prefab");

            // Place sample characters in the scene
            var charRoot = GameObject.Find("City_Pedestrians");
            if (charRoot == null)
            {
                charRoot = new GameObject("City_Pedestrians");
            }

            if (femalePrefab != null)
            {
                var femaleObj = PrefabUtility.InstantiatePrefab(femalePrefab, charRoot.transform) as GameObject;
                if (femaleObj != null)
                {
                    femaleObj.name = "AI_Citizen_Female_Cafe";
                    femaleObj.transform.position = new Vector3(18f, 0f, 18f); // Cafe terrace
                    var ctrl = femaleObj.GetComponent<AIPedestrianController>();
                    if (ctrl == null) ctrl = femaleObj.AddComponent<AIPedestrianController>();
                    ctrl.Initialize(PedestrianGender.Female, "Emma", 1.55f);
                }
            }

            if (malePrefabA != null)
            {
                var maleObj = PrefabUtility.InstantiatePrefab(malePrefabA, charRoot.transform) as GameObject;
                if (maleObj != null)
                {
                    maleObj.name = "AI_Citizen_Male_Market";
                    maleObj.transform.position = new Vector3(-35f, 0f, -45f); // Market promenade
                    var ctrl = maleObj.GetComponent<AIPedestrianController>();
                    if (ctrl == null) ctrl = maleObj.AddComponent<AIPedestrianController>();
                    ctrl.Initialize(PedestrianGender.Male, "Alex", 1.65f);
                }
            }

            if (malePrefabB != null)
            {
                var maleObj2 = PrefabUtility.InstantiatePrefab(malePrefabB, charRoot.transform) as GameObject;
                if (maleObj2 != null)
                {
                    maleObj2.name = "AI_Citizen_Male_School";
                    maleObj2.transform.position = new Vector3(35f, 0f, 48f); // School campus
                    var ctrl = maleObj2.GetComponent<AIPedestrianController>();
                    if (ctrl == null) ctrl = maleObj2.AddComponent<AIPedestrianController>();
                    ctrl.Initialize(PedestrianGender.Male, "David", 1.6f);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EmpireBuilderSetupTool] AI Pedestrians successfully initialized in GamePlay scene!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(DefPath)) Directory.CreateDirectory(DefPath);
            if (!Directory.Exists(EventPath)) Directory.CreateDirectory(EventPath);
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GamePlayScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera setup
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.16f, 0.24f);
            }

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // Canvas
            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Load Existing Resources
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBgHome);
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePanelBg);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCoin);
            var btnGreen = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnGreen);
            var btnBlue = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnBlue);
            var btnOrange = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnOrange);
            var btnTeal = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnTeal);
            var btnRed = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnRed);
            var btnDark = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnDark);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);

            // 1. Full Cartoon Background
            var bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = new Color(0.15f, 0.18f, 0.26f);
            }

            // Main Menu Controller Object
            var menuControllerObj = new GameObject("MainMenuController", typeof(MainMenuUI));
            var menuUI = menuControllerObj.GetComponent<MainMenuUI>();

            // 2. Money Header (Top Right)
            var moneyHeaderObj = new GameObject("MoneyHeader", typeof(RectTransform), typeof(Image));
            moneyHeaderObj.transform.SetParent(canvasObj.transform, false);
            var moneyRect = moneyHeaderObj.GetComponent<RectTransform>();
            moneyRect.anchorMin = new Vector2(1, 1);
            moneyRect.anchorMax = new Vector2(1, 1);
            moneyRect.pivot = new Vector2(1, 1);
            moneyRect.anchoredPosition = new Vector2(-40, -35);
            moneyRect.sizeDelta = new Vector2(280, 68);

            var moneyHeaderImg = moneyHeaderObj.GetComponent<Image>();
            if (panelSprite != null) { moneyHeaderImg.sprite = panelSprite; moneyHeaderImg.type = Image.Type.Sliced; }
            moneyHeaderImg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            var coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinIconObj.transform.SetParent(moneyHeaderObj.transform, false);
            var coinRect = coinIconObj.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0, 0.5f);
            coinRect.anchorMax = new Vector2(0, 0.5f);
            coinRect.pivot = new Vector2(0, 0.5f);
            coinRect.anchoredPosition = new Vector2(15, 0);
            coinRect.sizeDelta = new Vector2(46, 46);
            if (coinSprite != null) coinIconObj.GetComponent<Image>().sprite = coinSprite;

            var moneyText = CreateTMPText("MoneyText", moneyHeaderObj.transform, "$10,000", new Vector2(70, 0), new Vector2(190, 48), 28, fontAsset, TextAlignmentOptions.Left);

            // 3. Game Title
            var titleText = CreateTMPText("GameTitle", canvasObj.transform, "EMPIRE BUILDER\n<size=50%>FROM ZERO TO TYCOON</size>", new Vector2(0, 320), new Vector2(900, 160), 64, fontAsset, TextAlignmentOptions.Center);
            titleText.color = new Color(1f, 0.9f, 0.4f);

            // 4. Menu Buttons Container
            var buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform));
            buttonsContainer.transform.SetParent(canvasObj.transform, false);
            var btnContainerRect = buttonsContainer.GetComponent<RectTransform>();
            btnContainerRect.anchoredPosition = new Vector2(0, -60);
            btnContainerRect.sizeDelta = new Vector2(380, 480);

            float startY = 140f;
            float spacingY = 75f;

            var startBtn = CreateCartoonButton("StartGameButton", buttonsContainer.transform, "START GAME", new Vector2(0, startY), new Vector2(360, 64), btnGreen, fontAsset);
            var resumeBtn = CreateCartoonButton("ResumeButton", buttonsContainer.transform, "RESUME", new Vector2(0, startY - spacingY), new Vector2(360, 64), btnBlue, fontAsset);
            var optionBtn = CreateCartoonButton("OptionButton", buttonsContainer.transform, "OPTIONS", new Vector2(0, startY - spacingY * 2), new Vector2(360, 64), btnOrange, fontAsset);
            var aboutBtn = CreateCartoonButton("AboutButton", buttonsContainer.transform, "ABOUT", new Vector2(0, startY - spacingY * 3), new Vector2(360, 64), btnTeal, fontAsset);
            var exitBtn = CreateCartoonButton("ExitButton", buttonsContainer.transform, "EXIT", new Vector2(0, startY - spacingY * 4), new Vector2(360, 64), btnRed, fontAsset);

            // 5. Option / Settings Modal
            var optionModalObj = new GameObject("OptionModal", typeof(RectTransform), typeof(Image));
            optionModalObj.transform.SetParent(canvasObj.transform, false);
            var optRect = optionModalObj.GetComponent<RectTransform>();
            optRect.anchorMin = new Vector2(0.5f, 0.5f);
            optRect.anchorMax = new Vector2(0.5f, 0.5f);
            optRect.sizeDelta = new Vector2(520, 360);
            var optImg = optionModalObj.GetComponent<Image>();
            if (panelSprite != null) { optImg.sprite = panelSprite; optImg.type = Image.Type.Sliced; }
            optImg.color = new Color(0.1f, 0.13f, 0.2f, 0.98f);

            CreateTMPText("OptTitle", optionModalObj.transform, "GAME OPTIONS", new Vector2(0, 120), new Vector2(400, 40), 28, fontAsset, TextAlignmentOptions.Center);
            CreateTMPText("VolLabel", optionModalObj.transform, "Master Volume", new Vector2(0, 45), new Vector2(360, 30), 20, fontAsset, TextAlignmentOptions.Center);

            var sliderObj = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(optionModalObj.transform, false);
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0, 5);
            sliderRect.sizeDelta = new Vector2(340, 24);
            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var closeOptBtn = CreateCartoonButton("CloseOptionButton", optionModalObj.transform, "CLOSE", new Vector2(0, -90), new Vector2(220, 50), btnDark, fontAsset);
            optionModalObj.SetActive(false);

            // 6. About Modal
            var aboutModalObj = new GameObject("AboutModal", typeof(RectTransform), typeof(Image));
            aboutModalObj.transform.SetParent(canvasObj.transform, false);
            var abtRect = aboutModalObj.GetComponent<RectTransform>();
            abtRect.anchorMin = new Vector2(0.5f, 0.5f);
            abtRect.anchorMax = new Vector2(0.5f, 0.5f);
            abtRect.sizeDelta = new Vector2(600, 380);
            var abtImg = aboutModalObj.GetComponent<Image>();
            if (panelSprite != null) { abtImg.sprite = panelSprite; abtImg.type = Image.Type.Sliced; }
            abtImg.color = new Color(0.1f, 0.13f, 0.2f, 0.98f);

            CreateTMPText("AbtTitle", aboutModalObj.transform, "ABOUT THE GAME", new Vector2(0, 130), new Vector2(500, 40), 28, fontAsset, TextAlignmentOptions.Center);
            CreateTMPText("AbtBody", aboutModalObj.transform, "Empire Builder: From Zero to Tycoon\n\nA 3D financial-literacy tycoon simulation.\nStart with limited capital, buy assets, maintain condition, and build your kingdom!", new Vector2(0, 20), new Vector2(520, 120), 18, fontAsset, TextAlignmentOptions.Center);

            var closeAbtBtn = CreateCartoonButton("CloseAboutButton", aboutModalObj.transform, "BACK", new Vector2(0, -110), new Vector2(220, 50), btnDark, fontAsset);
            aboutModalObj.SetActive(false);

            // Wire up serialized fields
            var serMenu = new SerializedObject(menuUI);
            serMenu.FindProperty("moneyText").objectReferenceValue = moneyText;
            serMenu.FindProperty("coinIcon").objectReferenceValue = coinIconObj.GetComponent<Image>();
            serMenu.FindProperty("startGameButton").objectReferenceValue = startBtn.GetComponent<Button>();
            serMenu.FindProperty("resumeButton").objectReferenceValue = resumeBtn.GetComponent<Button>();
            serMenu.FindProperty("optionButton").objectReferenceValue = optionBtn.GetComponent<Button>();
            serMenu.FindProperty("aboutButton").objectReferenceValue = aboutBtn.GetComponent<Button>();
            serMenu.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
            serMenu.FindProperty("optionPanel").objectReferenceValue = optionModalObj;
            serMenu.FindProperty("volumeSlider").objectReferenceValue = slider;
            serMenu.FindProperty("closeOptionButton").objectReferenceValue = closeOptBtn.GetComponent<Button>();
            serMenu.FindProperty("aboutPanel").objectReferenceValue = aboutModalObj;
            serMenu.FindProperty("closeAboutButton").objectReferenceValue = closeAbtBtn.GetComponent<Button>();
            serMenu.FindProperty("gamePlaySceneName").stringValue = "GamePlay";
            serMenu.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildGamePlayScene()
        {
            // Open the existing complete 3D suburban city scene so the whole city is present
            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(SourceSuburbanCityScene))
            {
                scene = EditorSceneManager.OpenScene(SourceSuburbanCityScene, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            // 1. Setup Camera with Clash-of-Clans Touch/Drag Controller
            var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                cam = camObj.GetComponent<Camera>();
            }

            cam.transform.position = new Vector3(0, 32, -32);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);

            var camCtrl = cam.GetComponent<IsometricCameraController>() ?? cam.gameObject.AddComponent<IsometricCameraController>();

            // 2. Ensure EventSystem exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // Remove any old canvas
            var oldCanvas = GameObject.Find("GamePlayCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            // 3. Canvas & UI
            var canvasObj = new GameObject("GamePlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Load Existing Sprites
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePanelBg);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCoin);
            var btnDark = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnDark);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);

            // Top Bar Header
            var headerObj = new GameObject("TopHeader", typeof(RectTransform), typeof(Image));
            headerObj.transform.SetParent(canvasObj.transform, false);
            var headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 90);
            headerRect.anchoredPosition = Vector2.zero;

            var headerImg = headerObj.GetComponent<Image>();
            if (panelSprite != null) { headerImg.sprite = panelSprite; headerImg.type = Image.Type.Sliced; }
            headerImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            // Money Container on Left of Header
            var moneyBoxObj = new GameObject("MoneyBox", typeof(RectTransform));
            moneyBoxObj.transform.SetParent(headerObj.transform, false);
            var moneyBoxRect = moneyBoxObj.GetComponent<RectTransform>();
            moneyBoxRect.anchorMin = new Vector2(0, 0.5f);
            moneyBoxRect.anchorMax = new Vector2(0, 0.5f);
            moneyBoxRect.pivot = new Vector2(0, 0.5f);
            moneyBoxRect.anchoredPosition = new Vector2(30, 0);
            moneyBoxRect.sizeDelta = new Vector2(260, 54);

            var coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinIconObj.transform.SetParent(moneyBoxObj.transform, false);
            var coinRect = coinIconObj.GetComponent<RectTransform>();
            coinRect.anchorMin = new Vector2(0, 0.5f);
            coinRect.anchorMax = new Vector2(0, 0.5f);
            coinRect.pivot = new Vector2(0, 0.5f);
            coinRect.anchoredPosition = Vector2.zero;
            coinRect.sizeDelta = new Vector2(48, 48);
            if (coinSprite != null) coinIconObj.GetComponent<Image>().sprite = coinSprite;

            var moneyText = CreateTMPText("MoneyText", moneyBoxObj.transform, "$10,000", new Vector2(65, 0), new Vector2(190, 48), 30, fontAsset, TextAlignmentOptions.Left);

            // Center Title
            CreateTMPText("SceneTitle", headerObj.transform, "CAPITAL KINGDOM DISTRICT", new Vector2(0, 0), new Vector2(450, 48), 28, fontAsset, TextAlignmentOptions.Center);

            // Back to Menu Button on Right
            var backBtn = CreateCartoonButton("BackToMenuButton", headerObj.transform, "MENU ↩", new Vector2(-100, 0), new Vector2(150, 52), btnDark, fontAsset);

            // GamePlayHeaderUI Controller
            var headerCtrl = headerObj.AddComponent<GamePlayHeaderUI>();
            var serHeader = new SerializedObject(headerCtrl);
            serHeader.FindProperty("coinIcon").objectReferenceValue = coinIconObj.GetComponent<Image>();
            serHeader.FindProperty("moneyText").objectReferenceValue = moneyText;
            serHeader.FindProperty("backToMenuButton").objectReferenceValue = backBtn.GetComponent<Button>();
            serHeader.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            serHeader.ApplyModifiedProperties();

            // Bottom Floating Hint: Touch/Swipe Instructions
            var hintObj = new GameObject("ScrollHint", typeof(RectTransform), typeof(Image));
            hintObj.transform.SetParent(canvasObj.transform, false);
            var hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0);
            hintRect.anchorMax = new Vector2(0.5f, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.anchoredPosition = new Vector2(0, 30);
            hintRect.sizeDelta = new Vector2(580, 54);

            var hintImg = hintObj.GetComponent<Image>();
            if (panelSprite != null) { hintImg.sprite = panelSprite; hintImg.type = Image.Type.Sliced; }
            hintImg.color = new Color(0.1f, 0.12f, 0.18f, 0.88f);

            CreateTMPText("HintText", hintObj.transform, "👆 Swipe/Drag screen to scroll city • Pinch to zoom", new Vector2(0, 0), new Vector2(560, 46), 20, fontAsset, TextAlignmentOptions.Center);

            EditorSceneManager.SaveScene(scene, GamePlayScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void GenerateAssetDefinitions()
        {
            string taxiPath = $"{DefPath}/Asset_Taxi.asset";
            var taxiDef = AssetDatabase.LoadAssetAtPath<AssetDefinition>(taxiPath);
            if (taxiDef == null)
            {
                taxiDef = ScriptableObject.CreateInstance<AssetDefinition>();
                AssetDatabase.CreateAsset(taxiDef, taxiPath);
            }
            taxiDef.assetId = "asset_taxi";
            taxiDef.displayName = "City Cab / Taxi";
            taxiDef.assetType = AssetType.Vehicle;
            taxiDef.description = "Produces daily fares. Subject to mechanical wear.";
            taxiDef.purchasePrice = 5000;
            taxiDef.grossIncomePerDay = 120;
            taxiDef.operatingCostPerDay = 45;
            taxiDef.startingCondition = 100f;
            taxiDef.baseRepairCost = 100;
            taxiDef.dailyConditionDegradation = 3.5f;
            taxiDef.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabTaxi);
            taxiDef.icon = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCoin);
            EditorUtility.SetDirty(taxiDef);

            string housePath = $"{DefPath}/Asset_RentalBuilding.asset";
            var houseDef = AssetDatabase.LoadAssetAtPath<AssetDefinition>(housePath);
            if (houseDef == null)
            {
                houseDef = ScriptableObject.CreateInstance<AssetDefinition>();
                AssetDatabase.CreateAsset(houseDef, housePath);
            }
            houseDef.assetId = "asset_rental_building";
            houseDef.displayName = "Suburban Rental House";
            houseDef.assetType = AssetType.Building;
            houseDef.description = "Stable rental income from tenants. Periodic maintenance needed.";
            houseDef.purchasePrice = 18000;
            houseDef.grossIncomePerDay = 260;
            houseDef.operatingCostPerDay = 110;
            houseDef.startingCondition = 100f;
            houseDef.baseRepairCost = 250;
            houseDef.dailyConditionDegradation = 2.0f;
            houseDef.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabHouse);
            houseDef.icon = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconShop);
            EditorUtility.SetDirty(houseDef);

            string plotPath = $"{DefPath}/Asset_VacantPlot.asset";
            var plotDef = AssetDatabase.LoadAssetAtPath<AssetDefinition>(plotPath);
            if (plotDef == null)
            {
                plotDef = ScriptableObject.CreateInstance<AssetDefinition>();
                AssetDatabase.CreateAsset(plotDef, plotPath);
            }
            plotDef.assetId = "asset_vacant_plot";
            plotDef.displayName = "Commercial Land Plot";
            plotDef.assetType = AssetType.Land;
            plotDef.description = "Vacant real-estate parcel.";
            plotDef.purchasePrice = 8000;
            plotDef.grossIncomePerDay = 0;
            plotDef.operatingCostPerDay = 5;
            plotDef.startingCondition = 100f;
            plotDef.baseRepairCost = 50;
            plotDef.dailyConditionDegradation = 0.5f;
            plotDef.icon = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconTool);
            EditorUtility.SetDirty(plotDef);
        }

        private static void GenerateGameEvents()
        {
            string ev1Path = $"{EventPath}/Event_VehicleWarning.asset";
            var ev1 = AssetDatabase.LoadAssetAtPath<GameEvent>(ev1Path);
            if (ev1 == null)
            {
                ev1 = ScriptableObject.CreateInstance<GameEvent>();
                AssetDatabase.CreateAsset(ev1, ev1Path);
            }
            ev1.eventId = "event_vehicle_warning";
            ev1.title = "Vehicle Engine Warning Light";
            ev1.description = "A warning light illuminates on your taxi fleet. Do you pay for immediate preventive servicing or defer maintenance to save cash?";
            ev1.relevantAssetType = AssetType.Vehicle;
            ev1.choiceA = new EventChoice
            {
                choiceText = "Service Engine ($80)",
                cashImpact = -80,
                conditionImpact = 15,
                outcomeDescription = "Preventive maintenance completed. Breakdown risk averted."
            };
            ev1.choiceB = new EventChoice
            {
                choiceText = "Keep Running ($0)",
                cashImpact = 0,
                conditionImpact = -25,
                outcomeDescription = "Deferred maintenance caused additional mechanical wear."
            };
            EditorUtility.SetDirty(ev1);
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TMP_FontAsset font, TextAlignmentOptions align)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            if (font != null) tmp.font = font;
            return tmp;
        }

        private static GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateCartoonButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, Sprite btnSprite, TMP_FontAsset font)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = obj.GetComponent<Image>();
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
            }

            var textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(obj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (font != null) tmp.font = font;

            return obj;
        }

        [MenuItem("Tools/Empire Builder/4. Make All UI, Buildings & Scripts Static in GamePlay Scene", false, 4)]
        public static void MakeAllStaticInGamePlayScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);

            // 1. Ensure EventSystem exists
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            // 2. Setup Static GameManager GameObject
            var gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                var gmObj = new GameObject("GameManager");
                gameManager = gmObj.AddComponent<GameManager>();
            }

            // Populate catalog definitions if empty
            var defGuids = AssetDatabase.FindAssets("t:AssetDefinition", new[] { DefPath });
            var defs = new List<AssetDefinition>();
            foreach (var g in defGuids)
            {
                var defPath = AssetDatabase.GUIDToAssetPath(g);
                var def = AssetDatabase.LoadAssetAtPath<AssetDefinition>(defPath);
                if (def != null && !defs.Contains(def)) defs.Add(def);
            }
            var serGM = new SerializedObject(gameManager);
            var defProp = serGM.FindProperty("defaultAssetDefinitions");
            if (defProp != null && defProp.arraySize == 0 && defs.Count > 0)
            {
                defProp.arraySize = defs.Count;
                for (int i = 0; i < defs.Count; i++)
                {
                    defProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
                }
                serGM.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(gameManager.gameObject);

            // 3. Setup Canvas and Static UI
            var canvasObj = GameObject.Find("GamePlayCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GamePlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Remove BuildingInteractionManager if it was placed on GamePlayCanvas
            var bimOnCanvas = canvasObj.GetComponent<BuildingInteractionManager>();
            if (bimOnCanvas != null)
            {
                Object.DestroyImmediate(bimOnCanvas);
            }

            // Load UI Assets
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePanelBg);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCoin);
            var btnDark = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnDark);
            var btnGreen = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnGreen);
            var btnBlue = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnBlue);
            var btnOrange = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnOrange);
            var btnRed = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBtnRed);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);
            var ghostMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/characterEffects/shaders/ghostEffect/ghostMaterial.mat");

            // Ensure TopHeader exists
            var headerObj = canvasObj.transform.Find("TopHeader")?.gameObject;
            if (headerObj == null)
            {
                headerObj = new GameObject("TopHeader", typeof(RectTransform), typeof(Image));
                headerObj.transform.SetParent(canvasObj.transform, false);
                var headerRect = headerObj.GetComponent<RectTransform>();
                headerRect.anchorMin = new Vector2(0, 1);
                headerRect.anchorMax = new Vector2(1, 1);
                headerRect.pivot = new Vector2(0.5f, 1);
                headerRect.sizeDelta = new Vector2(0, 90);
                headerRect.anchoredPosition = Vector2.zero;

                var headerImg = headerObj.GetComponent<Image>();
                if (panelSprite != null) { headerImg.sprite = panelSprite; headerImg.type = Image.Type.Sliced; }
                headerImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                var moneyBoxObj = new GameObject("MoneyBox", typeof(RectTransform));
                moneyBoxObj.transform.SetParent(headerObj.transform, false);
                var moneyBoxRect = moneyBoxObj.GetComponent<RectTransform>();
                moneyBoxRect.anchorMin = new Vector2(0, 0.5f);
                moneyBoxRect.anchorMax = new Vector2(0, 0.5f);
                moneyBoxRect.pivot = new Vector2(0, 0.5f);
                moneyBoxRect.anchoredPosition = new Vector2(30, 0);
                moneyBoxRect.sizeDelta = new Vector2(260, 54);

                var coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
                coinIconObj.transform.SetParent(moneyBoxObj.transform, false);
                var coinRect = coinIconObj.GetComponent<RectTransform>();
                coinRect.anchorMin = new Vector2(0, 0.5f);
                coinRect.anchorMax = new Vector2(0, 0.5f);
                coinRect.pivot = new Vector2(0, 0.5f);
                coinRect.anchoredPosition = Vector2.zero;
                coinRect.sizeDelta = new Vector2(48, 48);
                if (coinSprite != null) coinIconObj.GetComponent<Image>().sprite = coinSprite;

                var moneyText = CreateTMPText("MoneyText", moneyBoxObj.transform, "$10,000", new Vector2(65, 0), new Vector2(190, 48), 30, fontAsset, TextAlignmentOptions.Left);
                CreateTMPText("SceneTitle", headerObj.transform, "CAPITAL KINGDOM DISTRICT", new Vector2(0, 0), new Vector2(450, 48), 28, fontAsset, TextAlignmentOptions.Center);
                var backBtn = CreateCartoonButton("BackToMenuButton", headerObj.transform, "MENU ↩", new Vector2(-100, 0), new Vector2(150, 52), btnDark, fontAsset);

                var headerCtrl = headerObj.AddComponent<GamePlayHeaderUI>();
                var serHeader = new SerializedObject(headerCtrl);
                serHeader.FindProperty("coinIcon").objectReferenceValue = coinIconObj.GetComponent<Image>();
                serHeader.FindProperty("moneyText").objectReferenceValue = moneyText;
                serHeader.FindProperty("backToMenuButton").objectReferenceValue = backBtn.GetComponent<Button>();
                serHeader.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
                serHeader.ApplyModifiedProperties();
            }

            // Build Static PropertyDetailModal Hierarchy under GamePlayCanvas
            var oldModal = canvasObj.transform.Find("PropertyDetailModalRoot");
            if (oldModal != null) Object.DestroyImmediate(oldModal.gameObject);
            var oldModal2 = canvasObj.transform.Find("PropertyDetailModal");
            if (oldModal2 != null) Object.DestroyImmediate(oldModal2.gameObject);

            // Create PropertyDetailModal Root
            var modalRoot = CreateUIElement("PropertyDetailModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // 1. Dimmed Overlay Button
            var overlayObj = CreateUIElement("DimmedOverlay", modalRoot.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.50f);
            var overlayBtn = overlayObj.AddComponent<Button>();

            // 2. Central Modal Card
            var cardObj = CreateUIElement("ModalCard", modalRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(530, 680), Vector2.zero);
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(0.10f, 0.13f, 0.20f, 0.98f);
            if (panelSprite != null) { cardImg.sprite = panelSprite; cardImg.type = Image.Type.Sliced; }
            var cardRt = cardObj.GetComponent<RectTransform>();

            // 3. HeaderBar
            var headerBar = CreateUIElement("HeaderBar", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 75), Vector2.zero);

            // Category Badge
            var catBadge = CreateUIElement("CategoryBadge", headerBar.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, 30), new Vector2(25, 10));
            var catBadgeImg = catBadge.AddComponent<Image>();
            catBadgeImg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            if (panelSprite != null) { catBadgeImg.sprite = panelSprite; catBadgeImg.type = Image.Type.Sliced; }
            var catBadgeText = CreateTMPText("Text", catBadge.transform, "CATEGORY", Vector2.zero, Vector2.zero, 14, fontAsset, TextAlignmentOptions.Center);
            catBadgeText.fontStyle = FontStyles.Bold;
            SetStretch(catBadgeText.rectTransform);

            // Status Badge
            var statBadge = CreateUIElement("StatusBadge", headerBar.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 30), new Vector2(175, 10));
            var statBadgeImg = statBadge.AddComponent<Image>();
            statBadgeImg.color = new Color(0.18f, 0.70f, 0.44f, 1f);
            if (panelSprite != null) { statBadgeImg.sprite = panelSprite; statBadgeImg.type = Image.Type.Sliced; }
            var statBadgeText = CreateTMPText("Text", statBadge.transform, "FOR SALE", Vector2.zero, Vector2.zero, 13, fontAsset, TextAlignmentOptions.Center);
            statBadgeText.fontStyle = FontStyles.Bold;
            SetStretch(statBadgeText.rectTransform);

            // Close Button
            var closeBtnObj = CreateUIElement("CloseButton", headerBar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(40, 40), new Vector2(-22, 10));
            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            if (btnDark != null) { closeBtnImg.sprite = btnDark; closeBtnImg.type = Image.Type.Sliced; }
            var closeBtn = closeBtnObj.AddComponent<Button>();
            var closeX = CreateTMPText("X", closeBtnObj.transform, "✕", Vector2.zero, Vector2.zero, 20, fontAsset, TextAlignmentOptions.Center);
            closeX.fontStyle = FontStyles.Bold;
            SetStretch(closeX.rectTransform);

            // 4. Title Text
            var titleText = CreateTMPText("PropertyTitle", cardObj.transform, "Building Title", new Vector2(25, -78), new Vector2(-50, 40), 26, fontAsset, TextAlignmentOptions.Left);
            titleText.fontStyle = FontStyles.Bold;
            titleText.rectTransform.anchorMin = new Vector2(0, 1);
            titleText.rectTransform.anchorMax = new Vector2(1, 1);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1);

            // 5. Description Text
            var descText = CreateTMPText("Description", cardObj.transform, "Building description here...", new Vector2(25, -122), new Vector2(-50, 42), 14, fontAsset, TextAlignmentOptions.TopLeft);
            descText.color = new Color(0.78f, 0.82f, 0.90f);
            descText.rectTransform.anchorMin = new Vector2(0, 1);
            descText.rectTransform.anchorMax = new Vector2(1, 1);
            descText.rectTransform.pivot = new Vector2(0.5f, 1);

            // 6. StatsBox (Purchase Price & Daily Rent)
            var statsBox = CreateUIElement("StatsBox", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 88), new Vector2(0, -172));
            var statsImg = statsBox.AddComponent<Image>();
            statsImg.color = new Color(0.15f, 0.19f, 0.28f, 1f);
            if (panelSprite != null) { statsImg.sprite = panelSprite; statsImg.type = Image.Type.Sliced; }

            // Price column
            var priceLabel = CreateTMPText("PriceLabel", statsBox.transform, "PURCHASE PRICE", Vector2.zero, Vector2.zero, 11, fontAsset, TextAlignmentOptions.Center);
            priceLabel.fontStyle = FontStyles.Bold;
            priceLabel.color = new Color(0.65f, 0.70f, 0.80f);
            priceLabel.rectTransform.anchorMin = new Vector2(0, 0.5f);
            priceLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
            priceLabel.rectTransform.offsetMin = priceLabel.rectTransform.offsetMax = Vector2.zero;

            var priceValueText = CreateTMPText("PriceValue", statsBox.transform, "$25,000", Vector2.zero, Vector2.zero, 24, fontAsset, TextAlignmentOptions.Center);
            priceValueText.fontStyle = FontStyles.Bold;
            priceValueText.color = new Color(1f, 0.85f, 0.25f);
            priceValueText.rectTransform.anchorMin = new Vector2(0, 0);
            priceValueText.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            priceValueText.rectTransform.offsetMin = priceValueText.rectTransform.offsetMax = Vector2.zero;

            // Rent column
            var rentLabel = CreateTMPText("RentLabel", statsBox.transform, "DAILY RENT INCOME", Vector2.zero, Vector2.zero, 11, fontAsset, TextAlignmentOptions.Center);
            rentLabel.fontStyle = FontStyles.Bold;
            rentLabel.color = new Color(0.65f, 0.70f, 0.80f);
            rentLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rentLabel.rectTransform.anchorMax = new Vector2(1, 1);
            rentLabel.rectTransform.offsetMin = rentLabel.rectTransform.offsetMax = Vector2.zero;

            var rentValueText = CreateTMPText("RentValue", statsBox.transform, "+$850 / day", Vector2.zero, Vector2.zero, 24, fontAsset, TextAlignmentOptions.Center);
            rentValueText.fontStyle = FontStyles.Bold;
            rentValueText.color = new Color(0.25f, 0.95f, 0.50f);
            rentValueText.rectTransform.anchorMin = new Vector2(0.5f, 0);
            rentValueText.rectTransform.anchorMax = new Vector2(1, 0.6f);
            rentValueText.rectTransform.offsetMin = rentValueText.rectTransform.offsetMax = Vector2.zero;

            // 7. Taxi Station Special Section (Max 8 taxis)
            var taxiSection = CreateUIElement("TaxiSection", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 160), new Vector2(0, -270));
            var taxiImg = taxiSection.AddComponent<Image>();
            taxiImg.color = new Color(0.20f, 0.18f, 0.12f, 0.95f);
            if (panelSprite != null) { taxiImg.sprite = panelSprite; taxiImg.type = Image.Type.Sliced; }

            var taxiTitle = CreateTMPText("TaxiTitle", taxiSection.transform, "TAXI FLEET MANAGEMENT (MAX 8 TAXIS)", new Vector2(15, -10), new Vector2(-30, 24), 13, fontAsset, TextAlignmentOptions.Left);
            taxiTitle.fontStyle = FontStyles.Bold;
            taxiTitle.color = new Color(1f, 0.82f, 0.20f);
            taxiTitle.rectTransform.anchorMin = new Vector2(0, 1);
            taxiTitle.rectTransform.anchorMax = new Vector2(1, 1);
            taxiTitle.rectTransform.pivot = new Vector2(0, 1);

            var taxiRentalFee = CreateTMPText("RentalFee", taxiSection.transform, "Rental Fee: -$150", new Vector2(15, -34), new Vector2(-15, 20), 12, fontAsset, TextAlignmentOptions.Left);
            taxiRentalFee.color = new Color(0.95f, 0.5f, 0.5f);
            taxiRentalFee.rectTransform.anchorMin = new Vector2(0, 1);
            taxiRentalFee.rectTransform.anchorMax = new Vector2(0.5f, 1);
            taxiRentalFee.rectTransform.pivot = new Vector2(0, 1);

            var taxiFareRevenue = CreateTMPText("FareRevenue", taxiSection.transform, "Random Fares: ~$35 - $110", new Vector2(10, -34), new Vector2(-10, 20), 12, fontAsset, TextAlignmentOptions.Left);
            taxiFareRevenue.color = new Color(0.4f, 0.95f, 0.5f);
            taxiFareRevenue.rectTransform.anchorMin = new Vector2(0.5f, 1);
            taxiFareRevenue.rectTransform.anchorMax = new Vector2(1, 1);
            taxiFareRevenue.rectTransform.pivot = new Vector2(0, 1);

            var taxiNetProfit = CreateTMPText("NetProfit", taxiSection.transform, "Active Fleet: 0 / 8 Taxis on Road", new Vector2(0, -60), new Vector2(-30, 22), 13, fontAsset, TextAlignmentOptions.Center);
            taxiNetProfit.fontStyle = FontStyles.Bold;
            taxiNetProfit.color = new Color(1f, 0.95f, 0.5f);
            taxiNetProfit.rectTransform.anchorMin = new Vector2(0, 1);
            taxiNetProfit.rectTransform.anchorMax = new Vector2(1, 1);
            taxiNetProfit.rectTransform.pivot = new Vector2(0, 1);

            var taxiFleetStatus = CreateTMPText("FleetStatus", taxiSection.transform, "Rented taxis cruise roads for 1 day, earning fares", new Vector2(0, -82), new Vector2(-30, 18), 11, fontAsset, TextAlignmentOptions.Center);
            taxiFleetStatus.color = new Color(0.85f, 0.85f, 0.85f);
            taxiFleetStatus.rectTransform.anchorMin = new Vector2(0, 1);
            taxiFleetStatus.rectTransform.anchorMax = new Vector2(1, 1);
            taxiFleetStatus.rectTransform.pivot = new Vector2(0, 1);

            var rentTaxiBtnObj = CreateUIElement("RentTaxiButton", taxiSection.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            var rentTaxiBtnImg = rentTaxiBtnObj.AddComponent<Image>();
            rentTaxiBtnImg.color = new Color(0.98f, 0.74f, 0.12f, 1f);
            if (btnOrange != null) { rentTaxiBtnImg.sprite = btnOrange; rentTaxiBtnImg.type = Image.Type.Sliced; }
            var rentTaxiBtn = rentTaxiBtnObj.AddComponent<Button>();
            var rentTaxiBtnText = CreateTMPText("Text", rentTaxiBtnObj.transform, "RENT TAXI (-$150) -> ROAD", Vector2.zero, Vector2.zero, 13, fontAsset, TextAlignmentOptions.Center);
            rentTaxiBtnText.fontStyle = FontStyles.Bold;
            rentTaxiBtnText.color = Color.black;
            SetStretch(rentTaxiBtnText.rectTransform);

            // 7b. Flat / Unit Section (for Apartments, Houses, Buildings)
            var flatSection = CreateUIElement("FlatSection", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 160), new Vector2(0, -270));
            var flatImg = flatSection.AddComponent<Image>();
            flatImg.color = new Color(0.12f, 0.20f, 0.28f, 0.95f);
            if (panelSprite != null) { flatImg.sprite = panelSprite; flatImg.type = Image.Type.Sliced; }

            var flatTitle = CreateTMPText("FlatTitle", flatSection.transform, "APARTMENT RESIDENCES & FLATS", new Vector2(15, -10), new Vector2(-30, 24), 13, fontAsset, TextAlignmentOptions.Left);
            flatTitle.fontStyle = FontStyles.Bold;
            flatTitle.color = new Color(0.35f, 0.85f, 1f);
            flatTitle.rectTransform.anchorMin = new Vector2(0, 1);
            flatTitle.rectTransform.anchorMax = new Vector2(1, 1);
            flatTitle.rectTransform.pivot = new Vector2(0, 1);

            var flatsCount = CreateTMPText("FlatsCount", flatSection.transform, "Flats Owned: 0/8 | Leased: 0/0", new Vector2(15, -34), new Vector2(-30, 20), 12, fontAsset, TextAlignmentOptions.Left);
            flatsCount.color = new Color(0.95f, 0.95f, 0.95f);
            flatsCount.rectTransform.anchorMin = new Vector2(0, 1);
            flatsCount.rectTransform.anchorMax = new Vector2(1, 1);
            flatsCount.rectTransform.pivot = new Vector2(0, 1);

            var flatIncome = CreateTMPText("FlatIncome", flatSection.transform, "+$0 / day from leased flats", new Vector2(15, -54), new Vector2(-30, 20), 12, fontAsset, TextAlignmentOptions.Left);
            flatIncome.color = new Color(0.25f, 0.95f, 0.50f);
            flatIncome.fontStyle = FontStyles.Bold;
            flatIncome.rectTransform.anchorMin = new Vector2(0, 1);
            flatIncome.rectTransform.anchorMax = new Vector2(1, 1);
            flatIncome.rectTransform.pivot = new Vector2(0, 1);

            var buyFlatBtnObj = CreateUIElement("BuyFlatButton", flatSection.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(-18, 44), new Vector2(12, 10));
            var buyFlatBtnImg = buyFlatBtnObj.AddComponent<Image>();
            buyFlatBtnImg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            if (btnBlue != null) { buyFlatBtnImg.sprite = btnBlue; buyFlatBtnImg.type = Image.Type.Sliced; }
            var buyFlatBtn = buyFlatBtnObj.AddComponent<Button>();
            var buyFlatBtnText = CreateTMPText("Text", buyFlatBtnObj.transform, "BUY FLAT #1 ($5,500)", Vector2.zero, Vector2.zero, 12, fontAsset, TextAlignmentOptions.Center);
            buyFlatBtnText.fontStyle = FontStyles.Bold;
            SetStretch(buyFlatBtnText.rectTransform);

            var giveRentBtnObj = CreateUIElement("GiveRentButton", flatSection.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 44), new Vector2(-12, 10));
            var giveRentBtnImg = giveRentBtnObj.AddComponent<Image>();
            giveRentBtnImg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (btnGreen != null) { giveRentBtnImg.sprite = btnGreen; giveRentBtnImg.type = Image.Type.Sliced; }
            var giveRentBtn = giveRentBtnObj.AddComponent<Button>();
            var giveRentBtnText = CreateTMPText("Text", giveRentBtnObj.transform, "RENT OUT FLAT", Vector2.zero, Vector2.zero, 12, fontAsset, TextAlignmentOptions.Center);
            giveRentBtnText.fontStyle = FontStyles.Bold;
            SetStretch(giveRentBtnText.rectTransform);

            // 7c. Restaurant / Cafe Dining Section
            var restSection = CreateUIElement("RestaurantSection", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 160), new Vector2(0, -270));
            var restImg = restSection.AddComponent<Image>();
            restImg.color = new Color(0.24f, 0.14f, 0.16f, 0.95f);
            if (panelSprite != null) { restImg.sprite = panelSprite; restImg.type = Image.Type.Sliced; }

            var restTitle = CreateTMPText("RestTitle", restSection.transform, "CAFE & RESTAURANT DINING", new Vector2(15, -10), new Vector2(-30, 24), 13, fontAsset, TextAlignmentOptions.Left);
            restTitle.fontStyle = FontStyles.Bold;
            restTitle.color = new Color(1f, 0.45f, 0.55f);
            restTitle.rectTransform.anchorMin = new Vector2(0, 1);
            restTitle.rectTransform.anchorMax = new Vector2(1, 1);
            restTitle.rectTransform.pivot = new Vector2(0, 1);

            var diningStatus = CreateTMPText("DiningStatus", restSection.transform, "Enjoy espresso or fine dining! Buy restaurant for VIP free dining & daily profit.", new Vector2(15, -34), new Vector2(-30, 36), 11, fontAsset, TextAlignmentOptions.Left);
            diningStatus.color = new Color(0.9f, 0.85f, 0.85f);
            diningStatus.rectTransform.anchorMin = new Vector2(0, 1);
            diningStatus.rectTransform.anchorMax = new Vector2(1, 1);
            diningStatus.rectTransform.pivot = new Vector2(0, 1);

            var drinkBtnObj = CreateUIElement("DrinkButton", restSection.transform, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(-18, 44), new Vector2(12, 10));
            var drinkBtnImg = drinkBtnObj.AddComponent<Image>();
            drinkBtnImg.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            if (btnOrange != null) { drinkBtnImg.sprite = btnOrange; drinkBtnImg.type = Image.Type.Sliced; }
            var drinkBtn = drinkBtnObj.AddComponent<Button>();
            var drinkBtnText = CreateTMPText("Text", drinkBtnObj.transform, "DRINK COFFEE ($12)", Vector2.zero, Vector2.zero, 12, fontAsset, TextAlignmentOptions.Center);
            drinkBtnText.fontStyle = FontStyles.Bold;
            drinkBtnText.color = Color.black;
            SetStretch(drinkBtnText.rectTransform);

            var eatBtnObj = CreateUIElement("EatButton", restSection.transform, new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 44), new Vector2(-12, 10));
            var eatBtnImg = eatBtnObj.AddComponent<Image>();
            eatBtnImg.color = new Color(0.88f, 0.28f, 0.35f, 1f);
            if (btnRed != null) { eatBtnImg.sprite = btnRed; eatBtnImg.type = Image.Type.Sliced; }
            var eatBtn = eatBtnObj.AddComponent<Button>();
            var eatBtnText = CreateTMPText("Text", eatBtnObj.transform, "EAT MEAL ($28)", Vector2.zero, Vector2.zero, 12, fontAsset, TextAlignmentOptions.Center);
            eatBtnText.fontStyle = FontStyles.Bold;
            SetStretch(eatBtnText.rectTransform);

            // 7d. Stadium Section
            var stadiumSection = CreateUIElement("StadiumSection", cardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(-50, 160), new Vector2(0, -270));
            var stadiumImg = stadiumSection.AddComponent<Image>();
            stadiumImg.color = new Color(0.18f, 0.16f, 0.26f, 0.95f);
            if (panelSprite != null) { stadiumImg.sprite = panelSprite; stadiumImg.type = Image.Type.Sliced; }

            var stadiumTitle = CreateTMPText("StadiumTitle", stadiumSection.transform, "FOOTBALL MATCH RENT HOSTING", new Vector2(15, -10), new Vector2(-30, 24), 13, fontAsset, TextAlignmentOptions.Left);
            stadiumTitle.fontStyle = FontStyles.Bold;
            stadiumTitle.color = new Color(0.85f, 0.65f, 1f);
            stadiumTitle.rectTransform.anchorMin = new Vector2(0, 1);
            stadiumTitle.rectTransform.anchorMax = new Vector2(1, 1);
            stadiumTitle.rectTransform.pivot = new Vector2(0, 1);

            var stadiumStats = CreateTMPText("StadiumStats", stadiumSection.transform, "Matches Hosted: 0 | Total Match Revenue: $0", new Vector2(15, -34), new Vector2(-30, 36), 12, fontAsset, TextAlignmentOptions.Left);
            stadiumStats.color = new Color(0.9f, 0.9f, 0.9f);
            stadiumStats.rectTransform.anchorMin = new Vector2(0, 1);
            stadiumStats.rectTransform.anchorMax = new Vector2(1, 1);
            stadiumStats.rectTransform.pivot = new Vector2(0, 1);

            var reqMatchBtnObj = CreateUIElement("RequestMatchButton", stadiumSection.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 44), new Vector2(0, 10));
            var reqMatchBtnImg = reqMatchBtnObj.AddComponent<Image>();
            reqMatchBtnImg.color = new Color(0.92f, 0.42f, 0.18f, 1f);
            if (btnOrange != null) { reqMatchBtnImg.sprite = btnOrange; reqMatchBtnImg.type = Image.Type.Sliced; }
            var reqMatchBtn = reqMatchBtnObj.AddComponent<Button>();
            var reqMatchBtnText = CreateTMPText("Text", reqMatchBtnObj.transform, "REQUEST MATCH RENT OFFER NOW", Vector2.zero, Vector2.zero, 13, fontAsset, TextAlignmentOptions.Center);
            reqMatchBtnText.fontStyle = FontStyles.Bold;
            SetStretch(reqMatchBtnText.rectTransform);

            // 8. Collect Rent Button
            var collectBtnObj = CreateUIElement("CollectRentButton", cardObj.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 50), new Vector2(0, 82));
            var collectBtnImg = collectBtnObj.AddComponent<Image>();
            collectBtnImg.color = new Color(0.18f, 0.65f, 0.92f, 1f);
            if (btnBlue != null) { collectBtnImg.sprite = btnBlue; collectBtnImg.type = Image.Type.Sliced; }
            var collectBtn = collectBtnObj.AddComponent<Button>();
            var collectBtnText = CreateTMPText("Text", collectBtnObj.transform, "COLLECT DAILY RENT", Vector2.zero, Vector2.zero, 15, fontAsset, TextAlignmentOptions.Center);
            collectBtnText.fontStyle = FontStyles.Bold;
            SetStretch(collectBtnText.rectTransform);

            // 9. Buy Property Button
            var buyBtnObj = CreateUIElement("BuyPropertyButton", cardObj.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-50, 54), new Vector2(0, 22));
            var buyBtnImg = buyBtnObj.AddComponent<Image>();
            buyBtnImg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (btnGreen != null) { buyBtnImg.sprite = btnGreen; buyBtnImg.type = Image.Type.Sliced; }
            var buyBtn = buyBtnObj.AddComponent<Button>();
            var buyBtnText = CreateTMPText("Text", buyBtnObj.transform, "BUY PROPERTY ($25,000)", Vector2.zero, Vector2.zero, 16, fontAsset, TextAlignmentOptions.Center);
            buyBtnText.fontStyle = FontStyles.Bold;
            SetStretch(buyBtnText.rectTransform);

            // 10. Feedback Toast
            var toastObj = canvasObj.transform.Find("FeedbackToast")?.gameObject;
            if (toastObj == null)
            {
                toastObj = CreateUIElement("FeedbackToast", canvasObj.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(640, 54), new Vector2(0, -98));
                var toastImg = toastObj.AddComponent<Image>();
                toastImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                if (panelSprite != null) { toastImg.sprite = panelSprite; toastImg.type = Image.Type.Sliced; }
                var toastMsg = CreateTMPText("ToastText", toastObj.transform, "Feedback message here", Vector2.zero, Vector2.zero, 16, fontAsset, TextAlignmentOptions.Center);
                toastMsg.fontStyle = FontStyles.Bold;
                toastMsg.color = new Color(1f, 0.9f, 0.3f);
                SetStretch(toastMsg.rectTransform);
            }
            var toastText = toastObj.GetComponentInChildren<TextMeshProUGUI>();

            // Setup PropertyDetailModalUI Component
            var modalUI = canvasObj.GetComponent<PropertyDetailModalUI>();
            if (modalUI == null) modalUI = canvasObj.AddComponent<PropertyDetailModalUI>();

            var serModal = new SerializedObject(modalUI);
            serModal.FindProperty("panelRoot").objectReferenceValue = modalRoot;
            serModal.FindProperty("modalCardTransform").objectReferenceValue = cardRt;
            serModal.FindProperty("overlayButton").objectReferenceValue = overlayBtn;
            serModal.FindProperty("propertyTitleText").objectReferenceValue = titleText;
            serModal.FindProperty("categoryBadgeText").objectReferenceValue = catBadgeText;
            serModal.FindProperty("categoryBadgeBg").objectReferenceValue = catBadgeImg;
            serModal.FindProperty("statusBadgeText").objectReferenceValue = statBadgeText;
            serModal.FindProperty("statusBadgeBg").objectReferenceValue = statBadgeImg;
            serModal.FindProperty("descriptionText").objectReferenceValue = descText;
            serModal.FindProperty("priceValueText").objectReferenceValue = priceValueText;
            serModal.FindProperty("dailyIncomeValueText").objectReferenceValue = rentValueText;
            serModal.FindProperty("buyPropertyButton").objectReferenceValue = buyBtn;
            serModal.FindProperty("buyButtonText").objectReferenceValue = buyBtnText;
            serModal.FindProperty("buyButtonBg").objectReferenceValue = buyBtnImg;
            serModal.FindProperty("collectRentButton").objectReferenceValue = collectBtn;
            serModal.FindProperty("collectRentButtonText").objectReferenceValue = collectBtnText;
            serModal.FindProperty("closeButton").objectReferenceValue = closeBtn;

            // Wire Special Sections
            serModal.FindProperty("taxiSectionRoot").objectReferenceValue = taxiSection;
            serModal.FindProperty("taxiRentalFeeText").objectReferenceValue = taxiRentalFee;
            serModal.FindProperty("taxiFareRevenueText").objectReferenceValue = taxiFareRevenue;
            serModal.FindProperty("taxiNetProfitText").objectReferenceValue = taxiNetProfit;
            serModal.FindProperty("taxiFleetStatusText").objectReferenceValue = taxiFleetStatus;
            serModal.FindProperty("rentTaxiButton").objectReferenceValue = rentTaxiBtn;
            serModal.FindProperty("rentTaxiButtonText").objectReferenceValue = rentTaxiBtnText;
            serModal.FindProperty("rentTaxiButtonBg").objectReferenceValue = rentTaxiBtnImg;

            serModal.FindProperty("flatSectionRoot").objectReferenceValue = flatSection;
            serModal.FindProperty("flatsCountText").objectReferenceValue = flatsCount;
            serModal.FindProperty("flatIncomeText").objectReferenceValue = flatIncome;
            serModal.FindProperty("buyFlatButton").objectReferenceValue = buyFlatBtn;
            serModal.FindProperty("buyFlatButtonText").objectReferenceValue = buyFlatBtnText;
            serModal.FindProperty("giveRentButton").objectReferenceValue = giveRentBtn;
            serModal.FindProperty("giveRentButtonText").objectReferenceValue = giveRentBtnText;

            serModal.FindProperty("restaurantSectionRoot").objectReferenceValue = restSection;
            serModal.FindProperty("diningStatusText").objectReferenceValue = diningStatus;
            serModal.FindProperty("drinkButton").objectReferenceValue = drinkBtn;
            serModal.FindProperty("drinkButtonText").objectReferenceValue = drinkBtnText;
            serModal.FindProperty("eatButton").objectReferenceValue = eatBtn;
            serModal.FindProperty("eatButtonText").objectReferenceValue = eatBtnText;

            serModal.FindProperty("stadiumSectionRoot").objectReferenceValue = stadiumSection;
            serModal.FindProperty("stadiumStatsText").objectReferenceValue = stadiumStats;
            serModal.FindProperty("requestMatchButton").objectReferenceValue = reqMatchBtn;
            serModal.FindProperty("requestMatchButtonText").objectReferenceValue = reqMatchBtnText;

            serModal.FindProperty("toastRoot").objectReferenceValue = toastObj;
            serModal.FindProperty("toastMessageText").objectReferenceValue = toastText;
            if (panelSprite != null) serModal.FindProperty("roundedBoxSprite").objectReferenceValue = panelSprite;
            serModal.ApplyModifiedProperties();
            EditorUtility.SetDirty(modalUI);

            // 11. Build Static StadiumMatchModal Dialog under GamePlayCanvas
            var oldMatchModal = canvasObj.transform.Find("StadiumMatchModal");
            if (oldMatchModal != null) Object.DestroyImmediate(oldMatchModal.gameObject);

            var matchModalRoot = CreateUIElement("StadiumMatchModal", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var matchOverlayObj = CreateUIElement("DimmedOverlay", matchModalRoot.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var matchOverlayImg = matchOverlayObj.AddComponent<Image>();
            matchOverlayImg.color = new Color(0f, 0f, 0f, 0.65f);
            var matchOverlayBtn = matchOverlayObj.AddComponent<Button>();

            var matchCardObj = CreateUIElement("ModalCard", matchModalRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520, 390), Vector2.zero);
            var matchCardImg = matchCardObj.AddComponent<Image>();
            matchCardImg.color = new Color(0.10f, 0.13f, 0.22f, 0.98f);
            if (panelSprite != null) { matchCardImg.sprite = panelSprite; matchCardImg.type = Image.Type.Sliced; }
            var matchCardRt = matchCardObj.GetComponent<RectTransform>();

            var matchHeaderBar = CreateUIElement("HeaderBar", matchCardObj.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 60), Vector2.zero);
            var matchHeaderTitle = CreateTMPText("HeaderTitle", matchHeaderBar.transform, "STADIUM MATCH RENT REQUEST", new Vector2(25, 0), new Vector2(-100, 36), 18, fontAsset, TextAlignmentOptions.Left);
            matchHeaderTitle.fontStyle = FontStyles.Bold;
            matchHeaderTitle.color = new Color(1f, 0.85f, 0.25f);
            matchHeaderTitle.rectTransform.anchorMin = new Vector2(0, 0.5f);
            matchHeaderTitle.rectTransform.anchorMax = new Vector2(1, 0.5f);
            matchHeaderTitle.rectTransform.pivot = new Vector2(0, 0.5f);

            var matchCloseBtnObj = CreateUIElement("CloseButton", matchHeaderBar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(36, 36), new Vector2(-20, 0));
            var matchCloseBtnImg = matchCloseBtnObj.AddComponent<Image>();
            matchCloseBtnImg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            if (btnDark != null) { matchCloseBtnImg.sprite = btnDark; matchCloseBtnImg.type = Image.Type.Sliced; }
            var matchCloseBtn = matchCloseBtnObj.AddComponent<Button>();
            var matchCloseX = CreateTMPText("X", matchCloseBtnObj.transform, "X", Vector2.zero, Vector2.zero, 18, fontAsset, TextAlignmentOptions.Center);
            matchCloseX.fontStyle = FontStyles.Bold;
            SetStretch(matchCloseX.rectTransform);

            var matchNameText = CreateTMPText("MatchName", matchCardObj.transform, "Premier League Derby Match", new Vector2(25, -70), new Vector2(-50, 38), 22, fontAsset, TextAlignmentOptions.Center);
            matchNameText.fontStyle = FontStyles.Bold;
            matchNameText.color = Color.white;
            matchNameText.rectTransform.anchorMin = new Vector2(0, 1);
            matchNameText.rectTransform.anchorMax = new Vector2(1, 1);
            matchNameText.rectTransform.pivot = new Vector2(0.5f, 1);

            var matchRentOfferText = CreateTMPText("RentOffer", matchCardObj.transform, "+$18,500 MATCH RENT", new Vector2(25, -112), new Vector2(-50, 48), 30, fontAsset, TextAlignmentOptions.Center);
            matchRentOfferText.fontStyle = FontStyles.Bold;
            matchRentOfferText.color = new Color(0.25f, 0.95f, 0.50f);
            matchRentOfferText.rectTransform.anchorMin = new Vector2(0, 1);
            matchRentOfferText.rectTransform.anchorMax = new Vector2(1, 1);
            matchRentOfferText.rectTransform.pivot = new Vector2(0.5f, 1);

            var matchDescText = CreateTMPText("Description", matchCardObj.transform, "A prestigious football tournament requests to rent your Grand Olympic Stadium. Accept to host the match and receive the full rent revenue immediately!", new Vector2(30, -170), new Vector2(-60, 60), 13, fontAsset, TextAlignmentOptions.Center);
            matchDescText.color = new Color(0.80f, 0.85f, 0.92f);
            matchDescText.rectTransform.anchorMin = new Vector2(0, 1);
            matchDescText.rectTransform.anchorMax = new Vector2(1, 1);
            matchDescText.rectTransform.pivot = new Vector2(0.5f, 1);

            var acceptBtnObj = CreateUIElement("AcceptButton", matchCardObj.transform, new Vector2(0, 0), new Vector2(0.65f, 0), new Vector2(0, 0), new Vector2(-20, 52), new Vector2(20, 20));
            var acceptBtnImg = acceptBtnObj.AddComponent<Image>();
            acceptBtnImg.color = new Color(0.12f, 0.78f, 0.38f, 1f);
            if (btnGreen != null) { acceptBtnImg.sprite = btnGreen; acceptBtnImg.type = Image.Type.Sliced; }
            var acceptBtn = acceptBtnObj.AddComponent<Button>();
            var acceptBtnText = CreateTMPText("Text", acceptBtnObj.transform, "ACCEPT MATCH RENT", Vector2.zero, Vector2.zero, 15, fontAsset, TextAlignmentOptions.Center);
            acceptBtnText.fontStyle = FontStyles.Bold;
            SetStretch(acceptBtnText.rectTransform);

            var declineBtnObj = CreateUIElement("DeclineButton", matchCardObj.transform, new Vector2(0.65f, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 52), new Vector2(-20, 20));
            var declineBtnImg = declineBtnObj.AddComponent<Image>();
            declineBtnImg.color = new Color(0.85f, 0.25f, 0.25f, 0.95f);
            if (btnRed != null) { declineBtnImg.sprite = btnRed; declineBtnImg.type = Image.Type.Sliced; }
            var declineBtn = declineBtnObj.AddComponent<Button>();
            var declineBtnText = CreateTMPText("Text", declineBtnObj.transform, "DECLINE", Vector2.zero, Vector2.zero, 15, fontAsset, TextAlignmentOptions.Center);
            declineBtnText.fontStyle = FontStyles.Bold;
            SetStretch(declineBtnText.rectTransform);

            matchModalRoot.SetActive(false);

            // Setup StadiumMatchRequestModalUI Component on GamePlayCanvas
            var matchModalUI = canvasObj.GetComponent<StadiumMatchRequestModalUI>();
            if (matchModalUI == null) matchModalUI = canvasObj.AddComponent<StadiumMatchRequestModalUI>();

            var serMatchUI = new SerializedObject(matchModalUI);
            serMatchUI.FindProperty("panelRoot").objectReferenceValue = matchModalRoot;
            serMatchUI.FindProperty("cardTransform").objectReferenceValue = matchCardRt;
            serMatchUI.FindProperty("overlayButton").objectReferenceValue = matchOverlayBtn;
            serMatchUI.FindProperty("headerTitleText").objectReferenceValue = matchHeaderTitle;
            serMatchUI.FindProperty("matchNameText").objectReferenceValue = matchNameText;
            serMatchUI.FindProperty("rentOfferAmountText").objectReferenceValue = matchRentOfferText;
            serMatchUI.FindProperty("descriptionText").objectReferenceValue = matchDescText;
            serMatchUI.FindProperty("acceptButton").objectReferenceValue = acceptBtn;
            serMatchUI.FindProperty("acceptButtonText").objectReferenceValue = acceptBtnText;
            serMatchUI.FindProperty("declineButton").objectReferenceValue = declineBtn;
            serMatchUI.FindProperty("declineButtonText").objectReferenceValue = declineBtnText;
            serMatchUI.FindProperty("closeButton").objectReferenceValue = matchCloseBtn;
            serMatchUI.ApplyModifiedProperties();
            EditorUtility.SetDirty(matchModalUI);

            // 4. Setup Dedicated BuildingInteractionManager GameObject
            var bimObj = GameObject.Find("BuildingInteractionManager");
            if (bimObj == null)
            {
                bimObj = new GameObject("BuildingInteractionManager");
            }
            var bim = bimObj.GetComponent<BuildingInteractionManager>();
            if (bim == null) bim = bimObj.AddComponent<BuildingInteractionManager>();

            var serBIM = new SerializedObject(bim);
            var mainCam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            if (mainCam != null) serBIM.FindProperty("targetCamera").objectReferenceValue = mainCam;
            if (ghostMat != null) serBIM.FindProperty("defaultSelectionMaterial").objectReferenceValue = ghostMat;
            serBIM.FindProperty("detailModalUI").objectReferenceValue = modalUI;
            serBIM.ApplyModifiedProperties();

            // Setup StadiumMatchManager on BuildingInteractionManager
            var smm = bimObj.GetComponent<StadiumMatchManager>();
            if (smm == null) smm = bimObj.AddComponent<StadiumMatchManager>();
            smm.FindStadiumInScene();
            EditorUtility.SetDirty(bimObj);

            // 5. Statically Attach and Configure PropertyBuilding to Every Building in the Scene
            string[] sectorNames = new string[]
            {
                "Buildings", "Hotel Zone", "Demonstration", "Demonstration (1)",
                "Demonstration (2)", "Demonstration (3)", "RoadNetwork", "AI Waypoints for character", "Terrain"
            };
            foreach (var sName in sectorNames)
            {
                GameObject sObj = GameObject.Find(sName);
                if (sObj != null)
                {
                    var pb = sObj.GetComponent<PropertyBuilding>();
                    if (pb != null) UnityEngine.Object.DestroyImmediate(pb);
                    var bc = sObj.GetComponent<BoxCollider>();
                    if (bc != null) UnityEngine.Object.DestroyImmediate(bc);
                    EditorUtility.SetDirty(sObj);
                }
            }

            int buildingsConfigured = 0;
            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (BuildingInteractionManager.IsBuildingObject(t, out string friendlyName, out PropertyType pType, out double price, out double dailyRent, out double taxiFee, out double taxiFare, out string desc))
                {
                    var pb = t.GetComponent<PropertyBuilding>();
                    if (pb == null)
                    {
                        pb = t.gameObject.AddComponent<PropertyBuilding>();
                    }
                    pb.SetConfig(friendlyName, pType, price, dailyRent, desc);
                    if (pType == PropertyType.TaxiStation)
                    {
                        pb.SetTaxiConfig(taxiFee, taxiFare);
                        pb.EnsureTaxiStationFleet();
                    }
                    if (ghostMat != null && pb.SelectionShaderMaterial == null)
                    {
                        pb.SetSelectionMaterial(ghostMat);
                    }
                    pb.EnsureCollider();
                    pb.EnsureWorldIndicator();
                    EditorUtility.SetDirty(t.gameObject);
                    buildingsConfigured++;
                }
            }
            Debug.Log($"[EmpireBuilderSetupTool] Successfully attached PropertyBuilding to {buildingsConfigured} static buildings in GamePlay scene.");

            // 6. Setup Static AI Pedestrians in Scene
            SetupAIPedestriansInScene();

            // 7. Setup Static Health Bar & Player Health System
            SetupPlayerHealthSystemInScene();

            // 8. Setup Static Building Animated World Indicators
            SetupBuildingWorldIndicatorsInScene();

            // 9. Setup Static 3D Sound System in GamePlay Scene
            SetupSoundSystemInScene();

            // 10. Save Scene & Assets
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EmpireBuilderSetupTool] ALL UI, BUILDINGS, MANAGERS & SCRIPTS ARE NOW STATIC IN GAMEPLAY SCENE!");
        }

        [MenuItem("Tools/Empire Builder/5. Setup Health Bar & Player Health System in GamePlay Scene", false, 5)]
        public static void SetupPlayerHealthSystemInScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);

            // 1. Ensure PlayerHealthManager GameObject exists at Scene Root
            var healthMgrObj = GameObject.Find("PlayerHealthManager");
            if (healthMgrObj == null)
            {
                healthMgrObj = new GameObject("PlayerHealthManager");
            }
            var healthMgr = healthMgrObj.GetComponent<PlayerHealthManager>();
            if (healthMgr == null) healthMgr = healthMgrObj.AddComponent<PlayerHealthManager>();

            var serHealth = new SerializedObject(healthMgr);
            serHealth.FindProperty("maxHealth").floatValue = 100f;
            serHealth.FindProperty("currentHealth").floatValue = 100f;
            serHealth.FindProperty("decayRatePerSecond").floatValue = 0.4f;
            serHealth.FindProperty("decayActive").boolValue = true;
            serHealth.FindProperty("foodHealthRestore").floatValue = 45f;
            serHealth.FindProperty("drinkHealthRestore").floatValue = 25f;
            serHealth.FindProperty("lowHealthWarningThreshold").floatValue = 30f;
            serHealth.FindProperty("criticalHealthThreshold").floatValue = 15f;
            serHealth.FindProperty("warningRepeatInterval").floatValue = 16f;
            serHealth.ApplyModifiedProperties();
            EditorUtility.SetDirty(healthMgrObj);

            // 2. Ensure HealthBarBox exists inside TopHeader in GamePlayCanvas
            var canvasObj = GameObject.Find("GamePlayCanvas");
            if (canvasObj == null)
            {
                Debug.LogError("[EmpireBuilderSetupTool] GamePlayCanvas not found!");
                return;
            }
            var headerTr = canvasObj.transform.Find("TopHeader");
            if (headerTr == null)
            {
                Debug.LogError("[EmpireBuilderSetupTool] TopHeader not found!");
                return;
            }

            var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconHeart);
            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoundedRect);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);

            // Remove previous HealthBox if re-generating
            var existingHb = headerTr.Find("HealthBox");
            if (existingHb != null)
            {
                UnityEngine.Object.DestroyImmediate(existingHb.gameObject);
            }

            var healthBoxObj = new GameObject("HealthBox", typeof(RectTransform), typeof(Image));
            healthBoxObj.transform.SetParent(headerTr, false);
            var hbRect = healthBoxObj.GetComponent<RectTransform>();
            hbRect.anchorMin = new Vector2(0, 0.5f);
            hbRect.anchorMax = new Vector2(0, 0.5f);
            hbRect.pivot = new Vector2(0, 0.5f);
            hbRect.anchoredPosition = new Vector2(285, 0);
            hbRect.sizeDelta = new Vector2(235, 50);

            var hbImg = healthBoxObj.GetComponent<Image>();
            if (roundedSprite != null) { hbImg.sprite = roundedSprite; hbImg.type = Image.Type.Sliced; }
            hbImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

            // Heart Icon (Left)
            var heartObj = new GameObject("HeartIcon", typeof(RectTransform), typeof(Image));
            heartObj.transform.SetParent(healthBoxObj.transform, false);
            var heartRect = heartObj.GetComponent<RectTransform>();
            heartRect.anchorMin = new Vector2(0, 0.5f);
            heartRect.anchorMax = new Vector2(0, 0.5f);
            heartRect.pivot = new Vector2(0, 0.5f);
            heartRect.anchoredPosition = new Vector2(8, 0);
            heartRect.sizeDelta = new Vector2(36, 36);
            var heartImg = heartObj.GetComponent<Image>();
            if (heartSprite != null) heartImg.sprite = heartSprite;
            heartImg.preserveAspect = true;

            // Health Track (Background)
            var trackObj = new GameObject("HealthTrack", typeof(RectTransform), typeof(Image));
            trackObj.transform.SetParent(healthBoxObj.transform, false);
            var trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0, 0.5f);
            trackRect.anchorMax = new Vector2(1, 0.5f);
            trackRect.pivot = new Vector2(0, 0.5f);
            trackRect.anchoredPosition = new Vector2(50, 0);
            trackRect.sizeDelta = new Vector2(-58, 28);
            var trackImg = trackObj.GetComponent<Image>();
            if (roundedSprite != null) { trackImg.sprite = roundedSprite; trackImg.type = Image.Type.Sliced; }
            trackImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);

            // Health Fill (Foreground Filled Image)
            var fillObj = new GameObject("HealthFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(trackObj.transform, false);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            var fillImg = fillObj.GetComponent<Image>();
            if (roundedSprite != null) { fillImg.sprite = roundedSprite; fillImg.type = Image.Type.Sliced; }
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 1f;
            fillImg.color = new Color(0.18f, 0.82f, 0.44f);

            // Health Text (Inside HealthTrack)
            var healthText = CreateTMPText("HealthText", trackObj.transform, "100/100 HP", Vector2.zero, Vector2.zero, 15, fontAsset, TextAlignmentOptions.Center);
            healthText.fontStyle = FontStyles.Bold;
            healthText.color = Color.white;
            SetStretch(healthText.rectTransform);

            // Warning Badge
            var warningObj = new GameObject("WarningBadge", typeof(RectTransform), typeof(Image));
            warningObj.transform.SetParent(healthBoxObj.transform, false);
            var warningRect = warningObj.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(1, 1);
            warningRect.anchorMax = new Vector2(1, 1);
            warningRect.pivot = new Vector2(0.5f, 0.5f);
            warningRect.anchoredPosition = new Vector2(-4, -4);
            warningRect.sizeDelta = new Vector2(22, 22);
            var warningImg = warningObj.GetComponent<Image>();
            if (roundedSprite != null) { warningImg.sprite = roundedSprite; warningImg.type = Image.Type.Sliced; }
            warningImg.color = new Color(0.92f, 0.25f, 0.25f);
            var warningText = CreateTMPText("Exclamation", warningObj.transform, "!", Vector2.zero, Vector2.zero, 13, fontAsset, TextAlignmentOptions.Center);
            warningText.fontStyle = FontStyles.Bold;
            warningText.color = Color.white;
            SetStretch(warningText.rectTransform);
            warningObj.SetActive(false);

            // Attach PlayerHealthBarUI
            var healthBarUI = healthBoxObj.AddComponent<PlayerHealthBarUI>();
            var serUI = new SerializedObject(healthBarUI);
            serUI.FindProperty("healthFillImage").objectReferenceValue = fillImg;
            serUI.FindProperty("healthTrackImage").objectReferenceValue = trackImg;
            serUI.FindProperty("healthText").objectReferenceValue = healthText;
            serUI.FindProperty("heartIconImage").objectReferenceValue = heartImg;
            serUI.FindProperty("warningBadge").objectReferenceValue = warningObj;
            serUI.FindProperty("warningBadgeText").objectReferenceValue = warningText;
            serUI.ApplyModifiedProperties();

            EditorUtility.SetDirty(healthBoxObj);

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[EmpireBuilderSetupTool] PlayerHealthManager and HealthBarBox successfully setup statically in GamePlay scene!");
        }

        [MenuItem("Tools/Empire Builder/6. Setup Building Animated World Indicators in GamePlay Scene", false, 6)]
        public static void SetupBuildingWorldIndicatorsInScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);

            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png");
            var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Cup.png");
            var crownSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Crown.png");
            var keySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Lock - Key/Key - Gold.png");
            var shopSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Commerce/Shop.png");
            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Stars/Star Yellow.png");
            var checkmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Basic/Checkmark.png");
            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png");

            var allBuildings = Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int indicatorsConfigured = 0;

            foreach (var pb in allBuildings)
            {
                if (pb == null) continue;
                pb.EnsureWorldIndicator();
                var ind = pb.WorldIndicator;
                if (ind != null)
                {
                    ind.EnsureUIHierarchy();
                    var serInd = new SerializedObject(ind);
                    if (coinSprite != null) serInd.FindProperty("coinSprite").objectReferenceValue = coinSprite;
                    if (trophySprite != null) serInd.FindProperty("trophySprite").objectReferenceValue = trophySprite;
                    if (crownSprite != null) serInd.FindProperty("crownSprite").objectReferenceValue = crownSprite;
                    if (keySprite != null) serInd.FindProperty("keySprite").objectReferenceValue = keySprite;
                    if (shopSprite != null) serInd.FindProperty("shopSprite").objectReferenceValue = shopSprite;
                    if (starSprite != null) serInd.FindProperty("starSprite").objectReferenceValue = starSprite;
                    if (checkmarkSprite != null) serInd.FindProperty("checkmarkSprite").objectReferenceValue = checkmarkSprite;
                    if (roundedSprite != null) serInd.FindProperty("backgroundRoundedSprite").objectReferenceValue = roundedSprite;
                    serInd.ApplyModifiedProperties();

                    ind.RefreshVisuals();
                    EditorUtility.SetDirty(ind.gameObject);
                    indicatorsConfigured++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EmpireBuilderSetupTool] Configured animated world indicators for {indicatorsConfigured} buildings in GamePlay scene.");
        }

        [MenuItem("Tools/Empire Builder/7. Setup 3D Sound System in GamePlay Scene", false, 7)]
        public static void SetupSoundSystemInScene()
        {
            var scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);

            // 1. Ensure SoundManager GameObject exists at Scene Root
            var soundMgrObj = GameObject.Find("SoundManager");
            if (soundMgrObj == null)
            {
                soundMgrObj = new GameObject("SoundManager");
            }
            soundMgrObj.transform.position = Vector3.zero;

            var soundMgr = soundMgrObj.GetComponent<GameSoundManager>();
            if (soundMgr == null)
            {
                soundMgr = soundMgrObj.AddComponent<GameSoundManager>();
            }

            soundMgr.EnsureAudioHierarchy();
            soundMgr.AutoLocateWorldEmitters();

            EditorUtility.SetDirty(soundMgrObj);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[EmpireBuilderSetupTool] 3D Sound System (SoundManager) successfully setup statically in GamePlay scene with all 3D emitters and empty audio slots!");
        }
    }
}
#endif
