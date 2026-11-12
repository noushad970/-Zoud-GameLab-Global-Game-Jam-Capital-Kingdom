using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Simulation;
using EmpireBuilder.UI;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Coordinates tennis tournament & match hosting for the City Tennis Court.
    /// Operates under the exact same high-revenue tournament rent mechanics as the Stadium:
    /// When owned by the player, periodically receives tennis match hosting requests
    /// with lucrative payout offers.
    /// Can be tuned directly in the Inspector in the Scene view.
    /// </summary>
    [DisallowMultipleComponent]
    public class TennisMatchManager : MonoBehaviour
    {
        public static TennisMatchManager Instance { get; private set; }

        [Header("Target Tennis Court Reference")]
        [SerializeField] private PropertyBuilding tennisCourtProperty;

        [Header("Match Request Frequency")]
        [Tooltip("Minimum time (in seconds) between random tennis tournament rent requests")]
        [SerializeField] private float minMatchInterval = 30f;
        [Tooltip("Maximum time (in seconds) between random tennis tournament rent requests")]
        [SerializeField] private float maxMatchInterval = 65f;

        [Header("Match Rent Economics")]
        [SerializeField] private double minMatchRent = 2500.0;
        [SerializeField] private double maxMatchRent = 7500.0;

        [Header("Runtime State")]
        [SerializeField] private float nextRequestTimer = 35f;
        [SerializeField] private bool isRequestPending = false;
        [SerializeField] private int totalMatchesHosted = 0;
        [SerializeField] private double totalMatchRevenueEarned = 0.0;
        [SerializeField] private string currentPendingMatchName = "";
        [SerializeField] private double currentPendingRentOffer = 0.0;

        public bool IsRequestPending => isRequestPending;
        public string CurrentPendingMatchName => currentPendingMatchName;
        public double CurrentPendingRentOffer => currentPendingRentOffer;
        public PropertyBuilding TennisCourtProperty => tennisCourtProperty;
        public static event System.Action<bool> OnMatchRequestStateChanged;

        private Coroutine requestLoopCoroutine;

        private readonly string[] matchTitles = new string[]
        {
            "City Open Championship: Singles Semi-Final",
            "Metropolitan Tennis Cup: Grand Final",
            "Junior Pro Invitational Tournament",
            "Grand Slam Capital Masters",
            "National Doubles Championship Classic",
            "Charity Celebrity Tennis Exhibition",
            "International Federation Challenger Trophy"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (tennisCourtProperty == null)
            {
                FindTennisCourtInScene();
            }
        }

        private void Start()
        {
            nextRequestTimer = Random.Range(minMatchInterval, maxMatchInterval);
            requestLoopCoroutine = StartCoroutine(MatchRequestTimerLoop());
        }

        private void OnDestroy()
        {
            if (requestLoopCoroutine != null)
            {
                StopCoroutine(requestLoopCoroutine);
                requestLoopCoroutine = null;
            }
        }

        public void FindTennisCourtInScene()
        {
            PropertyBuilding[] buildings = Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b != null && b.PropertyType == PropertyType.TennisCourt)
                {
                    tennisCourtProperty = b;
                    break;
                }
            }
        }

        private IEnumerator MatchRequestTimerLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                // Only generate match requests if the player has purchased the tennis court!
                if (tennisCourtProperty == null)
                {
                    FindTennisCourtInScene();
                    continue;
                }

                if (!tennisCourtProperty.IsOwned)
                {
                    // Wait until player owns the tennis court
                    continue;
                }

                if (isRequestPending)
                {
                    continue;
                }

                nextRequestTimer -= 1.0f;
                if (nextRequestTimer <= 0f)
                {
                    TriggerRandomMatchRequest();
                    nextRequestTimer = Random.Range(minMatchInterval, maxMatchInterval);
                }
            }
        }

        /// <summary>
        /// Generates a new tennis tournament match rent request and displays the modal.
        /// </summary>
        public void TriggerRandomMatchRequest()
        {
            if (tennisCourtProperty == null || !tennisCourtProperty.IsOwned)
            {
                Debug.Log("[TennisMatchManager] Cannot host match: Tennis Court is not owned by the player.");
                return;
            }

            if (isRequestPending) return;

            string matchName = matchTitles[Random.Range(0, matchTitles.Length)];
            double rentOffer = System.Math.Round(Random.Range((float)minMatchRent, (float)maxMatchRent) / 50.0) * 50.0;

            isRequestPending = true;
            currentPendingMatchName = matchName;
            currentPendingRentOffer = rentOffer;
            OnMatchRequestStateChanged?.Invoke(true);
            if (tennisCourtProperty != null) tennisCourtProperty.NotifyStateChanged();

            GameSoundManager.Instance?.PlayStadiumMatchRequestAlert(tennisCourtProperty != null ? tennisCourtProperty.transform.position : (Vector3?)null);

            CityNoticeBarUI.Instance?.ShowNotice(
                $"🎾 Tennis Tournament Offer: {matchName} is offering +${rentOffer:N0} to host!",
                NoticeType.StadiumMatch,
                8f
            );

            if (StadiumMatchRequestModalUI.Instance != null)
            {
                StadiumMatchRequestModalUI.Instance.OpenRequest(
                    "🎾 TENNIS MATCH RENTAL REQUEST",
                    matchName,
                    rentOffer,
                    "A professional tennis tour association is offering to rent your Championship Tennis Court for this match!",
                    onAccepted: () => AcceptMatch(matchName, rentOffer),
                    onDeclined: () => DeclineMatch()
                );
            }
            else
            {
                // Fallback auto-accept / toast if modal UI not present
                AcceptMatch(matchName, rentOffer);
            }
        }

        public void AcceptMatch(string matchName, double rentOffer)
        {
            isRequestPending = false;
            currentPendingMatchName = "";
            currentPendingRentOffer = 0.0;
            OnMatchRequestStateChanged?.Invoke(false);
            if (tennisCourtProperty != null) tennisCourtProperty.NotifyStateChanged();

            GameSoundManager.Instance?.PlayClick();

            totalMatchesHosted++;
            totalMatchRevenueEarned += rentOffer;

            Vector3? courtPos = tennisCourtProperty != null ? tennisCourtProperty.transform.position : (Vector3?)null;
            GameSoundManager.Instance?.PlayStadiumMatchCheer(courtPos);
            GameSoundManager.Instance?.PlayCollect(courtPos);

            int currentDay = 1;
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                currentDay = GameManager.Instance.Simulation.CurrentDay;
            }

            // Credit payment to player account
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.ApplyTransaction(
                    rentOffer,
                    TransactionCategory.Revenue,
                    $"Tennis Match Rent: {matchName}",
                    currentDay
                );
            }
            else
            {
                float savedCash = PlayerPrefs.GetFloat("SavedPlayerCash", 10000f);
                savedCash += (float)rentOffer;
                PlayerPrefs.SetFloat("SavedPlayerCash", savedCash);
                PlayerPrefs.Save();
            }

            if (tennisCourtProperty != null)
            {
                tennisCourtProperty.RegisterMatchHosted(rentOffer);
            }

            GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();

            PropertyDetailModalUI.Instance?.ShowToast(
                $"🎾 Tennis Tournament Hosted! Received +${rentOffer:N0} Court Rent!"
            );

            CityNoticeBarUI.Instance?.ShowNotice(
                $"🎾 Match Complete! You earned +${rentOffer:N0} from {matchName}.",
                NoticeType.RentReady,
                6f
            );

            Debug.Log($"[TennisMatchManager] Player accepted tennis match '{matchName}' for +${rentOffer:N0}!");
        }

        public void DeclineMatch()
        {
            isRequestPending = false;
            currentPendingMatchName = "";
            currentPendingRentOffer = 0.0;
            OnMatchRequestStateChanged?.Invoke(false);
            if (tennisCourtProperty != null) tennisCourtProperty.NotifyStateChanged();

            GameSoundManager.Instance?.PlayClick();

            PropertyDetailModalUI.Instance?.ShowToast("🎾 Tennis match request declined.");
            Debug.Log("[TennisMatchManager] Player declined tennis match request.");
        }
    }
}
