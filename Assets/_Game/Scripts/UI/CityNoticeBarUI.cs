using System;
using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public enum NoticeType
    {
        General,
        StadiumMatch,
        Maintenance,
        RentReady,
        HealthWarning,
        Taxi,
        Hospital
    }

    [Serializable]
    public class CityNoticeItem
    {
        public string id;
        public string title;
        public string description;
        public NoticeType type;
        public PropertyBuilding targetBuilding;
        public float expireTime;
        public bool isPersistent;
        public GameObject cardObject;
    }

    /// <summary>
    /// Displays a dynamic, animated city notice bar in the top-right corner below the header UI.
    /// Informs the player in real-time about all occurrences in their city:
    /// - Stadium match hosting invitations & rent requests
    /// - Property maintenance orders & repair needs
    /// - Rent & business production revenue waiting to be collected
    /// - Low health, hunger, energy depletion & hospital warnings
    /// - Taxi station fleet status
    /// Clicking any notice automatically focuses the camera on the target building!
    /// </summary>
    public class CityNoticeBarUI : MonoBehaviour
    {
        public static CityNoticeBarUI Instance { get; private set; }

        [Header("UI Containers")]
        [SerializeField] private RectTransform noticeContainer;
        [SerializeField] private GameObject noticeCardPrefab;
        [SerializeField] private TextMeshProUGUI noticeCountText;
        [SerializeField] private Image statusDotImage;

        [Header("Color Schemes")]
        [SerializeField] private Color colorGeneral = new Color(0.2f, 0.45f, 0.85f, 0.95f);
        [SerializeField] private Color colorMatch = new Color(0.12f, 0.65f, 0.38f, 0.95f);
        [SerializeField] private Color colorMaintenance = new Color(0.85f, 0.28f, 0.15f, 0.95f);
        [SerializeField] private Color colorRent = new Color(0.92f, 0.68f, 0.12f, 0.95f);
        [SerializeField] private Color colorHealth = new Color(0.88f, 0.20f, 0.35f, 0.95f);
        [SerializeField] private Color colorTaxi = new Color(0.95f, 0.75f, 0.10f, 0.95f);

        [Header("Settings")]
        [SerializeField] private int maxSimultaneousNotices = 4;
        [SerializeField] private float defaultNoticeDuration = 9f;
        [SerializeField] private float cityScanInterval = 3f;

        private readonly List<CityNoticeItem> activeNotices = new List<CityNoticeItem>();
        private readonly HashSet<string> currentNoticeKeys = new HashSet<string>();
        private float scanTimer = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureContainer();
            // Initial welcome notice
            PostNotice(
                "CAPITAL CITY ACTIVE",
                "City operations online. Watch this notice bar for matches, rents, repairs & health alerts!",
                NoticeType.General,
                null,
                7f
            );
        }

        private void Update()
        {
            UpdateNoticeTimers();

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = cityScanInterval;
                ScanCityOccurrences();
            }

            // Animate pulsating status dot
            if (statusDotImage != null)
            {
                float alpha = 0.5f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.5f;
                Color col = activeNotices.Count > 0 ? Color.green : Color.cyan;
                col.a = alpha;
                statusDotImage.color = col;
            }

            if (noticeCountText != null)
            {
                noticeCountText.text = activeNotices.Count > 0 ? $"{activeNotices.Count} ALERTS" : "CITY IDLE";
            }
        }

        public void EnsureContainer()
        {
            if (noticeContainer == null)
            {
                var c = transform.Find("NoticeContainer");
                if (c != null) noticeContainer = c.GetComponent<RectTransform>();
                else noticeContainer = GetComponent<RectTransform>();
            }

            if (noticeCountText == null)
            {
                var cnt = transform.Find("NoticeHeader/NoticeCount");
                if (cnt != null) noticeCountText = cnt.GetComponent<TextMeshProUGUI>();
            }

            if (statusDotImage == null)
            {
                var dot = transform.Find("NoticeHeader/StatusDot");
                if (dot != null) statusDotImage = dot.GetComponent<Image>();
            }
        }

        private void UpdateNoticeTimers()
        {
            float now = Time.time;
            for (int i = activeNotices.Count - 1; i >= 0; i--)
            {
                var notice = activeNotices[i];
                if (!notice.isPersistent && now >= notice.expireTime)
                {
                    DismissNotice(notice);
                }
            }
        }

        /// <summary>
        /// Post a dynamic notification to the top-right notice bar.
        /// </summary>
        public void PostNotice(string title, string description, NoticeType type, PropertyBuilding targetBuilding = null, float duration = 9f, bool persistent = false)
        {
            string key = $"{type}_{title}_{targetBuilding?.name}";
            if (currentNoticeKeys.Contains(key)) return;

            // Trim if full
            while (activeNotices.Count >= maxSimultaneousNotices)
            {
                DismissNotice(activeNotices[0]);
            }

            var item = new CityNoticeItem
            {
                id = key,
                title = title,
                description = description,
                type = type,
                targetBuilding = targetBuilding,
                expireTime = Time.time + (duration > 0f ? duration : defaultNoticeDuration),
                isPersistent = persistent
            };

            GameObject card = CreateNoticeCardUI(item);
            item.cardObject = card;

            activeNotices.Add(item);
            currentNoticeKeys.Add(key);

            GameSoundManager.Instance?.PlayNotification();
        }

        private void DismissNotice(CityNoticeItem item)
        {
            if (item == null) return;

            if (item.cardObject != null)
            {
                Destroy(item.cardObject);
            }

            currentNoticeKeys.Remove(item.id);
            activeNotices.Remove(item);
        }

        private GameObject CreateNoticeCardUI(CityNoticeItem item)
        {
            if (noticeContainer == null) EnsureContainer();

            GameObject card = new GameObject("NoticeCard", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(noticeContainer, false);

            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350, 68);

            var bgImg = card.GetComponent<Image>();
            bgImg.color = GetColorForType(item.type);

            // Left icon badge
            var iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
            iconBox.transform.SetParent(card.transform, false);
            var iconRt = iconBox.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(8, 0);
            iconRt.sizeDelta = new Vector2(48, 48);
            var iconBoxImg = iconBox.GetComponent<Image>();
            iconBoxImg.color = new Color(0, 0, 0, 0.25f);

            var iconTextObj = new GameObject("IconEmoji", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconTextObj.transform.SetParent(iconBox.transform, false);
            var emojiTmp = iconTextObj.GetComponent<TextMeshProUGUI>();
            emojiTmp.text = GetEmojiForType(item.type);
            emojiTmp.fontSize = 24;
            emojiTmp.alignment = TextAlignmentOptions.Center;
            emojiTmp.rectTransform.anchorMin = Vector2.zero;
            emojiTmp.rectTransform.anchorMax = Vector2.one;
            emojiTmp.rectTransform.sizeDelta = Vector2.zero;

            // Title
            var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = item.title;
            titleTmp.fontSize = 13;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Left;
            var titleRt = titleTmp.rectTransform;
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot = new Vector2(0, 1);
            titleRt.anchoredPosition = new Vector2(62, -8);
            titleRt.sizeDelta = new Vector2(-95, 20);

            // Description
            var descObj = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(card.transform, false);
            var descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.text = item.description;
            descTmp.fontSize = 11;
            descTmp.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.overflowMode = TextOverflowModes.Ellipsis;
            var descRt = descTmp.rectTransform;
            descRt.anchorMin = new Vector2(0, 0);
            descRt.anchorMax = new Vector2(1, 1);
            descRt.pivot = new Vector2(0, 0);
            descRt.anchoredPosition = new Vector2(62, 6);
            descRt.sizeDelta = new Vector2(-95, -30);

            // Close (X) button
            var closeBtnObj = new GameObject("DismissBtn", typeof(RectTransform), typeof(Button), typeof(Image));
            closeBtnObj.transform.SetParent(card.transform, false);
            var closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1, 1);
            closeRt.anchorMax = new Vector2(1, 1);
            closeRt.pivot = new Vector2(1, 1);
            closeRt.anchoredPosition = new Vector2(-6, -6);
            closeRt.sizeDelta = new Vector2(22, 22);
            closeBtnObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);

            var xTextObj = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
            xTextObj.transform.SetParent(closeBtnObj.transform, false);
            var xTmp = xTextObj.GetComponent<TextMeshProUGUI>();
            xTmp.text = "✕";
            xTmp.fontSize = 12;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;
            xTmp.rectTransform.anchorMin = Vector2.zero;
            xTmp.rectTransform.anchorMax = Vector2.one;
            xTmp.rectTransform.sizeDelta = Vector2.zero;

            closeBtnObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                DismissNotice(item);
            });

            // Card click listener: Pan camera to building or open detail modal
            var cardBtn = card.GetComponent<Button>();
            cardBtn.onClick.AddListener(() =>
            {
                OnNoticeClicked(item);
            });

            // Small pop-in animation
            StartCoroutine(AnimateCardPopIn(card));

            return card;
        }

        private IEnumerator AnimateCardPopIn(GameObject card)
        {
            if (card == null) yield break;
            Transform tr = card.transform;
            tr.localScale = new Vector3(0.8f, 0.8f, 1f);
            float elapsed = 0f;
            while (elapsed < 0.15f && tr != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.15f);
                tr.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, t);
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        private void OnNoticeClicked(CityNoticeItem item)
        {
            if (item == null) return;

            GameSoundManager.Instance?.PlayClick();

            if (item.targetBuilding != null)
            {
                // 1. Focus camera on building
                IsometricCameraController camCtrl = FindAnyObjectByType<IsometricCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.FocusOnPosition(item.targetBuilding.transform.position);
                }

                // 2. Select building and open dashboard
                BuildingInteractionManager.Instance?.SelectBuilding(item.targetBuilding);
            }
            else if (item.type == NoticeType.HealthWarning || item.type == NoticeType.Hospital)
            {
                // Find hospital or cafe
                PropertyBuilding hospital = FindHospital();
                if (hospital != null)
                {
                    FindAnyObjectByType<IsometricCameraController>()?.FocusOnPosition(hospital.transform.position);
                    BuildingInteractionManager.Instance?.SelectBuilding(hospital);
                }
            }
        }

        private PropertyBuilding FindHospital()
        {
            PropertyBuilding[] all = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var b in all)
            {
                if (b != null && b.PropertyType == PropertyType.Hospital) return b;
            }
            return null;
        }

        private Color GetColorForType(NoticeType type)
        {
            switch (type)
            {
                case NoticeType.StadiumMatch: return colorMatch;
                case NoticeType.Maintenance: return colorMaintenance;
                case NoticeType.RentReady: return colorRent;
                case NoticeType.HealthWarning: return colorHealth;
                case NoticeType.Hospital: return colorHealth;
                case NoticeType.Taxi: return colorTaxi;
                default: return colorGeneral;
            }
        }

        private string GetEmojiForType(NoticeType type)
        {
            switch (type)
            {
                case NoticeType.StadiumMatch: return "🏆";
                case NoticeType.Maintenance: return "🔧";
                case NoticeType.RentReady: return "💰";
                case NoticeType.HealthWarning: return "❤️";
                case NoticeType.Hospital: return "🏥";
                case NoticeType.Taxi: return "🚕";
                default: return "📢";
            }
        }

        /// <summary>
        /// Scans city state and surfaces actionable occurrences to the player.
        /// </summary>
        private void ScanCityOccurrences()
        {
            // 1. Check Stadium Match Request
            if (StadiumMatchManager.Instance != null && StadiumMatchManager.Instance.IsRequestPending)
            {
                PostNotice(
                    "STADIUM MATCH INVITATION",
                    $"{StadiumMatchManager.Instance.CurrentPendingMatchName} offers ${StadiumMatchManager.Instance.CurrentPendingRentOffer:N0}! Tap to host.",
                    NoticeType.StadiumMatch,
                    StadiumMatchManager.Instance.StadiumProperty,
                    15f
                );
            }

            // 2. Check Player Health
            if (PlayerHealthManager.Instance != null)
            {
                if (PlayerHealthManager.Instance.IsExhausted)
                {
                    PostNotice(
                        "💀 EXHAUSTION WARNING",
                        "Health empty! Visit Capital General Hospital or eat immediately!",
                        NoticeType.Hospital,
                        FindHospital(),
                        12f
                    );
                }
                else if (PlayerHealthManager.Instance.IsCriticalHealth)
                {
                    PostNotice(
                        "🚨 CRITICAL HUNGER & HEALTH",
                        $"Health {PlayerHealthManager.Instance.CurrentHealth:F0} HP! Eat at a Cafe/Restaurant or visit Hospital!",
                        NoticeType.HealthWarning,
                        null,
                        10f
                    );
                }
                else if (PlayerHealthManager.Instance.IsLowHealth)
                {
                    PostNotice(
                        "⚠️ HUNGER ALERT",
                        $"Health dropping ({PlayerHealthManager.Instance.CurrentHealth:F0} HP). Grab food or coffee at a Cafe!",
                        NoticeType.HealthWarning,
                        null,
                        8f
                    );
                }
            }

            // 3. Check All Buildings (Maintenance, Rent Ready, Production Profits, Oil Revenue)
            PropertyBuilding[] buildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b == null) continue;

                // Maintenance alert
                if (b.IsOwned && b.HasPendingMaintenance)
                {
                    PostNotice(
                        "MAINTENANCE ORDER REQUIRED",
                        $"{b.PropertyName} requires ${b.PendingMaintenanceCost:N0} repairs ({b.MaintenanceReason}). Tap to fix!",
                        NoticeType.Maintenance,
                        b,
                        14f
                    );
                }

                // Rent ready alert
                if (b.RentAvailableToCollect && (b.IsOwned || b.FlatsOwned > 0))
                {
                    double rent = b.CalculateAvailableRent();
                    if (rent > 0)
                    {
                        PostNotice(
                            "RENT REVENUE READY",
                            $"{b.PropertyName} has ${rent:N0} tenant rent available to collect!",
                            NoticeType.RentReady,
                            b,
                            12f
                        );
                    }
                }

                // Factory production ready
                if (b.IsOwned && b.PropertyType == PropertyType.Factory && b.PendingProductionEarnings > 0)
                {
                    PostNotice(
                        "FACTORY PRODUCTION FINISHED",
                        $"{b.PropertyName} completed manufacturing batch (+${b.PendingProductionEarnings:N0})!",
                        NoticeType.RentReady,
                        b,
                        12f
                    );
                }

                // Oil revenue ready
                if (b.IsOwned && b.PropertyType == PropertyType.OilStation && b.PendingOilRevenue > 0)
                {
                    PostNotice(
                        "FUEL REVENUE READY",
                        $"{b.PropertyName} accumulated ${b.PendingOilRevenue:N0} oil & gas distribution profits!",
                        NoticeType.RentReady,
                        b,
                        12f
                    );
                }
            }
        }
    }
}
