#if UNITY_EDITOR
using System.Collections.Generic;
using EmpireBuilder.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.Editor
{
    /// <summary>
    /// Editor utility that permanently builds static BuildingWorldIndicator GameObject hierarchies
    /// under every building in the GamePlay scene hierarchy.
    /// This allows developers and level designers to select, move, scale, recolor,
    /// or modify any building's indicator directly in the Scene View and Hierarchy panel.
    /// </summary>
    [InitializeOnLoad]
    public static class StaticBuildingIndicatorBuilder
    {
        private const string GamePlayScenePath = "Assets/Scenes/GamePlay.unity";

        private const string SpriteIconCoin = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png";
        private const string SpriteIconTrophy = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Cup.png";
        private const string SpriteIconCrown = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Crown.png";
        private const string SpriteIconKey = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Lock - Key/Key - Gold.png";
        private const string SpriteIconShop = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Commerce/Shop.png";
        private const string SpriteIconStar = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Stars/Star Yellow.png";
        private const string SpriteIconCheckmark = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Basic/Checkmark.png";
        private const string SpriteRoundedRect = "Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png";
        private const string FontLilita = "Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset";

        static StaticBuildingIndicatorBuilder()
        {
            EditorApplication.update += CheckAndBuildOnUpdate;
        }

        private static void CheckAndBuildOnUpdate()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;
            if (!scene.name.Equals("GamePlay", System.StringComparison.OrdinalIgnoreCase)) return;

            // Verify if static indicators already exist in hierarchy
            var existing = Object.FindObjectsByType<BuildingWorldIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing != null && existing.Length >= 10)
            {
                // Already built and present in hierarchy!
                EditorApplication.update -= CheckAndBuildOnUpdate;
                return;
            }

            // Build them once automatically
            EditorApplication.update -= CheckAndBuildOnUpdate;
            Debug.Log("[StaticBuildingIndicatorBuilder] Automatically generating static building indicators in scene hierarchy...");
            BuildAllStaticIndicators(false);
        }

        [MenuItem("Tools/Empire Builder/Build All Static Building Indicators in Hierarchy", false, 6)]
        public static void BuildAllStaticIndicatorsMenu()
        {
            BuildAllStaticIndicators(false);
        }

        [MenuItem("Tools/Empire Builder/Snap All Static Indicators to Rooftops", false, 7)]
        public static void SnapAllIndicatorsToRooftopsMenu()
        {
            BuildAllStaticIndicators(true);
        }

        [MenuItem("Tools/Empire Builder/Select All Building Indicators", false, 8)]
        public static void SelectAllIndicatorsMenu()
        {
            var indicators = Object.FindObjectsByType<BuildingWorldIndicator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (indicators != null && indicators.Length > 0)
            {
                var objs = new GameObject[indicators.Length];
                for (int i = 0; i < indicators.Length; i++) objs[i] = indicators[i].gameObject;
                Selection.objects = objs;
                Debug.Log($"[StaticBuildingIndicatorBuilder] Selected {indicators.Length} static indicators in hierarchy.");
            }
        }

        public static void BuildAllStaticIndicators(bool snapRooftops)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.name.Equals("GamePlay", System.StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene(GamePlayScenePath, OpenSceneMode.Single);
            }

            // Load UI Assets
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCoin);
            var trophySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconTrophy);
            var crownSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCrown);
            var keySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconKey);
            var shopSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconShop);
            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconStar);
            var checkmarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteIconCheckmark);
            var roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoundedRect);
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontLilita);
            var ghostMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/characterEffects/shaders/ghostEffect/ghostMaterial.mat");

            // 1. First ensure all tagged/recognized buildings have PropertyBuilding configured
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var buildings = new List<PropertyBuilding>();

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
                    EditorUtility.SetDirty(t.gameObject);

                    if (!buildings.Contains(pb)) buildings.Add(pb);
                }
            }

            int configuredCount = 0;

            // 2. Build or wire static child hierarchy under each building
            foreach (var pb in buildings)
            {
                if (pb == null) continue;

                // Look for existing child named "BuildingWorldIndicator"
                Transform indTr = pb.transform.Find("BuildingWorldIndicator");
                GameObject indObj;

                if (indTr != null)
                {
                    indObj = indTr.gameObject;
                }
                else
                {
                    indObj = new GameObject("BuildingWorldIndicator");
                    indObj.transform.SetParent(pb.transform, false);
                    indObj.transform.localPosition = pb.GetRoofPosition();
                    Undo.RegisterCreatedObjectUndo(indObj, "Create Static Building Indicator");
                }

                if (snapRooftops)
                {
                    indObj.transform.localPosition = pb.GetRoofPosition();
                }

                var ind = indObj.GetComponent<BuildingWorldIndicator>();
                if (ind == null)
                {
                    ind = indObj.AddComponent<BuildingWorldIndicator>();
                }

                // Child 1: WorldCanvas
                Transform canvasTr = indObj.transform.Find("WorldCanvas");
                GameObject canvasObj;
                if (canvasTr != null)
                {
                    canvasObj = canvasTr.gameObject;
                }
                else
                {
                    canvasObj = new GameObject("WorldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                    canvasObj.transform.SetParent(indObj.transform, false);
                    canvasObj.transform.localPosition = Vector3.zero;
                }

                var canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 300;

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10;

                // Child 2: BadgeRoot under WorldCanvas
                Transform badgeTr = canvasObj.transform.Find("BadgeRoot");
                GameObject badgeObj;
                if (badgeTr != null)
                {
                    badgeObj = badgeTr.gameObject;
                }
                else
                {
                    badgeObj = new GameObject("BadgeRoot", typeof(RectTransform), typeof(Image));
                    badgeObj.transform.SetParent(canvasObj.transform, false);
                }

                var badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.sizeDelta = new Vector2(250f, 52f);
                badgeRt.localScale = new Vector3(0.024f, 0.024f, 0.024f);
                badgeRt.localPosition = Vector3.zero;

                var bgImg = badgeObj.GetComponent<Image>();
                if (roundedSprite != null)
                {
                    bgImg.sprite = roundedSprite;
                    bgImg.type = Image.Type.Sliced;
                }
                bgImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

                // BoxCollider on indObj for raycasts
                var collider = indObj.GetComponent<BoxCollider>();
                if (collider == null) collider = indObj.AddComponent<BoxCollider>();
                collider.size = new Vector3(6.5f, 2.2f, 1.2f);
                collider.center = Vector3.zero;

                // Child 3: Icon under BadgeRoot
                Transform iconTr = badgeObj.transform.Find("Icon");
                GameObject iconObj;
                if (iconTr != null)
                {
                    iconObj = iconTr.gameObject;
                }
                else
                {
                    iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(badgeObj.transform, false);
                }

                var iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(26f, 0f);
                iconRt.sizeDelta = new Vector2(36f, 36f);

                var iconImg = iconObj.GetComponent<Image>();
                iconImg.preserveAspect = true;
                if (coinSprite != null && iconImg.sprite == null) iconImg.sprite = coinSprite;

                // Child 4: StatusText under BadgeRoot
                Transform textTr = badgeObj.transform.Find("StatusText");
                GameObject textObj;
                if (textTr != null)
                {
                    textObj = textTr.gameObject;
                }
                else
                {
                    textObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textObj.transform.SetParent(badgeObj.transform, false);
                }

                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = new Vector2(18f, 0f);
                textRt.sizeDelta = new Vector2(-48f, 0f);

                var statusText = textObj.GetComponent<TextMeshProUGUI>();
                if (fontAsset != null) statusText.font = fontAsset;
                statusText.fontSize = 15;
                statusText.fontSizeMin = 10;
                statusText.fontSizeMax = 17;
                statusText.enableAutoSizing = true;
                statusText.fontStyle = FontStyles.Bold;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = Color.white;
                statusText.text = pb.PropertyName;

                // 3. Serialize component properties
                var serInd = new SerializedObject(ind);
                serInd.FindProperty("targetBuilding").objectReferenceValue = pb;
                serInd.FindProperty("worldCanvas").objectReferenceValue = canvas;
                serInd.FindProperty("badgeTransform").objectReferenceValue = badgeRt;
                serInd.FindProperty("backgroundImage").objectReferenceValue = bgImg;
                serInd.FindProperty("iconImage").objectReferenceValue = iconImg;
                serInd.FindProperty("iconTransform").objectReferenceValue = iconRt;
                serInd.FindProperty("statusText").objectReferenceValue = statusText;
                serInd.FindProperty("clickCollider").objectReferenceValue = collider;
                serInd.FindProperty("useManualPosition").boolValue = true;

                if (coinSprite != null) serInd.FindProperty("coinSprite").objectReferenceValue = coinSprite;
                if (trophySprite != null) serInd.FindProperty("trophySprite").objectReferenceValue = trophySprite;
                if (crownSprite != null) serInd.FindProperty("crownSprite").objectReferenceValue = crownSprite;
                if (keySprite != null) serInd.FindProperty("keySprite").objectReferenceValue = keySprite;
                if (shopSprite != null) serInd.FindProperty("shopSprite").objectReferenceValue = shopSprite;
                if (starSprite != null) serInd.FindProperty("starSprite").objectReferenceValue = starSprite;
                if (checkmarkSprite != null) serInd.FindProperty("checkmarkSprite").objectReferenceValue = checkmarkSprite;
                if (roundedSprite != null) serInd.FindProperty("backgroundRoundedSprite").objectReferenceValue = roundedSprite;
                serInd.ApplyModifiedProperties();

                // Wire back to PropertyBuilding
                var serPb = new SerializedObject(pb);
                serPb.FindProperty("worldIndicator").objectReferenceValue = ind;
                serPb.ApplyModifiedProperties();

                ind.RefreshVisuals();

                EditorUtility.SetDirty(textObj);
                EditorUtility.SetDirty(iconObj);
                EditorUtility.SetDirty(badgeObj);
                EditorUtility.SetDirty(canvasObj);
                EditorUtility.SetDirty(indObj);
                EditorUtility.SetDirty(pb.gameObject);

                configuredCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StaticBuildingIndicatorBuilder] Successfully configured and saved static indicators for {configuredCount} buildings in '{scene.name}' scene hierarchy!");
        }
    }
}
#endif
