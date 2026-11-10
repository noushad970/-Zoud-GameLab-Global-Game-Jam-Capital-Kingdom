using System;
using System.Collections.Generic;
using EmpireBuilder.Core;
using EmpireBuilder.Economy;
using UnityEngine;

namespace EmpireBuilder.Events
{
    public class EventService : MonoBehaviour
    {
        [SerializeField] private List<GameEvent> eventPool = new List<GameEvent>();
        [Range(0f, 1f)] [SerializeField] private float eventChancePerDay = 0.35f;

        public event Action<GameEvent> OnEventTriggered;

        public void TryTriggerRandomEvent(int currentDay, EconomyService economy)
        {
            if (eventPool == null || eventPool.Count == 0) return;

            // Roll chance
            if (UnityEngine.Random.value <= eventChancePerDay)
            {
                int index = UnityEngine.Random.Range(0, eventPool.Count);
                var ev = eventPool[index];
                if (ev != null)
                {
                    OnEventTriggered?.Invoke(ev);
                }
            }
        }

        public void ResolveChoice(GameEvent ev, EventChoice choice, EconomyService economy, int currentDay)
        {
            if (ev == null || choice == null || economy == null) return;

            if (choice.cashImpact != 0)
            {
                economy.ApplyTransaction(choice.cashImpact, TransactionCategory.Event, $"Event: {ev.title} ({choice.choiceText})", currentDay);
            }

            Debug.Log($"[EventService] Resolved event '{ev.title}': {choice.outcomeDescription}");
        }
    }
}

