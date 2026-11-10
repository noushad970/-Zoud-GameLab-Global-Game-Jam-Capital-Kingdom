using System;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using UnityEngine;

namespace EmpireBuilder.Maintenance
{
    public class MaintenanceService : MonoBehaviour
    {
        public double CalculateRepairCost(OwnedAsset asset, AssetDefinition def)
        {
            if (asset == null || def == null) return 0;
            if (asset.condition >= 100f) return 0;

            // GDD Section 10.E formula:
            // repairPrice = baseRepairCost * (1 + (100 - condition) / 100)
            double multiplier = 1.0 + ((100.0 - asset.condition) / 100.0);
            double cost = def.baseRepairCost * multiplier;
            return Math.Round(cost, 2);
        }

        public bool TryRepairAsset(OwnedAsset asset, AssetDefinition def, EconomyService economy, int currentDay)
        {
            if (asset == null || def == null || economy == null) return false;
            if (asset.condition >= 100f) return false;

            double repairCost = CalculateRepairCost(asset, def);
            if (!economy.CanAfford(repairCost))
            {
                Debug.LogWarning($"[MaintenanceService] Cannot afford repair for {asset.customName} (Cost: ${repairCost:N0})");
                return false;
            }

            bool paid = economy.ApplyTransaction(-repairCost, TransactionCategory.Repair, $"Repaired {asset.customName}", currentDay, asset.instanceId);
            if (paid)
            {
                asset.condition = 100f;
                asset.SetState(AssetState.Operating);
                return true;
            }

            return false;
        }

        public void DegradeAsset(OwnedAsset asset, AssetDefinition def)
        {
            if (asset == null || def == null) return;
            if (asset.state != AssetState.Operating) return;

            asset.condition = Mathf.Max(0f, asset.condition - def.dailyConditionDegradation);

            if (asset.condition <= 20f && asset.condition > 0f)
            {
                asset.SetState(AssetState.NeedsRepair);
            }
            else if (asset.condition <= 0f)
            {
                asset.condition = 0f;
                asset.SetState(AssetState.Inactive); // Broken down, cannot earn
            }
        }
    }
}

