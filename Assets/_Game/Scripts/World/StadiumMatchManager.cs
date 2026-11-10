using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Simulation;
using EmpireBuilder.UI;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Coordinates football match hosting for the Grand Olympic Stadium.
    /// When the player purchases the stadium, this manager generates periodic
    /// match rental requests from football clubs and tournaments to earn massive rent revenues.
    /// Can be tuned directly in the Inspector in the Scene view.
    /// </summary>
    [DisallowMultipleComponent]
    public class StadiumMatchManager : MonoBehaviour
    {
        public static StadiumMatchManager Instance { get; private set; }

        [Header("Target Stadium Reference")]
        [SerializeField] private PropertyBuilding stadiumProperty;

        [Header("Match Request Frequency")]
        [Tooltip("Minimum time (in seconds) between random football match rent requests")]
        [SerializeField] private float minMatchInterval = 35f;
        [Tooltip("Maximum time (in seconds) between random football match rent requests")]
        [SerializeField] private float maxMatchInterval = 75f;

        [Header("Match Rent Economics")]
        [SerializeField] private double minMatchRent = 8500.0;
        [SerializeField] private double maxMatchRent = 25000.0;

        [Header("Runtime State")]
        [SerializeField] private float nextRequestTimer = 45f;
        [SerializeField] private bool isRequestPending = false;
        [SerializeField] private int totalMatchesHosted = 0;
        [SerializeField] private double totalMatchRevenueEarned = 0.0;

        private Coroutine requestLoopCoroutine;

        private readonly string[] matchTitles = new string[]
        {
            "National Championship Cup Final",
            "Premier League Derby: Capital FC vs Metro United",
            "Continental Supercup Exhibition",
            "Metropolitan Champions League Quarter-Final",
            "Grand Charity All-Stars Match",
            "City Trophy Classic: North Athletic vs South Wanderers",
            "International Friendly: National XI vs Royal FC"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (stadiumProperty == null)
            {
                FindStadiumInScene();
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

        public void FindStadiumInScene()
        {
            PropertyBuilding[] buildings = Object.FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in buildings)
            {
                if (b != null && b.PropertyType == PropertyType.Stadium)
                {
                    stadiumProperty = b;
                    break;
                }
            }
        }

        private IEnumerator MatchRequestTimerLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                // Only generate match requests if the player has purchased the stadium!
                if (stadiumProperty == null)
                {
                    FindStadiumInScene();
                    continue;
                }

                if (!stadiumProperty.IsOwned)
                {
                    // Wait until player owns the stadium
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
        /// Generates a new football match rent request and displays the modal.
        /// </summary>
        public void TriggerRandomMatchRequest()
        {
            if (stadiumProperty == null || !stadiumProperty.IsOwned)
            {
                Debug.Log("[StadiumMatchManager] Cannot host match: Stadium is not owned by the player.");
                return;
            }

            if (isRequestPending) return;

            string matchName = matchTitles[Random.Range(0, matchTitles.Length)];
            double rentOffer = System.Math.Round(Random.Range((float)minMatchRent, (float)maxMatchRent) / 100.0) * 100.0;

            isRequestPending = true;

            if (StadiumMatchRequestModalUI.Instance != null)
            {
                StadiumMatchRequestModalUI.Instance.OpenRequest(
                    matchName,
                    rentOffer,
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
            totalMatchesHosted++;
            totalMatchRevenueEarned += rentOffer;

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
                    $"Stadium Football Match Rent: {matchName}",
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

            if (stadiumProperty != null)
            {
                stadiumProperty.RegisterMatchHosted(rentOffer);
            }

            GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();

            PropertyDetailModalUI.Instance?.ShowToast(
                $"⚽ Football Match Hosted! Received +${rentOffer:N0} Stadium Rent!"
            );

            Debug.Log($"[StadiumMatchManager] Player accepted football match '{matchName}' for +${rentOffer:N0}!");
        }

        public void DeclineMatch()
        {
            isRequestPending = false;
            PropertyDetailModalUI.Instance?.ShowToast("⚽ Match rent request declined.");
            Debug.Log("[StadiumMatchManager] Player declined match rent request.");
        }
    }
}
