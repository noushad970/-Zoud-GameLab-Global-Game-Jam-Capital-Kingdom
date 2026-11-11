using System;
using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    [Serializable]
    public class TutorialStepData
    {
        public string iconEmoji;
        public string title;
        [TextArea(3, 8)]
        public string description;
        public Color accentColor;
    }

    /// <summary>
    /// Interactive guided onboarding tutorial for new players.
    /// Teaches camera controls, building purchases, flat acquisitions, rent collections,
    /// health/hunger restoration, stadium match invitations, and the top-right notice bar.
    /// Can be re-opened anytime from the HUD guide button.
    /// </summary>
    public class GameTutorialManager : MonoBehaviour
    {
        public static GameTutorialManager Instance { get; private set; }

        [Header("UI Component References")]
        [SerializeField] private GameObject tutorialModalRoot;
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private TextMeshProUGUI stepCountText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI iconEmojiText;
        [SerializeField] private Image iconBadgeImage;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonText;
        [SerializeField] private Button backButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button closeXButton;
        [SerializeField] private Transform stepDotsContainer;

        [Header("Settings")]
        [SerializeField] private bool autoShowOnFirstRun = true;
        [SerializeField] private float autoShowDelay = 1.2f;

        private int currentStepIndex = 0;
        private readonly List<TutorialStepData> steps = new List<TutorialStepData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureBindings();
            InitializeStepDefinitions();
        }

        private void Start()
        {
            EnsureBindings();
            BindButtons();

            // Check if player has completed tutorial
            int completed = PlayerPrefs.GetInt("TutorialCompleted", 0);
            if (autoShowOnFirstRun && completed == 0)
            {
                StartCoroutine(ShowTutorialDelayed(autoShowDelay));
            }
            else
            {
                if (tutorialModalRoot != null) tutorialModalRoot.SetActive(false);
            }
        }

        public void EnsureBindings()
        {
            if (tutorialModalRoot == null) tutorialModalRoot = gameObject;
            if (cardTransform == null)
            {
                var c = transform.Find("TutorialCard");
                if (c != null) cardTransform = c.GetComponent<RectTransform>();
            }
            if (cardTransform != null)
            {
                if (stepCountText == null) stepCountText = cardTransform.Find("StepCount")?.GetComponent<TextMeshProUGUI>();
                if (titleText == null) titleText = cardTransform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                if (bodyText == null)
                {
                    bodyText = cardTransform.Find("BodyBox/BodyText")?.GetComponent<TextMeshProUGUI>() ?? cardTransform.Find("BodyText")?.GetComponent<TextMeshProUGUI>();
                }
                if (iconEmojiText == null) iconEmojiText = cardTransform.Find("IconBadge/Emoji")?.GetComponent<TextMeshProUGUI>();
                if (iconBadgeImage == null) iconBadgeImage = cardTransform.Find("IconBadge")?.GetComponent<Image>();
                if (closeXButton == null) closeXButton = cardTransform.Find("CloseXButton")?.GetComponent<Button>();

                Transform bb = cardTransform.Find("BottomBar");
                if (bb != null)
                {
                    if (backButton == null) backButton = bb.Find("BackButton")?.GetComponent<Button>();
                    if (skipButton == null) skipButton = bb.Find("SkipButton")?.GetComponent<Button>();
                    if (nextButton == null) nextButton = bb.Find("NextButton")?.GetComponent<Button>();
                }
            }

            if (nextButton != null && nextButtonText == null)
            {
                nextButtonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void InitializeStepDefinitions()
        {
            steps.Clear();

            // Step 1: Welcome
            steps.Add(new TutorialStepData
            {
                iconEmoji = "👑",
                title = "WELCOME TO CAPITAL KINGDOM",
                description = "Greetings Mayor! You have arrived in Capital Kingdom with $10,000 starting capital.\n\nYour mission is to build a massive financial real estate empire! Acquire properties, purchase residential flats, collect rent, run factories, deploy taxis, and manage your health to conquer the city.",
                accentColor = new Color(0.18f, 0.52f, 0.95f)
            });

            // Step 2: Camera Controls
            steps.Add(new TutorialStepData
            {
                iconEmoji = "🎮",
                title = "CAMERA CONTROLS & NAVIGATION",
                description = "Explore the metropolitan district effortlessly:\n\n• <b>Pan Camera</b>: Hold Left Mouse Button & drag (or WASD / Arrow keys).\n• <b>Rotate View</b>: Hold Right Mouse Button & drag (or Q / E keys).\n• <b>Zoom</b>: Mouse Scroll Wheel (or pinch).\n• <b>Select Property</b>: Click any building to open its dashboard!",
                accentColor = new Color(0.20f, 0.75f, 0.55f)
            });

            // Step 3: Buying Buildings & Flats
            steps.Add(new TutorialStepData
            {
                iconEmoji = "🏢",
                title = "PROPERTIES & RESIDENTIAL FLATS",
                description = "Buildings in the city have 3D indicators above them:\n\n• <b>Rental Buildings</b>: Buy individual flats ($800/flat) or acquire the entire property!\n• <b>Commercial Buildings</b>: Acquire Factories, Oil Stations, Cafes, and the Grand Stadium.\n• <b>Civic Services</b>: Police Stations and Schools keep the city safe, while Hospitals heal you.",
                accentColor = new Color(0.92f, 0.65f, 0.15f)
            });

            // Step 4: Collecting Rent & Production Profits
            steps.Add(new TutorialStepData
            {
                iconEmoji = "💰",
                title = "COLLECTING RENTS & PRODUCTION PROFITS",
                description = "Your acquired assets generate high passive returns:\n\n• Every tenant in your flats pays daily rent.\n• When an indicator glows <b>GOLD ('COLLECT RENT')</b>, click the building to claim your money!\n• Factories produce manufacturing batches and Oil Stations accumulate fuel distribution revenue.",
                accentColor = new Color(0.95f, 0.75f, 0.10f)
            });

            // Step 5: Stadium Match Requests & Taxi Fleet
            steps.Add(new TutorialStepData
            {
                iconEmoji = "🏆",
                title = "STADIUM MATCHES & TAXI TRANSPORT",
                description = "Unlock special high-revenue metropolitan features:\n\n• <b>Olympic Stadium</b>: Major football clubs will periodically invite you to host matches for huge fees ($1,000 - $25,000)!\n• <b>Taxi Station</b>: Rent taxis to drive through the streets, generating steady automated daily fares.",
                accentColor = new Color(0.15f, 0.70f, 0.95f)
            });

            // Step 6: Health, Hunger & Hospital Care
            steps.Add(new TutorialStepData
            {
                iconEmoji = "❤️",
                title = "HEALTH, HUNGER & HOSPITAL CARE",
                description = "Manage your physical condition to keep running your empire:\n\n• Your health ticks down over time from hunger/energy depletion (watch the top-header Health Bar).\n• Visit any <b>Cafe or Restaurant</b> to eat food (+45 HP) or drink coffee (+25 HP).\n• If sick or exhausted (0 HP), visit <b>Capital General Hospital</b> for full medical treatment!",
                accentColor = new Color(0.92f, 0.25f, 0.35f)
            });

            // Step 7: Notice Bar & Maintenance
            steps.Add(new TutorialStepData
            {
                iconEmoji = "🔧",
                title = "CITY NOTICES & PROPERTY MAINTENANCE",
                description = "Stay informed and keep your empire running smoothly:\n\n• <b>Top-Right Notice Bar</b>: Displays instant alerts for stadium match offers, rent ready, repairs & hunger warnings!\n• <b>Maintenance</b>: Owned buildings will occasionally need repairs (plumbing, electrical, HVAC). Fix them promptly to ensure continuous rent flow!",
                accentColor = new Color(0.85f, 0.35f, 0.15f)
            });
        }

        private IEnumerator ShowTutorialDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowTutorial();
        }

        private void BindButtons()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
            if (closeXButton != null) closeXButton.onClick.AddListener(OnSkipClicked);
        }

        public void ShowTutorial(int startStep = 0)
        {
            currentStepIndex = Mathf.Clamp(startStep, 0, steps.Count - 1);

            if (tutorialModalRoot != null)
            {
                tutorialModalRoot.SetActive(true);
            }

            UpdateStepUI();

            if (cardTransform != null)
            {
                StartCoroutine(AnimateModalOpen(cardTransform));
            }

            GameSoundManager.Instance?.PlayNotification();
        }

        public void CloseTutorial()
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();

            if (tutorialModalRoot != null)
            {
                tutorialModalRoot.SetActive(false);
            }
        }

        private void OnNextClicked()
        {
            GameSoundManager.Instance?.PlayClick();

            if (currentStepIndex < steps.Count - 1)
            {
                currentStepIndex++;
                UpdateStepUI();
            }
            else
            {
                // Completed
                GameSoundManager.Instance?.PlaySuccess();
                CloseTutorial();
            }
        }

        private void OnBackClicked()
        {
            GameSoundManager.Instance?.PlayClick();

            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                UpdateStepUI();
            }
        }

        private void OnSkipClicked()
        {
            GameSoundManager.Instance?.PlayClick();
            CloseTutorial();
        }

        private void UpdateStepUI()
        {
            if (steps.Count == 0) return;

            var step = steps[currentStepIndex];

            if (stepCountText != null)
            {
                stepCountText.text = $"STEP {currentStepIndex + 1} OF {steps.Count}";
            }

            if (titleText != null)
            {
                titleText.text = step.title;
            }

            if (bodyText != null)
            {
                bodyText.text = step.description;
            }

            if (iconEmojiText != null)
            {
                iconEmojiText.text = step.iconEmoji;
            }

            if (iconBadgeImage != null)
            {
                iconBadgeImage.color = step.accentColor;
            }

            if (backButton != null)
            {
                backButton.gameObject.SetActive(currentStepIndex > 0);
            }

            if (nextButtonText != null)
            {
                nextButtonText.text = (currentStepIndex == steps.Count - 1) ? "START PLAYING! 🚀" : "NEXT ➔";
            }
        }

        private IEnumerator AnimateModalOpen(RectTransform rt)
        {
            rt.localScale = new Vector3(0.7f, 0.7f, 1f);
            float elapsed = 0f;
            while (elapsed < 0.2f && rt != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.2f);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                rt.localScale = Vector3.Lerp(new Vector3(0.7f, 0.7f, 1f), Vector3.one, ease);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }
    }
}
