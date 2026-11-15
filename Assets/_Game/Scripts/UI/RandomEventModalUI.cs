using EmpireBuilder.Core;
using EmpireBuilder.Events;
using EmpireBuilder.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class RandomEventModalUI : MonoBehaviour
    {
        [Header("Modal Root")]
        [SerializeField] private GameObject modalRoot;

        [Header("Text Labels")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Choice A")]
        [SerializeField] private Button choiceAButton;
        [SerializeField] private TextMeshProUGUI choiceAText;

        [Header("Choice B")]
        [SerializeField] private Button choiceBButton;
        [SerializeField] private TextMeshProUGUI choiceBText;

        [Header("Event Service")]
        [SerializeField] private EventService eventService;

        private GameEvent activeEvent;

        private void Start()
        {
            if (eventService != null)
            {
                eventService.OnEventTriggered += DisplayEvent;
            }

            if (choiceAButton != null) choiceAButton.onClick.AddListener(OnChoiceAClicked);
            if (choiceBButton != null) choiceBButton.onClick.AddListener(OnChoiceBClicked);

            if (modalRoot != null) modalRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (eventService != null)
            {
                eventService.OnEventTriggered -= DisplayEvent;
            }
        }

        public void DisplayEvent(GameEvent ev)
        {
            if (ev == null) return;
            activeEvent = ev;

            if (titleText != null) LocalizationManager.ApplyTo(titleText, ev.title);
            if (descriptionText != null) LocalizationManager.ApplyTo(descriptionText, ev.description);

            if (choiceAText != null)
            {
                string textA = ev.choiceA != null ? ev.choiceA.choiceText : LocalizationManager.Get("menu_confirm");
                LocalizationManager.ApplyTo(choiceAText, textA);
            }
            if (choiceBText != null)
            {
                string textB = ev.choiceB != null ? ev.choiceB.choiceText : LocalizationManager.Get("menu_cancel");
                LocalizationManager.ApplyTo(choiceBText, textB);
            }

            if (modalRoot != null) modalRoot.SetActive(true);
        }

        private void OnChoiceAClicked()
        {
            if (activeEvent != null && activeEvent.choiceA != null && GameManager.Instance != null)
            {
                eventService.ResolveChoice(activeEvent, activeEvent.choiceA, GameManager.Instance.Economy, GameManager.Instance.Simulation.CurrentDay);
            }
            Close();
        }

        private void OnChoiceBClicked()
        {
            if (activeEvent != null && activeEvent.choiceB != null && GameManager.Instance != null)
            {
                eventService.ResolveChoice(activeEvent, activeEvent.choiceB, GameManager.Instance.Economy, GameManager.Instance.Simulation.CurrentDay);
            }
            Close();
        }

        public void Close()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            activeEvent = null;
        }
    }
}

