using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.Assets
{
    [CreateAssetMenu(fileName = "NewAssetDefinition", menuName = "Empire Builder/Asset Definition")]
    public class AssetDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string assetId;
        public string displayName;
        public AssetType assetType;
        [TextArea(2, 4)]
        public string description;

        [Header("Economics")]
        [Min(0)] public double purchasePrice;
        [Min(0)] public double grossIncomePerDay;
        [Min(0)] public double operatingCostPerDay;

        [Header("Condition & Maintenance")]
        [Range(0, 100)] public float startingCondition = 100f;
        [Min(0)] public double baseRepairCost = 100;
        [Range(0, 50)] public float dailyConditionDegradation = 2.5f;

        [Header("Visuals")]
        public Sprite icon;
        public GameObject worldPrefab;

        public double ExpectedNetDailyIncome => grossIncomePerDay - operatingCostPerDay;

        public double CalculatePaybackDays()
        {
            double net = ExpectedNetDailyIncome;
            if (net <= 0) return 0;
            return purchasePrice / net;
        }

        public string GetPaybackString()
        {
            double net = ExpectedNetDailyIncome;
            if (net <= 0) return "N/A (Holding)";
            double days = purchasePrice / net;
            return $"{days:F0} days";
        }
    }
}

