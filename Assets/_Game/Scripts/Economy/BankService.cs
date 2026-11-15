using System;
using EmpireBuilder.Core;
using EmpireBuilder.Localization;
using EmpireBuilder.SaveLoad;
using EmpireBuilder.UI;
using UnityEngine;

namespace EmpireBuilder.Economy
{
    /// <summary>
    /// Manages the city banking system for Capital Kingdom.
    /// Features:
    /// 1. Account Opening: Requires a one-time fee of 100 coins.
    /// 2. Money Storage / Deposits: Store any amount of pocket cash in the bank account.
    /// 3. Money Withdrawals: Withdraw funds from bank balance back into pocket cash.
    /// 4. Daily Compound Interest: Automatically credits (10/30)% (~0.333333%) daily interest
    ///    on the total bank balance at every in-game day advancement.
    /// 5. Full Persistence across WebGL / PlayerPrefs / JSON save systems.
    /// </summary>
    [DisallowMultipleComponent]
    public class BankService : MonoBehaviour
    {
        public static BankService Instance { get; private set; }

        public const double AccountOpeningFee = 100.0;
        // Daily interest rate is (10 / 30)% = (10.0 / 30.0) / 100.0 = 0.0033333333333333335
        public const double DailyInterestRate = (10.0 / 30.0) / 100.0;

        [Header("Bank Account State")]
        [SerializeField] private bool hasBankAccount = false;
        [SerializeField] private double bankBalance = 0.0;
        [SerializeField] private double totalInterestEarned = 0.0;

        public bool HasBankAccount => hasBankAccount;
        public double BankBalance => bankBalance;
        public double TotalInterestEarned => totalInterestEarned;

        // Events
        public event Action OnBankStateChanged;
        public event Action<double> OnInterestEarned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadState();
        }

        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted += HandleDayCompleted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
            {
                GameManager.Instance.Simulation.OnDayCompleted -= HandleDayCompleted;
            }
        }

        /// <summary>
        /// Attempts to open a new bank account with the mandatory 100 coin fee.
        /// </summary>
        public bool TryOpenAccount(out string message)
        {
            if (hasBankAccount)
            {
                message = LocalizationManager.Get("bank_already_active");
                return false;
            }

            double currentCash = GetPlayerCash();
            if (currentCash < AccountOpeningFee)
            {
                message = LocalizationManager.Get("bank_fee_insufficient");
                return false;
            }

            // Deduct the 100 coin fee
            DeductPlayerCash(AccountOpeningFee, "Bank Account Opening Fee");

            hasBankAccount = true;
            SaveState();
            OnBankStateChanged?.Invoke();

            message = LocalizationManager.Get("bank_opened_success");
            Debug.Log("[BankService] Opened new bank account. Fee: 100 coins.");
            return true;
        }

        /// <summary>
        /// Stores / deposits an amount of money from the player's pocket cash into their bank account.
        /// </summary>
        public bool TryDeposit(double amount, out string message)
        {
            if (!hasBankAccount)
            {
                message = LocalizationManager.Get("bank_need_account_first");
                return false;
            }

            if (amount <= 0.01)
            {
                message = LocalizationManager.Get("bank_invalid_amount");
                return false;
            }

            double currentCash = GetPlayerCash();
            if (amount > currentCash)
            {
                message = LocalizationManager.Get("bank_deposit_insufficient");
                return false;
            }

            // Transfer cash to bank balance
            DeductPlayerCash(amount, $"Bank Deposit: ${amount:N0}");
            bankBalance += amount;

            SaveState();
            OnBankStateChanged?.Invoke();

            message = LocalizationManager.Get("bank_deposit_success", amount);
            Debug.Log($"[BankService] Deposited ${amount:N0}. New Balance: ${bankBalance:N2}");
            return true;
        }

        /// <summary>
        /// Withdraws funds from the bank account back into the player's pocket cash.
        /// </summary>
        public bool TryWithdraw(double amount, out string message)
        {
            if (!hasBankAccount)
            {
                message = LocalizationManager.Get("bank_need_account_first");
                return false;
            }

            if (amount <= 0.01)
            {
                message = LocalizationManager.Get("bank_invalid_amount");
                return false;
            }

            if (amount > bankBalance)
            {
                message = LocalizationManager.Get("bank_withdraw_insufficient");
                return false;
            }

            // Transfer bank balance to pocket cash
            bankBalance -= amount;
            AddPlayerCash(amount, $"Bank Withdrawal: ${amount:N0}");

            SaveState();
            OnBankStateChanged?.Invoke();

            message = LocalizationManager.Get("bank_withdraw_success", amount);
            Debug.Log($"[BankService] Withdrew ${amount:N0}. New Balance: ${bankBalance:N2}");
            return true;
        }

        /// <summary>
        /// Daily interest handler. Called when an in-game day cycle completes.
        /// Daily interest rate is (10/30)% = ~0.333333% daily.
        /// </summary>
        public void HandleDayCompleted(EmpireBuilder.Simulation.DailyReport report)
        {
            int dayNumber = report != null ? report.dayNumber : 1;
            ProcessDailyInterest(dayNumber);
        }

        public void ProcessDailyInterest(int dayNumber)
        {
            if (!hasBankAccount || bankBalance <= 0.01) return;

            // Interest calculation: (10/30)% of total bank balance
            double interestEarned = bankBalance * DailyInterestRate;
            bankBalance += interestEarned;
            totalInterestEarned += interestEarned;

            SaveState();
            OnInterestEarned?.Invoke(interestEarned);
            OnBankStateChanged?.Invoke();

            string noticeMsg = LocalizationManager.Get("bank_interest_credited", interestEarned);
            Debug.Log($"[BankService] Day {dayNumber} - {noticeMsg}");

            if (CityNoticeBarUI.Instance != null)
            {
                CityNoticeBarUI.Instance.PostNotice(LocalizationManager.Get("bank_notice_title"), noticeMsg, NoticeType.General, null, 9f);
            }
        }

        public void SetState(bool accountActive, double balance, double totalInterest)
        {
            hasBankAccount = accountActive;
            bankBalance = Math.Max(0.0, balance);
            totalInterestEarned = Math.Max(0.0, totalInterest);
            SaveState();
            OnBankStateChanged?.Invoke();
        }

        public void ResetBankData()
        {
            hasBankAccount = false;
            bankBalance = 0.0;
            totalInterestEarned = 0.0;

            PlayerPrefs.DeleteKey("Bank_HasAccount");
            PlayerPrefs.DeleteKey("Bank_Balance");
            PlayerPrefs.DeleteKey("Bank_TotalInterest");
            PlayerPrefs.Save();

            OnBankStateChanged?.Invoke();
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt("Bank_HasAccount", hasBankAccount ? 1 : 0);
            PlayerPrefs.SetString("Bank_Balance", bankBalance.ToString("R"));
            PlayerPrefs.SetString("Bank_TotalInterest", totalInterestEarned.ToString("R"));
            PlayerPrefs.Save();

            if (WebGLSaveManager.Instance != null)
            {
                WebGLSaveManager.Instance.SaveGame();
            }
        }

        private void LoadState()
        {
            hasBankAccount = PlayerPrefs.GetInt("Bank_HasAccount", 0) == 1;

            string balStr = PlayerPrefs.GetString("Bank_Balance", "0");
            if (double.TryParse(balStr, out double b)) bankBalance = b;

            string intStr = PlayerPrefs.GetString("Bank_TotalInterest", "0");
            if (double.TryParse(intStr, out double ti)) totalInterestEarned = ti;
        }

        private double GetPlayerCash()
        {
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                return GameManager.Instance.Economy.Cash;
            }
            return PlayerPrefs.GetFloat("SavedPlayerCash", 5000f);
        }

        private void DeductPlayerCash(double amount, string reason)
        {
            int day = GameManager.Instance != null && GameManager.Instance.Simulation != null
                ? GameManager.Instance.Simulation.CurrentDay
                : 1;

            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.ApplyTransaction(-amount, TransactionCategory.OperatingExpense, reason, day);
            }
            else
            {
                float cash = PlayerPrefs.GetFloat("SavedPlayerCash", 5000f);
                cash = Mathf.Max(0f, cash - (float)amount);
                PlayerPrefs.SetFloat("SavedPlayerCash", cash);
                PlayerPrefs.Save();
            }

            if (GamePlayHeaderUI.Instance != null)
            {
                GamePlayHeaderUI.Instance.UpdateMoneyDisplay();
            }
        }

        private void AddPlayerCash(double amount, string reason)
        {
            int day = GameManager.Instance != null && GameManager.Instance.Simulation != null
                ? GameManager.Instance.Simulation.CurrentDay
                : 1;

            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
            {
                GameManager.Instance.Economy.ApplyTransaction(amount, TransactionCategory.Revenue, reason, day);
            }
            else
            {
                float cash = PlayerPrefs.GetFloat("SavedPlayerCash", 5000f);
                cash += (float)amount;
                PlayerPrefs.SetFloat("SavedPlayerCash", cash);
                PlayerPrefs.Save();
            }

            if (GamePlayHeaderUI.Instance != null)
            {
                GamePlayHeaderUI.Instance.UpdateMoneyDisplay();
            }
        }
    }
}
