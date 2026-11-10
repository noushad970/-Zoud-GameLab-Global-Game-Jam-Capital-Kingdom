using System;
using System.Collections.Generic;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.Economy
{
    public class EconomyService : MonoBehaviour
    {
        [Header("Starting Economy")]
        [SerializeField] private double defaultStartingCash = 10000.0; // Appendix A: $10,000

        private double currentCash;
        private double currentNetWorth;
        private readonly List<Transaction> transactionHistory = new List<Transaction>();

        public double Cash => currentCash;
        public double NetWorth => currentNetWorth;
        public IReadOnlyList<Transaction> Transactions => transactionHistory.AsReadOnly();

        public event Action<double> OnCashChanged;
        public event Action<double> OnNetWorthChanged;
        public event Action<Transaction> OnTransactionApplied;

        public void Initialize(double startingCash = -1)
        {
            currentCash = startingCash >= 0 ? startingCash : defaultStartingCash;
            transactionHistory.Clear();

            // Record initial capital
            ApplyTransaction(currentCash, TransactionCategory.InitialCapital, "Starting Capital", 1);
            RecalculateNetWorth(new List<OwnedAsset>());
        }

        public void LoadState(double cash, List<Transaction> history, List<OwnedAsset> ownedAssets)
        {
            currentCash = cash;
            transactionHistory.Clear();
            if (history != null)
            {
                transactionHistory.AddRange(history);
            }
            RecalculateNetWorth(ownedAssets);
            OnCashChanged?.Invoke(currentCash);
        }

        public bool CanAfford(double cost)
        {
            if (double.IsNaN(cost) || double.IsInfinity(cost) || cost < 0) return false;
            return currentCash >= cost;
        }

        public bool ApplyTransaction(double amount, TransactionCategory category, string description, int dayNumber, string assetId = "")
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                Debug.LogError("[EconomyService] Attempted invalid transaction amount.");
                return false;
            }

            // If outflow, ensure sufficient cash (unless loan/event allows debt)
            if (amount < 0 && (currentCash + amount) < 0)
            {
                Debug.LogWarning($"[EconomyService] Insufficient funds for transaction: {description}. Required: {amount}, Cash: {currentCash}");
                return false;
            }

            currentCash += amount;
            var tx = new Transaction(dayNumber, amount, category, description, assetId);
            transactionHistory.Add(tx);

            OnCashChanged?.Invoke(currentCash);
            OnTransactionApplied?.Invoke(tx);
            return true;
        }

        public bool TryPurchaseAsset(AssetDefinition definition, int currentDay, out OwnedAsset newAsset)
        {
            newAsset = null;
            if (definition == null) return false;

            double cost = definition.purchasePrice;
            if (!CanAfford(cost))
            {
                Debug.LogWarning($"[EconomyService] Cannot afford {definition.displayName} (Price: ${cost:N0}, Cash: ${currentCash:N0})");
                return false;
            }

            // Deduct cash via official transaction
            bool success = ApplyTransaction(-cost, TransactionCategory.Purchase, $"Purchased {definition.displayName}", currentDay, definition.assetId);
            if (success)
            {
                newAsset = new OwnedAsset(definition, currentDay);
                return true;
            }

            return false;
        }

        public bool TrySellAsset(OwnedAsset asset, AssetDefinition definition, int currentDay, double salePercentage = 0.7)
        {
            if (asset == null || definition == null) return false;

            // Sale value based on market value & condition (GDD Section 5)
            double saleValue = (asset.currentMarketValue * (asset.condition / 100.0)) * salePercentage;
            saleValue = Math.Max(0, Math.Round(saleValue));

            bool success = ApplyTransaction(saleValue, TransactionCategory.Sale, $"Sold {asset.customName}", currentDay, asset.instanceId);
            if (success)
            {
                asset.SetState(AssetState.Sold);
                return true;
            }

            return false;
        }

        public void RecalculateNetWorth(List<OwnedAsset> ownedAssets, double liabilities = 0)
        {
            double assetValuation = 0;
            if (ownedAssets != null)
            {
                foreach (var asset in ownedAssets)
                {
                    if (asset.state != AssetState.Sold)
                    {
                        // Market value adjusted by asset condition
                        assetValuation += asset.currentMarketValue * (asset.condition / 100.0);
                    }
                }
            }

            // Net Worth = Cash + Asset Market Values - Liabilities (Section 5)
            currentNetWorth = Math.Max(0, currentCash + assetValuation - liabilities);
            OnNetWorthChanged?.Invoke(currentNetWorth);
        }
    }
}

