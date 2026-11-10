using System;
using System.Collections.Generic;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using EmpireBuilder.Maintenance;
using UnityEngine;

namespace EmpireBuilder.Simulation
{
    public class SimulationService : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EconomyService economy;
        [SerializeField] private AssetManager assetManager;
        [SerializeField] private MaintenanceService maintenance;

        private int currentDay = 1;
        private bool isProcessingDay = false;
        private DailyReport lastReport;

        public int CurrentDay => currentDay;
        public DailyReport LastReport => lastReport;
        public bool IsProcessingDay => isProcessingDay;

        public event Action<int> OnDayStarted;
        public event Action<DailyReport> OnDayCompleted;

        private readonly string[] financialLessons = new string[]
        {
            "Revenue is not Profit: Operating expenses and upkeep reduce your actual take-home cash.",
            "Emergency Savings: Always keep enough liquidity on hand to absorb unexpected repairs.",
            "Payback Period: Divide the purchase price by expected net income to see how fast you recover capital.",
            "Preventive Maintenance: Servicing assets early prevents costly downtime and total breakdown.",
            "Opportunity Cost: Tying up all your cash in land leaves you without income until you can develop it.",
            "Diversification: Spreading your capital across multiple asset types lowers operational risk.",
            "Net Worth vs Cash: Your true wealth includes physical property value, but property cannot directly pay bills."
        };

        public void Initialize(int startingDay = 1)
        {
            currentDay = startingDay;
        }

        public void SetDependencies(EconomyService econ, AssetManager am, MaintenanceService maint)
        {
            economy = econ;
            assetManager = am;
            maintenance = maint;
        }

        public bool AdvanceDay()
        {
            if (isProcessingDay) return false;
            isProcessingDay = true;

            OnDayStarted?.Invoke(currentDay);

            double dayRevenue = 0;
            double dayExpenses = 0;
            double dayRepairs = 0;
            var dayTransactions = new List<Transaction>();

            // 1. Freeze current set of operating assets
            var activeAssets = new List<OwnedAsset>(assetManager.OwnedAssets);

            foreach (var asset in activeAssets)
            {
                if (asset == null || asset.state == AssetState.Sold) continue;

                var def = assetManager.GetDefinition(asset.definitionId);
                if (def == null) continue;

                // 2. Revenue calculation
                if (asset.CanOperate())
                {
                    // Condition affects performance: efficiency drops slightly if condition is worn
                    float conditionFactor = Mathf.Clamp(asset.condition / 100f, 0.4f, 1.0f);
                    double rev = Math.Round(def.grossIncomePerDay * conditionFactor, 2);
                    if (rev > 0)
                    {
                        dayRevenue += rev;
                        economy.ApplyTransaction(rev, TransactionCategory.Revenue, $"{asset.customName} Revenue", currentDay, asset.instanceId);
                    }

                    // 3. Operating expenses
                    double exp = def.operatingCostPerDay;
                    if (exp > 0)
                    {
                        dayExpenses += exp;
                        economy.ApplyTransaction(-exp, TransactionCategory.OperatingExpense, $"{asset.customName} Upkeep & Expenses", currentDay, asset.instanceId);
                    }

                    // 4. Condition degradation
                    maintenance.DegradeAsset(asset, def);
                    assetManager.NotifyAssetUpdated(asset);
                }
                else if (asset.state == AssetState.Inactive)
                {
                    // Land or idle asset holding costs
                    if (def.assetType == AssetType.Land && def.operatingCostPerDay > 0)
                    {
                        double holdingCost = def.operatingCostPerDay;
                        dayExpenses += holdingCost;
                        economy.ApplyTransaction(-holdingCost, TransactionCategory.OperatingExpense, $"{asset.customName} Holding Tax", currentDay, asset.instanceId);
                    }
                }
            }

            // 5. Update Net Worth
            economy.RecalculateNetWorth(new List<OwnedAsset>(assetManager.OwnedAssets));

            // 6. Select an educational lesson
            string lesson = financialLessons[(currentDay - 1) % financialLessons.Length];

            // 7. Create Daily Report
            double netChange = dayRevenue - dayExpenses - dayRepairs;
            lastReport = new DailyReport(
                currentDay,
                dayRevenue,
                dayExpenses,
                dayRepairs,
                netChange,
                economy.Cash,
                economy.NetWorth,
                lesson,
                dayTransactions
            );

            // 8. Advance Day Counter
            currentDay++;
            isProcessingDay = false;

            OnDayCompleted?.Invoke(lastReport);
            return true;
        }
    }
}

