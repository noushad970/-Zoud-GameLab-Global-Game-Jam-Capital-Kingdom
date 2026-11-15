using System;
using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Audio;
using EmpireBuilder.Localization;
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
    /// Supports dynamic bilingual Arabic and English localization.
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
        [SerializeField] private TextMeshProUGUI backButtonText;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI skipButtonText;
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

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            InitializeStepDefinitions();
            UpdateStepUI();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            InitializeStepDefinitions();
            UpdateStepUI();
        }

        private void Start()
        {
            EnsureBindings();
            BindButtons();

            // Check if player has completed tutorial
            int completed = PlayerPrefs.GetInt("TutorialCompleted", 0);
            if (autoShowOnFirstRun && completed == 0)
            {
                ShowTutorial(0);
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
            if (backButton != null && backButtonText == null)
            {
                backButtonText = backButton.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (skipButton != null && skipButtonText == null)
            {
                skipButtonText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        private void InitializeStepDefinitions()
        {
            steps.Clear();

            // Step 1: Welcome
            steps.Add(new TutorialStepData
            {
                iconEmoji = "1",
                title = LocalizationManager.Get("tut_welcome_title"),
                description = LocalizationManager.Get("tut_welcome_desc"),
                accentColor = new Color(0.18f, 0.52f, 0.95f)
            });

            // Step 2: Camera Controls
            steps.Add(new TutorialStepData
            {
                iconEmoji = "2",
                title = LocalizationManager.Get("tut_camera_title"),
                description = LocalizationManager.Get("tut_camera_desc"),
                accentColor = new Color(0.20f, 0.75f, 0.55f)
            });

            // Step 3: Buying Buildings & Flats
            steps.Add(new TutorialStepData
            {
                iconEmoji = "3",
                title = LocalizationManager.Get("tut_properties_title"),
                description = LocalizationManager.Get("tut_properties_desc"),
                accentColor = new Color(0.92f, 0.65f, 0.15f)
            });

            // Step 4: National Bank & Compound Savings
            steps.Add(new TutorialStepData
            {
                iconEmoji = "4",
                title = LocalizationManager.Get("tut_bank_title"),
                description = LocalizationManager.Get("tut_bank_desc"),
                accentColor = new Color(0.95f, 0.75f, 0.10f)
            });

            // Step 5: Residential Flats & Upgrades
            steps.Add(new TutorialStepData
            {
                iconEmoji = "5",
                title = LocalizationManager.Get("tut_flats_title"),
                description = LocalizationManager.Get("tut_flats_desc"),
                accentColor = new Color(0.15f, 0.70f, 0.95f)
            });

            // Step 6: Industrial Plants & Oil Refineries
            steps.Add(new TutorialStepData
            {
                iconEmoji = "6",
                title = LocalizationManager.Get("tut_industry_title"),
                description = LocalizationManager.Get("tut_industry_desc"),
                accentColor = new Color(0.92f, 0.45f, 0.15f)
            });

            // Step 7: City Taxi Fleet & Logistics
            steps.Add(new TutorialStepData
            {
                iconEmoji = "7",
                title = LocalizationManager.Get("tut_taxi_title"),
                description = LocalizationManager.Get("tut_taxi_desc"),
                accentColor = new Color(0.95f, 0.75f, 0.10f)
            });

            // Step 8: Health, Hunger & Hospital Care
            steps.Add(new TutorialStepData
            {
                iconEmoji = "8",
                title = LocalizationManager.Get("tut_health_title"),
                description = LocalizationManager.Get("tut_health_desc"),
                accentColor = new Color(0.92f, 0.25f, 0.35f)
            });

            // Step 9: Notice Bar & Maintenance
            steps.Add(new TutorialStepData
            {
                iconEmoji = "9",
                title = LocalizationManager.Get("tut_alerts_title"),
                description = LocalizationManager.Get("tut_alerts_desc"),
                accentColor = new Color(0.85f, 0.35f, 0.15f)
            });

            // Step 10: Arena Tournaments & Matches
            steps.Add(new TutorialStepData
            {
                iconEmoji = "10",
                title = LocalizationManager.Get("tut_stadium_title"),
                description = LocalizationManager.Get("tut_stadium_desc"),
                accentColor = new Color(0.18f, 0.80f, 0.44f)
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
                stepCountText.text = LocalizationManager.Get("tut_step_count", currentStepIndex + 1, steps.Count);
            }

            bool isArabic = LocalizationManager.CurrentLanguage == GameLanguage.Arabic;

            if (titleText != null)
            {
                titleText.text = step.title;
                titleText.alignment = isArabic ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            }

            if (bodyText != null)
            {
                bodyText.text = step.description;
                bodyText.alignment = isArabic ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
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

            if (backButtonText != null)
            {
                backButtonText.text = LocalizationManager.Get("tut_btn_back");
            }

            if (skipButtonText != null)
            {
                skipButtonText.text = LocalizationManager.Get("tut_btn_skip");
            }

            if (nextButtonText != null)
            {
                nextButtonText.text = (currentStepIndex == steps.Count - 1)
                    ? LocalizationManager.Get("tut_btn_finish")
                    : LocalizationManager.Get("tut_btn_next");
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
