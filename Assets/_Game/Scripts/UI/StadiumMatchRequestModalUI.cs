using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    /// <summary>
    /// Modal dialog displayed when a football club or tournament requests to rent the Grand Olympic Stadium.
    /// Allows the player to accept and earn massive stadium rent money, or decline.
    /// Can be customized and styled directly in the Scene Hierarchy.
    /// </summary>
    public class StadiumMatchRequestModalUI : MonoBehaviour
    {
        public static StadiumMatchRequestModalUI Instance { get; private set; }

        [Header("Modal UI Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private Button overlayButton;

        [Header("Match Request Details")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI matchNameText;
        [SerializeField] private TextMeshProUGUI rentOfferAmountText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Action Buttons")]
        [SerializeField] private Button acceptButton;
        [SerializeField] private TextMeshProUGUI acceptButtonText;
        [SerializeField] private Button declineButton;
        [SerializeField] private TextMeshProUGUI declineButtonText;
        [SerializeField] private Button closeButton;

        private Action onAcceptedCallback;
        private Action onDeclinedCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureBindings();
        }

        private void Start()
        {
            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(OnAcceptClicked);
            }

            if (declineButton != null)
            {
                declineButton.onClick.RemoveAllListeners();
                declineButton.onClick.AddListener(OnDeclineClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnDeclineClicked);
            }

            if (overlayButton != null)
            {
                overlayButton.onClick.RemoveAllListeners();
                overlayButton.onClick.AddListener(OnDeclineClicked);
            }

            // Initially hidden
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void EnsureBindings()
        {
            if (panelRoot == null && transform.childCount > 0)
            {
                panelRoot = transform.GetChild(0).gameObject;
            }
        }

        /// <summary>
        /// Displays the match rent request modal with default football stadium styling.
        /// </summary>
        public void OpenRequest(string matchName, double rentOffer, Action onAccepted, Action onDeclined)
        {
            OpenRequest(
                "⚽ STADIUM MATCH RENTAL REQUEST",
                matchName,
                rentOffer,
                "A major sports association is offering to rent your facility for this high-profile match!",
                onAccepted,
                onDeclined
            );
        }

        /// <summary>
        /// Displays the match rent request modal with customizable header, match details, and description.
        /// </summary>
        public void OpenRequest(string headerTitle, string matchName, double rentOffer, string description, Action onAccepted, Action onDeclined)
        {
            onAcceptedCallback = onAccepted;
            onDeclinedCallback = onDeclined;

            if (headerTitleText != null && !string.IsNullOrEmpty(headerTitle))
            {
                headerTitleText.text = headerTitle;
            }

            if (matchNameText != null)
            {
                matchNameText.text = matchName;
            }

            if (rentOfferAmountText != null)
            {
                rentOfferAmountText.text = $"+${rentOffer:N0}";
            }

            if (acceptButtonText != null)
            {
                acceptButtonText.text = $"ACCEPT & EARN +${rentOffer:N0}";
            }

            if (descriptionText != null)
            {
                descriptionText.text = description;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnAcceptClicked()
        {
            Close();
            onAcceptedCallback?.Invoke();
        }

        private void OnDeclineClicked()
        {
            Close();
            onDeclinedCallback?.Invoke();
        }
    }
}
