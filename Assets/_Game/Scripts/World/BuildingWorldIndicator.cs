using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Overhead animated 3D/world-space indicator for city buildings.
    /// Features:
    /// - Distinctive cartoon icons and bold text indicating collectible revenues (Rent, Match Fees, Taxi Fares).
    /// - Displays ownership status ("MY RESTAURANT", "MY CAFE", "MY STADIUM", "OWNED") or flat counts ("FLATS: 3/8").
    /// - Floating bobbing animation & billboarding to face the active camera.
    /// - Energetic bouncing/pulse animation whenever collectibles are available.
    /// - Clickable world collider so clicking the badge directly selects the building.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingWorldIndicator : MonoBehaviour
    {
        [Header("Target Building Reference")]
        [SerializeField] private PropertyBuilding targetBuilding;

        [Header("UI Element References")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private RectTransform badgeTransform;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform iconTransform;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private BoxCollider clickCollider;

        [Header("Sprite Library")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite trophySprite;
        [SerializeField] private Sprite crownSprite;
        [SerializeField] private Sprite keySprite;
        [SerializeField] private Sprite shopSprite;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite checkmarkSprite;
        [SerializeField] private Sprite backgroundRoundedSprite;

        [Header("Animation Settings")]
        [SerializeField] private bool billboardToCamera = true;
        [SerializeField] private bool animateFloatingBob = true;
        [SerializeField] private float bobSpeed = 3.2f;
        [SerializeField] private float bobHeight = 0.28f;
        [SerializeField] private float bounceSpeed = 4.5f;
        [SerializeField] private float bounceMagnitude = 0.16f;

        [Header("Display Configuration")]
        [SerializeField] private bool showUnownedIndicators = true;
        [SerializeField] private Vector3 badgeBaseScale = new Vector3(0.024f, 0.024f, 0.024f);

        // Runtime state
        private Camera cachedCamera;
        private bool isCollectibleReady = false;
        private float seedOffset = 0f;
        private float periodicCheckTimer = 0f;

        public PropertyBuilding TargetBuilding => targetBuilding;

        private void Awake()
        {
            if (targetBuilding == null)
            {
                targetBuilding = GetComponentInParent<PropertyBuilding>();
            }

            // Generate organic phase offset based on world position
            seedOffset = (transform.position.x * 1.73f + transform.position.z * 2.37f) % 6.28f;
            EnsureUIHierarchy();
        }

        private void Start()
        {
            if (targetBuilding != null)
            {
                targetBuilding.OnStateChanged += HandleBuildingStateChanged;
            }

            StadiumMatchManager.OnMatchRequestStateChanged += HandleMatchRequestChanged;

            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (targetBuilding != null)
            {
                targetBuilding.OnStateChanged -= HandleBuildingStateChanged;
            }

            StadiumMatchManager.OnMatchRequestStateChanged -= HandleMatchRequestChanged;
        }

        private void LateUpdate()
        {
            // 1. Billboard to camera
            if (billboardToCamera)
            {
                if (cachedCamera == null) cachedCamera = Camera.main;
                if (cachedCamera != null)
                {
                    Transform camT = cachedCamera.transform;
                    transform.rotation = Quaternion.LookRotation(camT.forward, camT.up);
                }
            }

            // 2. Animated Floating Bob & Scale Pulse
            if (badgeTransform != null && badgeTransform.gameObject.activeSelf)
            {
                float bobOffset = animateFloatingBob ? Mathf.Sin(Time.time * bobSpeed + seedOffset) * bobHeight : 0f;
                badgeTransform.localPosition = new Vector3(0f, bobOffset, 0f);

                if (isCollectibleReady)
                {
                    // Energetic bounce when cash / match fee is ready
                    float bounce = 1f + Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed)) * bounceMagnitude;
                    badgeTransform.localScale = badgeBaseScale * bounce;

                    if (iconTransform != null)
                    {
                        float wobble = Mathf.Sin(Time.time * 6.5f) * 9f;
                        iconTransform.localRotation = Quaternion.Euler(0f, 0f, wobble);
                    }
                }
                else
                {
                    // Subtle breathing when normal
                    float breath = 1f + Mathf.Sin(Time.time * 1.8f + seedOffset) * 0.035f;
                    badgeTransform.localScale = badgeBaseScale * breath;

                    if (iconTransform != null)
                    {
                        iconTransform.localRotation = Quaternion.identity;
                    }
                }
            }

            // 3. Periodic state sync (checks every 0.6 seconds to catch simulation/timer updates)
            periodicCheckTimer += Time.deltaTime;
            if (periodicCheckTimer >= 0.6f)
            {
                periodicCheckTimer = 0f;
                RefreshVisuals();
            }
        }

        private void HandleBuildingStateChanged(PropertyBuilding b)
        {
            RefreshVisuals();
        }

        private void HandleMatchRequestChanged(bool pending)
        {
            if (targetBuilding != null && targetBuilding.PropertyType == PropertyType.Stadium)
            {
                RefreshVisuals();
            }
        }

        /// <summary>
        /// Evaluates the building state and configures text, icon, color, and animation.
        /// </summary>
        public void RefreshVisuals()
        {
            if (targetBuilding == null)
            {
                targetBuilding = GetComponentInParent<PropertyBuilding>();
                if (targetBuilding == null) return;
            }

            EnsureUIHierarchy();

            // 1. Check Mandatory Building Maintenance (Highest Priority Alert)
            if (targetBuilding.HasPendingMaintenance)
            {
                isCollectibleReady = true; // Trigger energetic pulsing bounce
                SetVisuals(
                    starSprite ?? keySprite,
                    $"MAINTENANCE: -${targetBuilding.PendingMaintenanceCost:N0}",
                    new Color(0.92f, 0.22f, 0.18f, 0.98f), // Urgent Crimson Red
                    Color.white
                );
                return;
            }

            // 2. Check Stadium Match Request
            bool isStadiumMatch = targetBuilding.PropertyType == PropertyType.Stadium &&
                                  StadiumMatchManager.Instance != null &&
                                  StadiumMatchManager.Instance.IsRequestPending;
            if (isStadiumMatch)
            {
                isCollectibleReady = true;
                double offer = StadiumMatchManager.Instance.CurrentPendingRentOffer;
                string text = offer > 0 ? $"MATCH +${offer:N0}!" : "MATCH READY!";
                SetVisuals(trophySprite ?? crownSprite ?? coinSprite, text, new Color(0.02f, 0.52f, 0.78f, 0.98f), Color.white);
                return;
            }

            // 3. Check Factory Production Earnings Ready to Collect
            if (targetBuilding.PropertyType == PropertyType.Factory && targetBuilding.PendingProductionEarnings > 0)
            {
                isCollectibleReady = true;
                SetVisuals(
                    coinSprite ?? starSprite,
                    $"PROD +${targetBuilding.PendingProductionEarnings:N0}",
                    new Color(0.15f, 0.55f, 0.90f, 0.98f), // Industrial Blue
                    Color.white
                );
                return;
            }

            // 4. Check Oil Station Petroleum Revenue Ready to Collect
            if (targetBuilding.PropertyType == PropertyType.OilStation && targetBuilding.PendingOilRevenue > 0)
            {
                isCollectibleReady = true;
                SetVisuals(
                    coinSprite ?? starSprite,
                    $"OIL +${targetBuilding.PendingOilRevenue:N0}",
                    new Color(0.85f, 0.65f, 0.08f, 0.98f), // Petroleum Gold
                    Color.white
                );
                return;
            }

            // 5. Check Collectible Rent
            double buildingRent = targetBuilding.IsOwned ? targetBuilding.DailyRentIncome : 0;
            double flatRent = targetBuilding.FlatsRentedOut * targetBuilding.FlatDailyRent;
            double totalDue = buildingRent + flatRent;

            if (targetBuilding.RentAvailableToCollect && totalDue > 0)
            {
                isCollectibleReady = true;
                SetVisuals(coinSprite, $"COLLECT ${totalDue:N0}", new Color(0.85f, 0.65f, 0.08f, 0.98f), Color.white);
                return;
            }

            // 6. Check Hospital Medical Treatment Needed (If Player Health is Low)
            if (targetBuilding.PropertyType == PropertyType.Hospital &&
                EmpireBuilder.Core.PlayerHealthManager.Instance != null &&
                EmpireBuilder.Core.PlayerHealthManager.Instance.IsLowHealth)
            {
                isCollectibleReady = true;
                SetVisuals(
                    checkmarkSprite ?? starSprite,
                    "HEAL HP (HOSPITAL)",
                    new Color(0.88f, 0.18f, 0.24f, 0.98f), // Red Cross Alert
                    Color.white
                );
                return;
            }

            // 7. Check Civic Municipal Buildings (School & Police Station)
            if (targetBuilding.IsCivic)
            {
                isCollectibleReady = false;
                Color civicBg = new Color(0.20f, 0.28f, 0.38f, 0.92f);
                string cTxt = targetBuilding.PropertyType == PropertyType.School ? "CIVIC: SCHOOL" : "CIVIC: POLICE HQ";
                SetVisuals(starSprite, cTxt, civicBg, Color.white);
                return;
            }

            // 8. Check Fully Owned Building
            if (targetBuilding.IsOwned)
            {
                isCollectibleReady = false;
                switch (targetBuilding.PropertyType)
                {
                    case PropertyType.Restaurant:
                        SetVisuals(shopSprite, "MY RESTAURANT", new Color(0.82f, 0.35f, 0.12f, 0.95f), Color.white);
                        break;
                    case PropertyType.Cafe:
                        SetVisuals(shopSprite, "MY CAFE", new Color(0.78f, 0.48f, 0.15f, 0.95f), Color.white);
                        break;
                    case PropertyType.Stadium:
                        SetVisuals(crownSprite ?? trophySprite, "MY STADIUM", new Color(0.40f, 0.28f, 0.85f, 0.95f), Color.white);
                        break;
                    case PropertyType.Factory:
                        SetVisuals(starSprite, "MY FACTORY", new Color(0.28f, 0.45f, 0.60f, 0.95f), Color.white);
                        break;
                    case PropertyType.OilStation:
                        SetVisuals(coinSprite, "MY OIL STATION", new Color(0.68f, 0.52f, 0.12f, 0.95f), Color.white);
                        break;
                    case PropertyType.Hospital:
                        SetVisuals(checkmarkSprite, "MY HOSPITAL", new Color(0.15f, 0.65f, 0.45f, 0.95f), Color.white);
                        break;
                    case PropertyType.TaxiStation:
                        int taxis = targetBuilding.CurrentlyRentedTaxis;
                        string taxiTxt = taxis > 0 ? $"TAXIS: {taxis}/8 ACTIVE" : "MY TAXI STATION";
                        SetVisuals(starSprite ?? coinSprite, taxiTxt, new Color(0.78f, 0.52f, 0.08f, 0.95f), Color.white);
                        break;
                    default:
                        string ownedTxt = targetBuilding.HasFlats ? $"ALL {targetBuilding.TotalFlats} FLATS OWNED" : "OWNED PROPERTY";
                        SetVisuals(checkmarkSprite ?? starSprite, ownedTxt, new Color(0.10f, 0.65f, 0.35f, 0.95f), Color.white);
                        break;
                }
                return;
            }

            // 9. Check Flat Ownership (Owns some flats, but not entire building)
            if (targetBuilding.HasFlats && targetBuilding.FlatsOwned > 0)
            {
                isCollectibleReady = false;
                string flatTxt = targetBuilding.FlatsRentedOut > 0
                    ? $"FLATS: {targetBuilding.FlatsOwned}/{targetBuilding.TotalFlats} ({targetBuilding.FlatsRentedOut} RENTED)"
                    : $"FLATS: {targetBuilding.FlatsOwned}/{targetBuilding.TotalFlats} OWNED";
                SetVisuals(keySprite, flatTxt, new Color(0.15f, 0.45f, 0.85f, 0.95f), Color.white);
                return;
            }

            // 10. Unowned / For Sale
            isCollectibleReady = false;
            if (!showUnownedIndicators)
            {
                if (badgeTransform != null) badgeTransform.gameObject.SetActive(false);
                return;
            }

            if (badgeTransform != null) badgeTransform.gameObject.SetActive(true);

            Color unownedBg = new Color(0.10f, 0.14f, 0.22f, 0.88f);
            Color unownedText = new Color(0.92f, 0.92f, 0.95f, 0.95f);

            if (targetBuilding.HasFlats)
            {
                SetVisuals(keySprite, $"{targetBuilding.TotalFlats} FLATS FOR SALE", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.Restaurant)
            {
                SetVisuals(shopSprite, $"RESTAURANT (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.Cafe)
            {
                SetVisuals(shopSprite, $"CAFE (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.Stadium)
            {
                SetVisuals(trophySprite ?? crownSprite, $"STADIUM (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.Factory)
            {
                SetVisuals(starSprite, $"FACTORY (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.OilStation)
            {
                SetVisuals(coinSprite, $"OIL STATION (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.Hospital)
            {
                SetVisuals(checkmarkSprite, $"HOSPITAL (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else if (targetBuilding.PropertyType == PropertyType.TaxiStation)
            {
                SetVisuals(coinSprite, $"TAXI FLEET (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
            else
            {
                SetVisuals(coinSprite, $"FOR SALE (${targetBuilding.PurchasePrice:N0})", unownedBg, unownedText);
            }
        }

        private void SetVisuals(Sprite icon, string message, Color bgColor, Color textColor)
        {
            if (badgeTransform != null) badgeTransform.gameObject.SetActive(true);

            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = textColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = bgColor;
            }
        }

        /// <summary>
        /// Self-healing initialization of the WorldSpace Canvas and UI elements.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            LoadDefaultSprites();

            // 1. World Canvas
            if (worldCanvas == null)
            {
                worldCanvas = GetComponentInChildren<Canvas>();
            }
            if (worldCanvas == null)
            {
                GameObject canvasObj = new GameObject("WorldCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                canvasObj.transform.SetParent(transform, false);
                canvasObj.transform.localPosition = Vector3.zero;
                worldCanvas = canvasObj.GetComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
                worldCanvas.overrideSorting = true;
                worldCanvas.sortingOrder = 300; // Always render on top of world mesh

                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10;
            }

            // 2. Badge Transform (Root of visual badge)
            if (badgeTransform == null)
            {
                Transform bt = worldCanvas.transform.Find("BadgeRoot");
                if (bt != null) badgeTransform = bt.GetComponent<RectTransform>();
            }
            if (badgeTransform == null)
            {
                GameObject badgeObj = new GameObject("BadgeRoot", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(worldCanvas.transform, false);
                badgeTransform = badgeObj.GetComponent<RectTransform>();
                badgeTransform.sizeDelta = new Vector2(250f, 52f);
                badgeTransform.localScale = badgeBaseScale;
                badgeTransform.localPosition = Vector3.zero;

                backgroundImage = badgeObj.GetComponent<Image>();
                if (backgroundRoundedSprite != null)
                {
                    backgroundImage.sprite = backgroundRoundedSprite;
                    backgroundImage.type = Image.Type.Sliced;
                }
                backgroundImage.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);
            }

            if (backgroundImage == null && badgeTransform != null)
            {
                backgroundImage = badgeTransform.GetComponent<Image>();
            }

            // 3. Icon Image
            if (iconImage == null && badgeTransform != null)
            {
                Transform iconTr = badgeTransform.Find("Icon");
                if (iconTr != null)
                {
                    iconImage = iconTr.GetComponent<Image>();
                    iconTransform = iconTr.GetComponent<RectTransform>();
                }
            }
            if (iconImage == null && badgeTransform != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(badgeTransform, false);
                iconTransform = iconObj.GetComponent<RectTransform>();
                iconTransform.anchorMin = new Vector2(0f, 0.5f);
                iconTransform.anchorMax = new Vector2(0f, 0.5f);
                iconTransform.pivot = new Vector2(0.5f, 0.5f);
                iconTransform.anchoredPosition = new Vector2(26f, 0f);
                iconTransform.sizeDelta = new Vector2(36f, 36f);

                iconImage = iconObj.GetComponent<Image>();
                iconImage.preserveAspect = true;
                if (coinSprite != null) iconImage.sprite = coinSprite;
            }

            // 4. Status Text
            if (statusText == null && badgeTransform != null)
            {
                Transform textTr = badgeTransform.Find("StatusText");
                if (textTr != null) statusText = textTr.GetComponent<TextMeshProUGUI>();
            }
            if (statusText == null && badgeTransform != null)
            {
                GameObject textObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(badgeTransform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(0f, 0f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.pivot = new Vector2(0.5f, 0.5f);
                textRt.anchoredPosition = new Vector2(18f, 0f); // Offset past the icon
                textRt.sizeDelta = new Vector2(-48f, 0f);

                statusText = textObj.GetComponent<TextMeshProUGUI>();
                statusText.fontSize = 15;
                statusText.fontSizeMin = 10;
                statusText.fontSizeMax = 17;
                statusText.enableAutoSizing = true;
                statusText.fontStyle = FontStyles.Bold;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = Color.white;
                statusText.text = "PROPERTY";

#if UNITY_EDITOR
                var font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Assets No/GUIPackCartoon/Demo/Fonts/LilitaOne - Regular SDF.asset");
                if (font != null) statusText.font = font;
#else
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                foreach (var f in fonts)
                {
                    if (f != null && f.name.Contains("LilitaOne"))
                    {
                        statusText.font = f;
                        break;
                    }
                }
#endif
            }

            // 5. Ensure BoxCollider for Raycast Clicks
            if (clickCollider == null)
            {
                clickCollider = GetComponent<BoxCollider>();
            }
            if (clickCollider == null)
            {
                clickCollider = gameObject.AddComponent<BoxCollider>();
            }
            clickCollider.size = new Vector3(6.5f, 2.2f, 1.2f);
            clickCollider.center = Vector3.zero;
        }

        private void LoadDefaultSprites()
        {
#if UNITY_EDITOR
            if (coinSprite == null)
                coinSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Coins/Coin.png");
            if (trophySprite == null)
                trophySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Cup.png");
            if (crownSprite == null)
                crownSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Badges/Crown.png");
            if (keySprite == null)
                keySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Lock - Key/Key - Gold.png");
            if (shopSprite == null)
                shopSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Commerce/Shop.png");
            if (starSprite == null)
                starSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Stars/Star Yellow.png");
            if (checkmarkSprite == null)
                checkmarkSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Icons/Icons Colored/Basic/Checkmark.png");
            if (backgroundRoundedSprite == null)
                backgroundRoundedSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets No/GUIPackCartoon/Demo/Sprites/Shapes/Shapes/Rectangle/Rounded Rectangle - 256px.png");
#endif
            if (coinSprite == null || backgroundRoundedSprite == null)
            {
                var loadedSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in loadedSprites)
                {
                    if (s == null) continue;
                    if (coinSprite == null && s.name == "Coin") coinSprite = s;
                    else if (trophySprite == null && s.name == "Cup") trophySprite = s;
                    else if (crownSprite == null && s.name == "Crown") crownSprite = s;
                    else if (keySprite == null && s.name == "Key - Gold") keySprite = s;
                    else if (shopSprite == null && s.name == "Shop") shopSprite = s;
                    else if (starSprite == null && s.name == "Star Yellow") starSprite = s;
                    else if (checkmarkSprite == null && s.name == "Checkmark") checkmarkSprite = s;
                    else if (backgroundRoundedSprite == null && s.name.Contains("Rounded Rectangle")) backgroundRoundedSprite = s;
                }
            }
        }

        /// <summary>
        /// External setup hook called by PropertyBuilding or Editor Setup Tool.
        /// </summary>
        public void Setup(PropertyBuilding building, Vector3 localRoofPos = default)
        {
            targetBuilding = building;
            if (localRoofPos != default)
            {
                transform.localPosition = localRoofPos;
            }
            EnsureUIHierarchy();
            RefreshVisuals();
        }
    }
}
