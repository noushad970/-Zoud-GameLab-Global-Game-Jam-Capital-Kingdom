using System;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using UnityEngine;

namespace EmpireBuilder.Events
{
    [Serializable]
    public class EventChoice
    {
        public string choiceText;
        public double cashImpact; // Negative = cost, Positive = gain
        public float conditionImpact; // Change in condition
        public string outcomeDescription;
    }

    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Empire Builder/Game Event")]
    public class GameEvent : ScriptableObject
    {
        public string eventId;
        public string title;
        [TextArea(2, 4)]
        public string description;
        public AssetType relevantAssetType;

        public EventChoice choiceA;
        public EventChoice choiceB;
    }
}

