using System;
using System.Collections;
using EmpireBuilder.UI;
using TMPro;
using UnityEngine;

namespace EmpireBuilder.Core
{
    /// <summary>
    /// Manages the player's health, hunger/energy decay, and low-health warning alerts.
    /// Eating food or drinking beverages at restaurants and cafes restores health.
    /// </summary>
    public class PlayerHealthManager : MonoBehaviour
    {
        public static PlayerHealthManager Instance { get; private set; }

        [Header("Health Configuration")]
        [Tooltip("Maximum player health capacity")]
        [SerializeField] private float maxHealth = 100f;

        [Tooltip("Current player health value")]
        [SerializeField] private float currentHealth = 100f;

        [Tooltip("Health points lost per real-time second from hunger/energy depletion")]
        [SerializeField] private float decayRatePerSecond = 0.5f;

        [Tooltip("Enable continuous health decay over time")]
        [SerializeField] private bool decayActive = true;

        [Header("Restoration Values")]
        [Tooltip("Health restored when eating food at a restaurant or cafe")]
        [SerializeField] private float foodHealthRestore = 45f;

        [Tooltip("Health restored when drinking a beverage at a cafe or restaurant")]
        [SerializeField] private float drinkHealthRestore = 25f;

        [Header("Low Health Warnings")]
        [Tooltip("Health percentage/value threshold to trigger low health warnings (e.g. 30%)")]
        [SerializeField] private float lowHealthWarningThreshold = 30f;

        [Tooltip("Health percentage/value threshold for critical health warnings (e.g. 15%)")]
        [SerializeField] private float criticalHealthThreshold = 15f;

        [Tooltip("Seconds between recurring warning toasts when health is below threshold")]
        [SerializeField] private float warningRepeatInterval = 16f;

        [Header("Illness Condition")]
        [Tooltip("Health percentage/value threshold to contract illness from starvation (e.g. 25 HP)")]
        [SerializeField] private float illnessThreshold = 25f;

        [Tooltip("Whether the player is currently suffering from illness")]
        [SerializeField] private bool isIll = false;

        [Tooltip("Description of the current illness")]
        [SerializeField] private string illnessReason = "Severe Starvation & Exhaustion";

        [Tooltip("Multiplier applied to health decay rate when ill")]
        [SerializeField] private float illnessDecayMultiplier = 1.35f;

        // Events
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action<string, bool> OnWarningTriggered; // (message, isCritical)
        public event Action<bool> OnLowHealthStateChanged; // (isLow)
        public event Action<bool, string> OnIllnessStateChanged; // (isIll, illnessReason)

        // Runtime state
        private float warningCooldownTimer = 0f;
        private bool wasInLowHealthState = false;
        private float lastReportedHealth = -1f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth > 0.001f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public float FoodHealthRestore => foodHealthRestore;
        public float DrinkHealthRestore => drinkHealthRestore;
        public bool IsLowHealth => currentHealth <= lowHealthWarningThreshold;
        public bool IsCriticalHealth => currentHealth <= criticalHealthThreshold;
        public bool IsExhausted => currentHealth <= 0.01f;
        public bool IsIll => isIll;
        public string IllnessReason => illnessReason;
        public float IllnessThreshold => illnessThreshold;
        public float IllnessDecayMultiplier => illnessDecayMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Load saved health if available
            if (PlayerPrefs.HasKey("SavedPlayerHealth"))
            {
                currentHealth = PlayerPrefs.GetFloat("SavedPlayerHealth", maxHealth);
            }

            if (PlayerPrefs.HasKey("SavedPlayerIsIll"))
            {
                isIll = PlayerPrefs.GetInt("SavedPlayerIsIll", 0) == 1;
                illnessReason = PlayerPrefs.GetString("SavedPlayerIllnessReason", "Severe Starvation & Exhaustion");
            }

            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            lastReportedHealth = currentHealth;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (isIll)
            {
                OnIllnessStateChanged?.Invoke(true, illnessReason);
            }
            // Give player initial grace period before first low-health warning if started low
            warningCooldownTimer = 8f;
        }

        private void Update()
        {
            if (decayActive && currentHealth > 0f)
            {
                float decayRate = decayRatePerSecond * (isIll ? illnessDecayMultiplier : 1.0f);
                currentHealth = Mathf.Max(0f, currentHealth - decayRate * Time.deltaTime);

                // Auto-contract illness if health falls below illness threshold
                if (!isIll && currentHealth <= illnessThreshold)
                {
                    ContractIllness("Severe Starvation & Malnutrition");
                }

                // Notify listeners when health changes noticeably or hits zero
                if (Mathf.Abs(lastReportedHealth - currentHealth) >= 0.08f || currentHealth <= 0f)
                {
                    lastReportedHealth = currentHealth;
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                }
            }

            CheckLowHealthState();
        }

        private void CheckLowHealthState()
        {
            bool isLowNow = IsLowHealth;

            // Trigger state change event
            if (isLowNow != wasInLowHealthState)
            {
                wasInLowHealthState = isLowNow;
                OnLowHealthStateChanged?.Invoke(isLowNow);

                if (isLowNow)
                {
                    // Trigger immediate warning upon first crossing below threshold
                    TriggerLowHealthWarning();
                    warningCooldownTimer = warningRepeatInterval;
                }
            }
            else if (isLowNow)
            {
                // Recurring warning timer
                warningCooldownTimer -= Time.deltaTime;
                if (warningCooldownTimer <= 0f)
                {
                    TriggerLowHealthWarning();
                    warningCooldownTimer = warningRepeatInterval;
                }
            }
        }

        /// <summary>
        /// Restores player health by a specified amount (e.g. from eating or drinking).
        /// </summary>
        public float RestoreHealth(float amount, string source = "")
        {
            if (amount <= 0f) return 0f;

            float previous = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            float actualRestored = currentHealth - previous;

            // Reset warning timer to give the player breathing room
            warningCooldownTimer = warningRepeatInterval;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            bool isLowNow = IsLowHealth;
            if (isLowNow != wasInLowHealthState)
            {
                wasInLowHealthState = isLowNow;
                OnLowHealthStateChanged?.Invoke(isLowNow);
            }

            Debug.Log($"[PlayerHealthManager] Restored {actualRestored:F0} HP via {source}. Current Health: {currentHealth:F0}/{maxHealth:F0}");
            return actualRestored;
        }

        /// <summary>
        /// Consumes food from a restaurant or cafe, restoring health.
        /// </summary>
        public float EatFood(float bonusRestore = 0f)
        {
            float restoreAmount = foodHealthRestore + bonusRestore;
            return RestoreHealth(restoreAmount, "Dining / Food");
        }

        /// <summary>
        /// Drinks a beverage from a cafe or restaurant, restoring health/energy.
        /// </summary>
        public float DrinkBeverage(float bonusRestore = 0f)
        {
            float restoreAmount = drinkHealthRestore + bonusRestore;
            return RestoreHealth(restoreAmount, "Beverage / Drink");
        }

        /// <summary>
        /// Inflicts damage or direct health loss to the player.
        /// </summary>
        public void TakeDamage(float amount, string reason = "")
        {
            if (amount <= 0f) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"[PlayerHealthManager] Took {amount:F0} damage ({reason}). Current Health: {currentHealth:F0}/{maxHealth:F0}");
            CheckLowHealthState();
        }

        /// <summary>
        /// Explicitly sets current health.
        /// </summary>
        public void SetHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            if (!isIll && currentHealth <= illnessThreshold)
            {
                ContractIllness("Severe Starvation & Malnutrition");
            }
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            CheckLowHealthState();
        }

        /// <summary>
        /// Contracts illness from starvation, dehydration, or critical exhaustion.
        /// </summary>
        public void ContractIllness(string reason = "Severe Starvation & Malnutrition")
        {
            if (isIll) return;

            isIll = true;
            illnessReason = string.IsNullOrEmpty(reason) ? "Severe Starvation & Malnutrition" : reason;
            OnIllnessStateChanged?.Invoke(true, illnessReason);

            string alertMsg = $"🤒 ILLNESS CONTRACTED! You have fallen ill from starvation ({illnessReason})! Visit Capital General Hospital for medical treatment ($150)!";
            CityNoticeBarUI.Instance?.PostNotice(
                "🤒 ILLNESS DETECTED",
                alertMsg,
                NoticeType.Hospital,
                null,
                14f
            );

            TriggerLowHealthWarning();
            SaveHealth();
            Debug.LogWarning($"[PlayerHealthManager] Contracted illness: {illnessReason}");
        }

        /// <summary>
        /// Fully cures the player's illness through medical treatment at the Hospital.
        /// </summary>
        public void CureIllness()
        {
            if (!isIll) return;

            isIll = false;
            illnessReason = "";
            OnIllnessStateChanged?.Invoke(false, "");

            string healMsg = "❤️ FULLY CURED! Hospital medical treatment has cured your illness and restored full vitality!";
            CityNoticeBarUI.Instance?.PostNotice(
                "❤️ HEALTH RESTORED",
                healMsg,
                NoticeType.General,
                null,
                8f
            );

            SaveHealth();
            Debug.Log("[PlayerHealthManager] Illness successfully cured by hospital treatment!");
        }

        /// <summary>
        /// Triggers warning message toast and notifies UI listeners.
        /// </summary>
        public void TriggerLowHealthWarning()
        {
            string message;
            bool isCritical = IsCriticalHealth;

            if (isIll)
            {
                message = $"🤒 SICK & WEAK ({currentHealth:F0}/{maxHealth:F0})! Illness: {illnessReason}! Visit Capital General Hospital for medical treatment!";
            }
            else if (IsExhausted)
            {
                message = "💀 CRITICAL EXHAUSTION! Your health is empty! Visit a Restaurant or Cafe immediately to eat or drink!";
            }
            else if (isCritical)
            {
                message = $"🚨 CRITICAL HEALTH ({currentHealth:F0}/{maxHealth:F0})! Starving and weak! Visit a Restaurant or Cafe to eat or drink!";
            }
            else
            {
                message = $"⚠️ LOW HEALTH WARNING ({currentHealth:F0}/{maxHealth:F0})! Feeling hungry & tired! Grab food or coffee at a Restaurant or Cafe!";
            }

            OnWarningTriggered?.Invoke(message, isCritical);

            EmpireBuilder.Audio.GameSoundManager.Instance?.PlayLowHealthWarning();

            // Display floating toast if UI is available
            if (PropertyDetailModalUI.Instance != null)
            {
                PropertyDetailModalUI.Instance.ShowToast(message);
            }
            else
            {
                // Fallback direct discovery of FeedbackToast
                DisplayDirectToast(message);
            }

            Debug.LogWarning($"[PlayerHealthManager] {message}");
        }

        private void DisplayDirectToast(string message)
        {
            var toastTextObj = GameObject.Find("ToastText");
            if (toastTextObj != null)
            {
                var tmp = toastTextObj.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = message;
                    toastTextObj.transform.parent.gameObject.SetActive(true);
                }
            }
        }

        public void SetDecayActive(bool active)
        {
            decayActive = active;
        }

        public void SaveHealth()
        {
            PlayerPrefs.SetFloat("SavedPlayerHealth", currentHealth);
            PlayerPrefs.SetInt("SavedPlayerIsIll", isIll ? 1 : 0);
            PlayerPrefs.SetString("SavedPlayerIllnessReason", illnessReason ?? "");
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveHealth();
        }

        private void OnApplicationQuit()
        {
            SaveHealth();
        }
    }
}
