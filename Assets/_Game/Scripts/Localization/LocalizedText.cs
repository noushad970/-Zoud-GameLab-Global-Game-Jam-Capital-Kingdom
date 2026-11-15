using TMPro;
using UnityEngine;

namespace EmpireBuilder.Localization
{
    /// <summary>
    /// Attach to any GameObject with a TextMeshProUGUI or TMP_Text component.
    /// Automatically updates the text content and applies appropriate shaping/alignment
    /// whenever the active language changes in LocalizationManager.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;
        [SerializeField] private string prefix = "";
        [SerializeField] private string suffix = "";
        [SerializeField] private bool adjustAlignmentForRTL = false;

        private TMP_Text textComponent;
        private TextAlignmentOptions defaultAlignment;

        public string LocalizationKey
        {
            get => localizationKey;
            set
            {
                localizationKey = value;
                RefreshText();
            }
        }

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                defaultAlignment = textComponent.alignment;
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
            RefreshText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged()
        {
            RefreshText();
        }

        public void RefreshText()
        {
            if (textComponent == null) textComponent = GetComponent<TMP_Text>();
            if (textComponent == null || string.IsNullOrEmpty(localizationKey)) return;

            string localized = LocalizationManager.Get(localizationKey);
            string full = $"{prefix}{localized}{suffix}";
            LocalizationManager.ApplyTo(textComponent, full);

            if (adjustAlignmentForRTL)
            {
                if (LocalizationManager.CurrentLanguage == GameLanguage.Arabic)
                {
                    if (defaultAlignment == TextAlignmentOptions.Left)
                        textComponent.alignment = TextAlignmentOptions.Right;
                }
                else
                {
                    textComponent.alignment = defaultAlignment;
                }
            }
        }
    }
}
