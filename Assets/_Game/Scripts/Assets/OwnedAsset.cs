using System;
using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.Assets
{
    [Serializable]
    public class OwnedAsset
    {
        public string instanceId;
        public string definitionId;
        public string customName;
        public float condition = 100f;
        public bool isOperating = true;
        public int purchaseDay;
        public double currentMarketValue;
        public AssetState state = AssetState.Operating;

        public OwnedAsset()
        {
        }

        public OwnedAsset(AssetDefinition definition, int currentDay)
        {
            instanceId = Guid.NewGuid().ToString("N");
            definitionId = definition.assetId;
            customName = definition.displayName;
            condition = definition.startingCondition;
            isOperating = true;
            purchaseDay = currentDay;
            currentMarketValue = definition.purchasePrice;
            state = AssetState.Operating;
        }

        public bool CanOperate()
        {
            return isOperating && condition > 0 && state == AssetState.Operating;
        }

        public void SetState(AssetState newState)
        {
            state = newState;
            if (state == AssetState.NeedsRepair || state == AssetState.UnderRepair || state == AssetState.Inactive)
            {
                isOperating = false;
            }
            else if (state == AssetState.Operating)
            {
                isOperating = condition > 0;
            }
        }
    }
}

