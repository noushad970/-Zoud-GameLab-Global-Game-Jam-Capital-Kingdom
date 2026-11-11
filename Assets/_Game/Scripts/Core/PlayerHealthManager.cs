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
        [SerializeField] private float decayRatePerSecond = 0.4f;

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

        // Events
        public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
        public event Action<string, bool> OnWarningTriggered; // (message, isCritical)
        public event Action<bool> OnLowHealthStateChanged; // (isLow)

        // Runtime state
        private float warningCooldownTimer = 0f;
        private bool wasInLowHealthState = false;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth > 0.001f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public float FoodHealthRestore => foodHealthRestore;
        public float DrinkHealthRestore => drinkHealthRestore;
        public bool IsLowHealth => currentHealth <= lowHealthWarningThreshold;
        public bool IsCriticalHealth => currentHealth <= criticalHealthThreshold;
        public bool IsExhausted => currentHealth <= 0.01f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            // Give player initial grace period before first low-health warning if started low
            warningCooldownTimer = 8f;
        }

        private void Update()
        {
            if (decayActive && currentHealth > 0f)
            {
                float previousHealth = currentHealth;
                currentHealth = Mathf.Max(0f, currentHealth - decayRatePerSecond * Time.deltaTime);

                if (Mathf.Abs(previousHealth - currentHealth) > 0.05f)
                {
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
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            CheckLowHealthState();
        }

        /// <summary>
        /// Triggers warning message toast and notifies UI listeners.
        /// </summary>
        public void TriggerLowHealthWarning()
        {
            string message;
            bool isCritical = IsCriticalHealth;

            if (IsExhausted)
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
    }
}
