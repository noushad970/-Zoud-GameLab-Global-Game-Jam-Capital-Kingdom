using System;
using System.Collections.Generic;
using EmpireBuilder.World;
using TMPro;
using UnityEngine;

namespace EmpireBuilder.Localization
{
    public enum GameLanguage
    {
        English = 0,
        Arabic = 1
    }

    /// <summary>
    /// Centralized Localization Manager supporting English and Arabic translations.
    /// Manages language switching, PlayerPrefs persistence, event notifications,
    /// dynamic string lookup, and seamless Arabic text shaping via ArabicFixer.
    /// Also manages automatic font switching between English font and Arabic font (Tahoma SDF)
    /// to guarantee zero missing glyph tofu boxes across all game UI.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public const string PREF_KEY_LANGUAGE = "Game_Language_Code";

        public static GameLanguage CurrentLanguage { get; private set; } = GameLanguage.English;

        public static event Action OnLanguageChanged;

        [Header("Font Assets")]
        [SerializeField] private TMP_FontAsset englishFont;
        [SerializeField] private TMP_FontAsset arabicFont;

        private static TMP_FontAsset cachedArabicFontAsset;
        private static TMP_FontAsset cachedEnglishFontAsset;

        public static TMP_FontAsset EnglishFont
        {
            get
            {
                if (Instance != null && Instance.englishFont != null) return Instance.englishFont;
                if (cachedEnglishFontAsset == null)
                {
                    cachedEnglishFontAsset = Resources.Load<TMP_FontAsset>("Fonts/LilitaOne - Regular SDF")
                                          ?? Resources.Load<TMP_FontAsset>("LilitaOne - Regular SDF")
                                          ?? TMP_Settings.defaultFontAsset;
                }
                return cachedEnglishFontAsset;
            }
        }

        public static TMP_FontAsset ArabicFont
        {
            get
            {
                if (Instance != null && Instance.arabicFont != null) return Instance.arabicFont;
                if (cachedArabicFontAsset == null) EnsureRuntimeArabicFont();
                return cachedArabicFontAsset;
            }
        }

        public static void EnsureRuntimeArabicFont()
        {
            if (cachedArabicFontAsset != null) return;

            // 1. Try loading pre-built Tahoma SDF from Resources
            cachedArabicFontAsset = Resources.Load<TMP_FontAsset>("Fonts/Tahoma SDF");
            if (cachedArabicFontAsset == null)
            {
                cachedArabicFontAsset = Resources.Load<TMP_FontAsset>("Tahoma SDF");
            }

            // 2. If not found in Resources as TMP_FontAsset, dynamically create from Font
            if (cachedArabicFontAsset == null)
            {
                Font font = Resources.Load<Font>("Fonts/Tahoma");
                if (font != null)
                {
                    try
                    {
                        cachedArabicFontAsset = TMP_FontAsset.CreateFontAsset(font);
                        cachedArabicFontAsset.name = "Tahoma Dynamic SDF";
                        Debug.Log("[LocalizationManager] Dynamically created Arabic TMP_FontAsset from Tahoma.ttf");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LocalizationManager] Dynamic TMP_FontAsset creation fallback: {ex.Message}");
                    }
                }
            }

            // 3. Inject into TMP_Settings global fallback list so ALL texts render Arabic characters seamlessly!
            if (cachedArabicFontAsset != null)
            {
                if (TMP_Settings.fallbackFontAssets == null)
                {
                    TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
                }
                if (!TMP_Settings.fallbackFontAssets.Contains(cachedArabicFontAsset))
                {
                    TMP_Settings.fallbackFontAssets.Add(cachedArabicFontAsset);
                }

                if (TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset != cachedArabicFontAsset)
                {
                    if (TMP_Settings.defaultFontAsset.fallbackFontAssetTable == null)
                    {
                        TMP_Settings.defaultFontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    }
                    if (!TMP_Settings.defaultFontAsset.fallbackFontAssetTable.Contains(cachedArabicFontAsset))
                    {
                        TMP_Settings.defaultFontAsset.fallbackFontAssetTable.Add(cachedArabicFontAsset);
                    }
                }
            }
        }

        private static readonly Dictionary<string, string> EnglishDict = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> ArabicDict = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> BuildingNamesAr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> BuildingDescriptionsAr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<PropertyType, string> PropertyTypesEn = new Dictionary<PropertyType, string>();
        private static readonly Dictionary<PropertyType, string> PropertyTypesAr = new Dictionary<PropertyType, string>();

        private static bool isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeIfNeeded();
            LoadSavedLanguage();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            InitializeIfNeeded();
            LoadSavedLanguage();
        }

        public static void InitializeIfNeeded()
        {
            if (isInitialized) return;
            isInitialized = true;

            EnsureRuntimeArabicFont();
            PopulateTranslations();
            PopulateBuildingTranslations();
        }

        private static void LoadSavedLanguage()
        {
            string saved = PlayerPrefs.GetString(PREF_KEY_LANGUAGE, "en");
            CurrentLanguage = saved.Equals("ar", StringComparison.OrdinalIgnoreCase) ? GameLanguage.Arabic : GameLanguage.English;
        }

        public static void SetLanguage(GameLanguage language)
        {
            if (CurrentLanguage == language && isInitialized) return;

            CurrentLanguage = language;
            PlayerPrefs.SetString(PREF_KEY_LANGUAGE, language == GameLanguage.Arabic ? "ar" : "en");
            PlayerPrefs.Save();

            Debug.Log($"[LocalizationManager] Language switched to: {language}");
            OnLanguageChanged?.Invoke();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == GameLanguage.English ? GameLanguage.Arabic : GameLanguage.English);
        }

        /// <summary>
        /// Retrieves localized text by key. Formats arguments if provided.
        /// If current language is Arabic, automatically shapes and reverses the string.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            InitializeIfNeeded();

            string text = null;
            if (CurrentLanguage == GameLanguage.Arabic)
            {
                if (!ArabicDict.TryGetValue(key, out text))
                {
                    EnglishDict.TryGetValue(key, out text);
                }
            }
            else
            {
                EnglishDict.TryGetValue(key, out text);
            }

            if (string.IsNullOrEmpty(text))
            {
                text = key; // Fallback to raw key
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    text = string.Format(text, args);
                }
                catch
                {
                    // Ignore format errors on mismatched arguments
                }
            }

            if (CurrentLanguage == GameLanguage.Arabic)
            {
                text = ArabicFixer.Fix(text);
            }

            return text;
        }

        /// <summary>
        /// Translates arbitrary Arabic string into shaped display form if currently in Arabic mode.
        /// </summary>
        public static string FormatRaw(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            return CurrentLanguage == GameLanguage.Arabic ? ArabicFixer.Fix(rawText) : rawText;
        }

        /// <summary>
        /// Applies text and seamlessly assigns the appropriate font asset (ArabicFont or EnglishFont).
        /// This permanently prevents missing glyph tofu boxes (▯) in all UI contexts.
        /// </summary>
        public static void ApplyTo(TMP_Text textComponent, string text)
        {
            if (textComponent == null) return;
            if (string.IsNullOrEmpty(text))
            {
                textComponent.text = string.Empty;
                return;
            }

            bool hasArabic = ArabicFixer.ContainsArabic(text);
            textComponent.text = hasArabic ? FormatRaw(text) : text;

            if (hasArabic)
            {
                var arFont = ArabicFont;
                if (arFont != null && textComponent.font != arFont)
                {
                    textComponent.font = arFont;
                    if (arFont.material != null)
                    {
                        textComponent.fontSharedMaterial = arFont.material;
                    }
                }
            }
            else
            {
                var enFont = EnglishFont;
                if (enFont != null && textComponent.font != enFont)
                {
                    textComponent.font = enFont;
                    if (enFont.material != null)
                    {
                        textComponent.fontSharedMaterial = enFont.material;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves localized text by key and applies it directly to the TMP_Text component with proper font.
        /// </summary>
        public static void ApplyLocalized(TMP_Text textComponent, string key, params object[] args)
        {
            if (textComponent == null) return;
            string text = Get(key, args);
            ApplyTo(textComponent, text);
        }

        /// <summary>
        /// Gets the localized building name for any property in the city.
        /// </summary>
        public static string GetBuildingName(string originalName, PropertyType type)
        {
            InitializeIfNeeded();
            if (CurrentLanguage == GameLanguage.English || string.IsNullOrEmpty(originalName))
            {
                return originalName;
            }

            if (BuildingNamesAr.TryGetValue(originalName.Trim(), out var arName))
            {
                return FormatRaw(arName);
            }

            // Fallback by property type
            if (PropertyTypesAr.TryGetValue(type, out var fallbackType))
            {
                return FormatRaw(fallbackType);
            }

            return originalName;
        }

        /// <summary>
        /// Gets the localized building description for any property in the city.
        /// </summary>
        public static string GetBuildingDescription(string originalDesc, PropertyType type)
        {
            InitializeIfNeeded();
            if (CurrentLanguage == GameLanguage.English || string.IsNullOrEmpty(originalDesc))
            {
                return originalDesc;
            }

            if (BuildingDescriptionsAr.TryGetValue(originalDesc.Trim(), out var arDesc))
            {
                return FormatRaw(arDesc);
            }

            return originalDesc;
        }

        /// <summary>
        /// Gets the localized category badge name for a PropertyType.
        /// </summary>
        public static string GetPropertyTypeName(PropertyType type)
        {
            InitializeIfNeeded();
            if (CurrentLanguage == GameLanguage.Arabic)
            {
                if (PropertyTypesAr.TryGetValue(type, out var ar))
                {
                    return FormatRaw(ar);
                }
            }
            else
            {
                if (PropertyTypesEn.TryGetValue(type, out var en))
                {
                    return en;
                }
            }

            return type.ToString().ToUpper();
        }

        private static void Add(string key, string en, string ar)
        {
            EnglishDict[key] = en;
            ArabicDict[key] = ar;
        }

        private static void PopulateBuildingTranslations()
        {
            // Category Badges
            PropertyTypesEn[PropertyType.RentalBuilding] = "RESIDENTIAL";
            PropertyTypesAr[PropertyType.RentalBuilding] = "سكني";

            PropertyTypesEn[PropertyType.SmallRentalBuilding] = "RESIDENTIAL";
            PropertyTypesAr[PropertyType.SmallRentalBuilding] = "سكني";

            PropertyTypesEn[PropertyType.Restaurant] = "DINING & FOOD";
            PropertyTypesAr[PropertyType.Restaurant] = "مطعم ومأكولات";

            PropertyTypesEn[PropertyType.Cafe] = "CAFE & DRINKS";
            PropertyTypesAr[PropertyType.Cafe] = "مقهى ومشروبات";

            PropertyTypesEn[PropertyType.TaxiStation] = "TRANSPORTATION";
            PropertyTypesAr[PropertyType.TaxiStation] = "نقل ومواصلات";

            PropertyTypesEn[PropertyType.Stadium] = "SPORTS ARENA";
            PropertyTypesAr[PropertyType.Stadium] = "منشأة رياضية";

            PropertyTypesEn[PropertyType.TennisCourt] = "SPORTS CLUB";
            PropertyTypesAr[PropertyType.TennisCourt] = "نادي رياضي";

            PropertyTypesEn[PropertyType.Factory] = "INDUSTRIAL";
            PropertyTypesAr[PropertyType.Factory] = "صناعي";

            PropertyTypesEn[PropertyType.OilStation] = "PETROLEUM & FUEL";
            PropertyTypesAr[PropertyType.OilStation] = "محطة وقود";

            PropertyTypesEn[PropertyType.Hospital] = "HEALTHCARE";
            PropertyTypesAr[PropertyType.Hospital] = "رعاية صحية";

            PropertyTypesEn[PropertyType.School] = "EDUCATION";
            PropertyTypesAr[PropertyType.School] = "تعليم";

            PropertyTypesEn[PropertyType.PoliceStation] = "CIVIC & SECURITY";
            PropertyTypesAr[PropertyType.PoliceStation] = "أمن وخدمات";

            PropertyTypesEn[PropertyType.Park] = "MUNICIPAL PARK";
            PropertyTypesAr[PropertyType.Park] = "حديقة عامة";

            PropertyTypesEn[PropertyType.Bank] = "FINANCIAL & BANKING";
            PropertyTypesAr[PropertyType.Bank] = "خدمات مصرفية";

            // All 20 building instances in the city scene
            BuildingNamesAr["Cafe 1"] = "مقهى 1";
            BuildingNamesAr["Cafe 2"] = "مقهى 2";
            BuildingNamesAr["Cafe 3"] = "مقهى 3";
            BuildingNamesAr["Fresh Town Supermarket"] = "سوبرماركت المدينة";
            BuildingNamesAr["Capital Industrial Manufacturing Plant #1"] = "مصنع العاصمة الصناعي 1";
            BuildingNamesAr["Capital Industrial Manufacturing Plant #2"] = "مصنع العاصمة الصناعي 2";
            BuildingNamesAr["City Central Taxi Station #1"] = "محطة تاكسي وسط المدينة";
            BuildingNamesAr["Metropolitan Police Department HQ"] = "مقر شرطة العاصمة";
            BuildingNamesAr["Eco Building Slope"] = "مجمع الواحة السكني - المنحدر";
            BuildingNamesAr["Eco Building Terrace"] = "مجمع الواحة السكني - التراس";
            BuildingNamesAr["Eco Building Grid"] = "مجمع الواحة السكني - الشبكة";
            BuildingNamesAr["Regular Building TwistedTower Large"] = "برج العاصمة السكني الملتوي";
            BuildingNamesAr["Capital General Hospital"] = "مستشفى العاصمة العام";
            BuildingNamesAr["Grand Central Municipal Park"] = "حديقة العاصمة المركزية";
            BuildingNamesAr["Metropolitan Oil & Petroleum Station #1"] = "محطة وقود العاصمة 1";
            BuildingNamesAr["Metropolitan Oil & Petroleum Station #2"] = "محطة وقود العاصمة 2";
            BuildingNamesAr["Rental House"] = "سكن الإيجار الحضري";
            BuildingNamesAr["Capital Tennis Club Court #1"] = "نادي العاصمة للتنس";
            BuildingNamesAr["Capital Public Academy"] = "أكاديمية العاصمة التعليمية";
            BuildingNamesAr["Grand Olympic Stadium"] = "استاد العاصمة الأولمبي";
            BuildingNamesAr["Capital National Bank"] = "بنك العاصمة الوطني";

            // All 14 building descriptions in the city scene
            BuildingDescriptionsAr["Popular urban coffeehouse. Visit to drink roasted espresso or food to replenish health, or buy the cafe for continuous income!"] =
                "مقهى حضري شهير. تفضل بزيارته لتناول القهوة أو الطعام لاستعادة صحتك، أو اشتر المقهى للحصول على دخل يومي مستمر!";

            BuildingDescriptionsAr["High-traffic neighborhood retail and grocery market supplying fresh goods and everyday essentials."] =
                "سوبرماركت تجاري حيوي يوفر المنتجات الطازجة والاحتياجات اليومية لسكان المدينة.";

            BuildingDescriptionsAr["Heavy manufacturing plant. Buy the factory for large-scale industrial output and periodic production revenue collections!"] =
                "منشأة تصنيع صناعي ثقيل. اشتر المصنع لإنتاج بضائع كبرى وجمع أرباح الإنتاج الدورية!";

            BuildingDescriptionsAr["Central taxi transit depot. Rent up to 8 taxis per day with roof markers to collect passenger fares on city roads, or purchase the station!"] =
                "محطة النقل المركزي للتاكسي. استأجر حتى 8 سيارات تاكسي يومياً تجمع أجرة الركاب في شوارع المدينة، أو اشتر المحطة بالكامل!";

            BuildingDescriptionsAr["Municipal law enforcement headquarters ensuring city safety. Public facility (not available for private purchase or flat leasing)."] =
                "المقر الرئيسي لإدارة شرطة العاصمة لضمان أمن وسلامة المدينة. منشأة عامة (غير معروضة للبيع الخاص أو تأجير الشقق).";

            BuildingDescriptionsAr["High-density residential high-rise. Purchase individual flats (25 flats available) to lease out for rent, or buy the entire building for massive daily revenue!"] =
                "برج سكني فاخر عالي الكثافة. يمكنك شراء شقق منفصلة وتأجيرها للسكان، أو شراء البرج بأكمله لتحقيق دخل يومي ضخم!";

            BuildingDescriptionsAr["Advanced metropolitan medical center. Visit anytime you are sick or low health for full medical treatment (restoring 100 HP), or buy the hospital!"] =
                "مركز طبي متطور في العاصمة. زره عند المرض أو انخفاض الصحة لتلقي الرعاية الشاملة واستعادة 100% من طاقتك، أو اشتر المستشفى بالكامل!";

            BuildingDescriptionsAr["Expansive scenic public park with walking trails and garden attractions. Buy this prestigious park to earn visitor entry fees from citizens walking through!"] =
                "حديقة بلدية فسيحة تضم مسارات مشي ومساحات خضراء. اشتر الحديقة لتحصيل رسوم دخول الزوار من المتنزهين!";

            BuildingDescriptionsAr["High-traffic fuel and petroleum station. Buy the oil station to earn regular revenue from citywide vehicle fuel sales!"] =
                "محطة توزيع وقود وبترول حيوية. اشتر محطة الوقود لكسب عوائد منتظمة من مبيعات الوقود لمركبات المدينة!";

            BuildingDescriptionsAr["Premier dining establishment. Visit to eat gourmet dishes or drink beverages to restore health and stamina, or acquire the restaurant!"] =
                "مطعم راقٍ يقدم أشهى الأطباق والمشروبات لاستعادة الصحة والنشاط، أو تملك المطعم كاستثمار تجاري رابح!";

            BuildingDescriptionsAr["Multi-unit residential property. Purchase individual flats (8 flats available) to lease out for rent, or buy the entire house!"] =
                "عقار سكني متعدد الوحدات. اشتر شققاً منفردة لتأجيرها، أو تملك المبنى بالكامل!";

            BuildingDescriptionsAr["Championship tennis courts with professional lighting. Buy the tennis court to receive lucrative tournament match hosting requests and earn match fees!"] =
                "ملاعب تنس احترافية مجهزة بإضاءة دولية. اشتر نادي التنس لاستقبال طلبات استضافة البطولات وكسب عوائد التأجير!";

            BuildingDescriptionsAr["Civic public school dedicated to city education. Public municipal facility (not available for private purchase or flat leasing)."] =
                "مدرسة تعليمية عامة تابعة لبلدية العاصمة. منشأة مدنية لخدمة المجتمع (غير معروضة للبيع الخاص).";

            BuildingDescriptionsAr["Massive sports and entertainment colosseum. Buy the stadium to receive football match hosting rental requests and earn huge rewards!"] =
                "استاد رياضي وترفيهي ضخم. اشتر الاستاد لتلقي عروض استضافة مباريات كرة القدم وتحقيق أرباح استثنائية!";

            BuildingDescriptionsAr["Capital National Bank. Create an account for 100 coins, deposit your funds securely, and earn high daily compound interest (10/30%)!"] =
                "بنك العاصمة الوطني. أنشئ حسابك المصرفي مقابل 100 عملة، وأودع أموالك بأمان لتحصل على أرباح فائدة يومية بنسبة (10/30%)!";
        }

        private static void PopulateTranslations()
        {
            // ==========================================
            // 1. MAIN MENU
            // ==========================================
            Add("menu_title", "CAPITAL KINGDOM", "المملكة الرأسمالية");
            Add("menu_subtitle", "BUILD YOUR REAL ESTATE EMPIRE", "ابنِ إمبراطوريتك العقارية");
            Add("menu_start_game", "START NEW GAME", "بدء لعبة جديدة");
            Add("menu_resume_game", "RESUME GAME", "استئناف اللعبة");
            Add("menu_options", "SETTINGS", "الإعدادات");
            Add("menu_about", "ABOUT", "حول اللعبة");
            Add("menu_exit", "EXIT GAME", "خروج");
            Add("menu_language", "LANGUAGE / اللغة", "اللغة / LANGUAGE");
            Add("menu_sound_volume", "SOUND VOLUME", "مستوى الصوت");
            Add("menu_close", "CLOSE", "إغلاق");
            Add("menu_language_en", "English", "الإنجليزية");
            Add("menu_language_ar", "العربية", "العربية");

            // Reset Warning Modal
            Add("modal_reset_title", "START NEW GAME?", "بدء لعبة جديدة؟");
            Add("modal_reset_message", "<b>Previous saved city progress found!</b>\n\nStarting a new game will <color=#FF4444>permanently erase</color> your existing city data, buildings, and properties, resetting your balance to <b>$5,000 coins</b>.\n\nAre you sure you want to proceed?",
                                       "<b>تم العثور على تقدم سابق محفوظ!</b>\n\nبدء لعبة جديدة سيؤدي إلى <color=#FF4444>مسح بيانات مدينتك الحالية والمباني والممتلكات بشكل دائم</color>، وإعادة تعيين رصيدك إلى <b>5,000 عملة</b>.\n\nهل أنت متأكد أنك تريد المتابعة؟");
            Add("modal_reset_confirm", "YES, START OVER ($5,000)", "نعم، ابدأ من جديد (5,000$)");
            Add("modal_reset_cancel", "CANCEL & KEEP CITY", "إلغاء والاحتفاظ بالمدينة");

            // ==========================================
            // 2. GAMEPLAY HEADER UI
            // ==========================================
            Add("header_day_badge", "<color=#FFD700>DAY {0} IN CITY</color>  <color=#556677>•</color>  <color=#FFFFFF>{1}</color>  <color=#556677>•</color>  <color=#70C5FF>{2}</color>",
                                    "<color=#FFD700>اليوم {0} في المدينة</color>  <color=#556677>•</color>  <color=#FFFFFF>{1}</color>  <color=#556677>•</color>  <color=#70C5FF>{2}</color>");
            Add("header_menu_btn", "MENU", "القائمة");
            Add("header_guide_btn", "GUIDE", "الدليل");
            Add("header_lang_btn", "EN/AR", "عربي/EN");
            Add("header_notice_title", "CITY OCCURRENCES", "أحداث المدينة");
            Add("header_notice_alerts", "{0} ALERTS", "{0} تنبيهات");
            Add("header_notice_clear", "ALL CLEAR", "كل شيء هادئ");
            Add("header_notice_empty", "No current city alerts. All operations nominal.", "لا توجد تنبيهات حالية. جميع العمليات تسير بشكل طبيعي.");

            // ==========================================
            // 3. PROPERTY DETAIL MODAL - GENERAL
            // ==========================================
            Add("prop_modal_title", "PROPERTY DETAILS", "تفاصيل العقار");
            Add("prop_price_heading", "PRICE", "السعر");
            Add("prop_income_heading", "DAILY INCOME", "الدخل اليومي");
            Add("prop_close_btn", "CLOSE", "إغلاق");

            Add("prop_civic_price", "N/A (CIVIC)", "غير معروض (منشأة عامة)");
            Add("prop_civic_income", "PUBLIC UTILITY", "خدمة عامة");
            Add("prop_daily_rent_per_day", "+${0:N0} / day", "+{0:N0}$ / يومياً");

            Add("prop_buy_button", "PURCHASE PROPERTY", "شراء العقار");
            Add("prop_owned_status", "OWNED", "مملوك");
            Add("prop_for_sale_status", "FOR SALE", "للبيع");
            Add("prop_civic_status", "CIVIC PUBLIC FACILITY", "منشأة عامة للمدينة");
            Add("prop_owned_by_you", "OWNED BY YOU", "مملوك لك");
            Add("prop_flats_owned_status", "YOU OWN {0}/{1} FLATS", "أنت تملك {0}/{1} شقق");
            Add("prop_available_sale", "AVAILABLE FOR SALE", "متاح للبيع");
            Add("prop_not_for_sale", "CIVIC FACILITY (NOT FOR SALE)", "منشأة عامة (ليست للبيع)");
            Add("prop_already_owned", "ALREADY OWNED", "مملوك بالفعل");
            Add("prop_buy_button_cash", "BUY PROPERTY (${0:N0})", "شراء العقار ({0:N0}$)");
            Add("prop_buy_rest_button", "BUY REST OF BUILDING (${0:N0})", "شراء باقي المبنى ({0:N0}$)");
            Add("prop_insufficient_funds_btn", "INSUFFICIENT FUNDS (${0:N0})", "رصيد غير كافٍ ({0:N0}$)");
            Add("prop_collect_rent_amount", "COLLECT RENT (+${0:N0})", "تحصيل الإيجار (+{0:N0}$)");
            Add("prop_rent_collected_today", "RENT COLLECTED TODAY", "تم تحصيل إيجار اليوم");
            Add("prop_no_rent_due", "NO RENT DUE", "لا يوجد إيجار مستحق");
            Add("prop_price_label", "PRICE:", "السعر:");
            Add("prop_income_label", "DAILY INCOME:", "الدخل اليومي:");
            Add("prop_collect_rent", "COLLECT RENT", "تحصيل الإيجار");
            Add("prop_rent_ready", "RENT READY!", "الإيجار جاهز!");
            Add("prop_rent_in", "Next Rent in: {0}", "الإيجار القادم خلال: {0}");
            Add("prop_insufficient_funds", "Insufficient coins to purchase this property!", "العملات غير كافية لشراء هذا العقار!");
            Add("prop_purchased_success", "Congratulations! You acquired {0}!", "تهانينا! لقد اشتريت {0}!");
            Add("prop_rent_collected", "Collected ${0:N0} in rent from {1}!", "تم تحصيل {0:N0}$ إيجار من {1}!");

            // ==========================================
            // 4. FLATS / RESIDENTIAL UNITS
            // ==========================================
            Add("flat_title", "LUXURY RESIDENTIAL APARTMENTS & SUITES", "شقق وأجنحة سكنية فاخرة");
            Add("flat_stats_label", "Flats Owned: {0}/{1}  |  Leased: {2}/{0}", "الشقق المملوكة: {0}/{1}  |  المؤجرة: {2}/{0}");
            Add("flat_income_label", "+${0:N0} / day from leased flats", "+{0:N0}$ / يومياً من الشقق المؤجرة");
            Add("flat_buy_unit_btn", "BUY FLAT #{0} (${1:N0})", "شراء الشقة #{0} ({1:N0}$)");
            Add("flat_lease_unit_btn", "LEASE FLAT #{0} (+${1:N0}/day)", "تأجير الشقة #{0} (+{1:N0}$/يوم)");
            Add("flat_all_owned", "ALL FLATS OWNED", "جميع الشقق مملوكة");
            Add("flat_buy_first", "BUY A FLAT FIRST", "اشترِ شقة أولاً");
            Add("flat_all_leased", "ALL FLATS LEASED", "جميع الشقق مؤجرة");
            Add("flat_rent_out_btn", "RENT OUT FLAT (+${0:N0}/day)", "تأجير شقة (+{0:N0}$/يوم)");

            // ==========================================
            // 5. CAFE & RESTAURANT DINING
            // ==========================================
            Add("dining_vip_status", "VIP Owner: Food (+{0:0} HP) & Drinks (+{1:0} HP) are FREE! Replenish your health anytime.", "مالك VIP: الطعام (+{0:0} صحة) والمشروبات (+{1:0} صحة) مجانية تماماً! استعد طاقتك في أي وقت.");
            Add("dining_regular_status", "Replenish Health! Food restores +{0:0} HP, Drinks restore +{1:0} HP.", "استعد طاقتك وصحتك! الطعام يعوض +{0:0} صحة، والمشروبات تعوض +{1:0} صحة.");
            Add("dining_drink_free", "DRINK (+{0:0} HP, FREE)", "شرب (+{0:0} صحة، مجاني)");
            Add("dining_drink_paid", "DRINK (+{0:0} HP, ${1:N0})", "شرب (+{0:0} صحة، {1:N0}$)");
            Add("dining_eat_free", "EAT (+{0:0} HP, FREE)", "تناول طعام (+{0:0} صحة، مجاني)");
            Add("dining_eat_paid", "EAT (+{0:0} HP, ${1:N0})", "تناول طعام (+{0:0} صحة، {1:N0}$)");

            // ==========================================
            // 6. TAXI STATION
            // ==========================================
            Add("taxi_title", "CAPITAL TAXI FLEET HEADQUARTERS", "المقر الرئيسي لأسطول سيارات الأجرة");
            Add("taxi_fee_label", "Rental Fee: -${0:N0} / taxi", "رسوم الاستئجار: -{0:N0}$ / لكل تاكسي");
            Add("taxi_fares_range", "Random Fares: ~$35 - $110 per trip", "أجرة عشوائية: ~35$ - 110$ لكل رحلة");
            Add("taxi_active_fleet", "Active Fleet: {0} / {1} Taxis on Road", "الأسطول النشط: {0} / {1} تاكسي في الشوارع");
            Add("taxi_rented_status", "Rented Taxis: {0} / {1} operating on city roads", "سيارات التاكسي المستأجرة: {0} / {1} تعمل حالياً في شوارع المدينة");
            Add("taxi_max_active", "MAX {0} TAXIS ACTIVE TODAY", "الحد الأقصى {0} سيارات تاكسي نشطة اليوم");
            Add("taxi_rent_btn", "RENT TAXI #{0} (${1:N0}) -> Dispatches to Road", "استئجار تاكسي #{0} ({1:N0}$) -> انطلاق للشوارع");
            Add("taxi_need_cash", "NEED ${0:N0} TO RENT TAXI", "تحتاج إلى {0:N0}$ لاستئجار تاكسي");

            // ==========================================
            // 7. STADIUM & TENNIS COURT
            // ==========================================
            Add("stadium_match_term", "Matches Hosted", "المباريات المستضافة");
            Add("tennis_match_term", "Tournaments Hosted", "البطولات المستضافة");
            Add("stadium_stats_label", "{0}: {1}  |  Total Match Rent: ${2:N0}", "{0}: {1}  |  إجمالي عوائد التأجير: {2:N0}$");
            Add("stadium_request_offer_btn", "REQUEST MATCH RENT OFFER NOW", "طلب عرض تأجير مباراة الآن");
            Add("stadium_buy_to_host_btn", "BUY {0} TO HOST MATCHES", "اشتر {0} لاستضافة المباريات");
            Add("stadium_venue_term", "STADIUM", "الاستاد");
            Add("tennis_venue_term", "TENNIS COURT", "ملعب التنس");

            // ==========================================
            // 8. HOSPITAL HEALTHCARE
            // ==========================================
            Add("hosp_title", "CAPITAL CITY HOSPITAL & MEDICAL CENTER", "مستشفى ومركز العاصمة الطبي");
            Add("hosp_healthy_status", "<color=#44FF88>HEALTHY & FIT (100% HP)</color>\nVital signs optimal. Hospital emergency facilities standing by (No treatment needed).",
                                       "<color=#44FF88>بصحة ممتازة (100% صحة)</color>\nالعلامات الحيوية ممتازة. طوارئ المستشفى على أهبة الاستعداد (لا يلزم علاج حالياً).");
            Add("hosp_owner_status", "<color={0}>CONDITION: {1}</color>\n{2} (Health: {3:0}/{4:0} HP).\n<color=#44FF88>As Hospital Owner, VIP emergency medical care is COMPLIMENTARY (FREE)!</color>",
                                     "<color={0}>الحالة: {1}</color>\n{2} (الصحة: {3:0}/{4:0} نقطة).\n<color=#44FF88>بصفتك مالك المستشفى، العلاج الطبي لحالات الطوارئ VIP مجاني بالكامل!</color>");
            Add("hosp_treatment_cost_status", "<color={0}>CONDITION: {1}</color>\n{2} (Health: {3:0}/{4:0} HP).\nTreatment Fee: ${5:N0} (Restores health to 100 HP).",
                                              "<color={0}>الحالة: {1}</color>\n{2} (الصحة: {3:0}/{4:0} نقطة).\nتكلفة العلاج: {5:N0}$ (استعادة كامل الصحة إلى 100%).");
            Add("hosp_healthy_btn", "HEALTHY (NO TREATMENT NEEDED)", "بصحة جيدة (لا يلزم علاج)");
            Add("hosp_vip_treat_btn", "RECEIVE VIP MEDICAL TREATMENT (FREE)", "تلقي العلاج الطبي VIP (مجاناً)");
            Add("hosp_pay_treat_btn", "PAY FOR HOSPITAL TREATMENT (${0:N0})", "دفع تكلفة العلاج الطبي ({0:N0}$)");
            Add("hosp_need_cash_treat", "NEED ${0:N0} FOR TREATMENT", "تحتاج إلى {0:N0}$ للعلاج");

            // ==========================================
            // 9. FACTORY INDUSTRIAL
            // ==========================================
            Add("fact_title", "HEAVY INDUSTRIAL MANUFACTURING PLANT", "مصنع التصنيع الصناعي الثقيل");
            Add("fact_stats_label", "Production Batches Completed: {0}  |  Total Revenue: ${1:N0}\nReady to Collect: +${2:N0}",
                                    "دفعات الإنتاج المكتملة: {0}  |  إجمالي الأرباح: {1:N0}$\nجاهز للتحصيل: +{2:N0}$");
            Add("fact_collect_btn", "COLLECT PRODUCTION REVENUE (+${0:N0})", "تحصيل أرباح الإنتاج (+{0:N0}$)");
            Add("fact_none_ready", "NO PRODUCTION EARNINGS READY YET", "لا توجد أرباح إنتاج جاهزة للتحصيل حالياً");
            Add("fact_buy_to_earn", "BUY FACTORY TO EARN INDUSTRIAL REVENUE", "اشتر المصنع لكسب عوائد الإنتاج الصناعي");

            // ==========================================
            // 10. OIL PETROLEUM STATION
            // ==========================================
            Add("oil_title", "METROPOLITAN OIL & PETROLEUM STATION", "محطة وقود وبترول العاصمة");
            Add("oil_stats_label", "Fuel Sales Active across city roads.\nAccumulated Oil Revenue: +${0:N0}",
                                   "مبيعات الوقود نشطة عبر شوارع المدينة.\nعوائد الوقود المتراكمة: +{0:N0}$");
            Add("oil_collect_btn", "COLLECT FUEL EARNINGS (+${0:N0})", "تحصيل عوائد الوقود (+{0:N0}$)");
            Add("oil_none_ready", "NO FUEL REVENUE ACCUMULATED YET", "لم تتراكم عوائد وقود للتحصيل حتى الآن");
            Add("oil_buy_to_earn", "BUY OIL STATION TO EARN FUEL REVENUE", "اشتر محطة الوقود لكسب عوائد مبيعات الوقود");

            // ==========================================
            // 11. MUNICIPAL PARK
            // ==========================================
            Add("park_title", "GRAND CENTRAL MUNICIPAL PARK", "حديقة العاصمة المركزية الكبرى");
            Add("park_stats_label", "Visitors Admitted: {0} citizens  |  Total Revenue: ${1:N0}\nEntry Fees Available to Collect: +${2:N0}",
                                    "عدد الزوار الوافدين: {0} مواطن  |  إجمالي الإيرادات: {1:N0}$\nرسوم الدخول المتاحة للتحصيل: +{2:N0}$");
            Add("park_collect_btn", "COLLECT PARK ENTRY FEES (+${0:N0})", "تحصيل رسوم دخول الحديقة (+{0:N0}$)");
            Add("park_none_ready", "NO ENTRY FEES TO COLLECT YET", "لا توجد رسوم دخول متاحة للتحصيل الآن");
            Add("park_buy_to_earn", "BUY PARK TO EARN VISITOR ENTRY FEES", "اشتر الحديقة لتحصيل رسوم دخول الزوار");

            // ==========================================
            // 12. BANKING & INTEREST
            // ==========================================
            Add("bank_title", "CAPITAL NATIONAL BANK & SAVINGS", "بنك العاصمة الوطني والمدخرات");
            Add("bank_notice_title", "BANK INTEREST", "فوائد البنك");
            Add("bank_status_closed", "Open a personal Bank Account for 100 coins fee to deposit funds and earn daily interest (10/30%)!", "أنشئ حساباً مصرفياً شخصياً برسم 100 عملة لإيداع أموالك وكسب فائدة يومية بنسبة (10/30%)!");
            Add("bank_status_active", "Account Active  •  Balance: <b><color=#70FF70>${0:N0}</color></b>  •  Total Interest: <color=#FFD700>+${1:N0}</color> (+0.33% Daily)",
                                      "الحساب نشط  •  الرصيد: <b><color=#70FF70>{0:N0}$</color></b>  •  إجمالي الأرباح: <color=#FFD700>+{1:N0}$</color> (+0.33٪ يومياً)");
            Add("bank_open_account_btn", "CREATE BANK ACCOUNT (100 COINS)", "إنشاء حساب بنكي (100 عملة)");
            Add("bank_need_coins_open", "NEED 100 COINS TO OPEN ACCOUNT", "تحتاج 100 عملة لفتح الحساب");
            Add("bank_placeholder", "Enter coin amount...", "أدخل مبلغ العملات...");
            Add("bank_deposit_btn", "DEPOSIT", "إيداع");
            Add("bank_withdraw_btn", "WITHDRAW", "سحب");
            Add("bank_withdraw_all_btn", "WITHDRAW ALL", "سحب الكل");
            Add("bank_all_label", "ALL", "الكل");

            Add("bank_already_active", "You already have an active bank account with Capital National Bank!", "لديك بالفعل حساب بنكي نشط في بنك العاصمة الوطني!");
            Add("bank_need_account_first", "You must first open a bank account (100 coin fee) before banking!", "يجب عليك أولاً فتح حساب بنكي (رسوم 100 عملة)!");
            Add("bank_opened_success", "Bank account opened successfully! Paid 100 coins fee.", "تم فتح الحساب البنكي بنجاح! تم دفع 100 عملة رسوم.");
            Add("bank_fee_insufficient", "You need at least 100 coins to open a bank account!", "تحتاج إلى 100 عملة على الأقل لفتح حساب بنكي!");
            Add("bank_deposit_success", "Deposited ${0:N0} into your bank account.", "تم إيداع {0:N0}$ في حسابك البنكي.");
            Add("bank_withdraw_success", "Withdrew ${0:N0} from your bank account.", "تم سحب {0:N0}$ من حسابك البنكي.");
            Add("bank_deposit_insufficient", "Insufficient cash in pocket to deposit this amount!", "النقود المتوفرة معك غير كافية لهذا المبلغ!");
            Add("bank_withdraw_insufficient", "Insufficient balance in your bank account!", "رصيد حسابك البنكي غير كافٍ!");
            Add("bank_invalid_amount", "Please enter a valid coin amount!", "يرجى إدخال مبلغ صحيح من العملات!");
            Add("bank_interest_credited", "Bank daily interest of ${0:N0} credited to your account!", "تمت إضافة فائدة بنكية يومية قدرها {0:N0}$ إلى حسابك!");

            // ==========================================
            // 13. MAINTENANCE & REPAIRS
            // ==========================================
            Add("maint_title", "PROPERTY MAINTENANCE & REPAIR", "صيانة وإصلاح الممتلكات");
            Add("maint_required_title", "MAINTENANCE REQUIRED: -${0:N0}", "يلزم إجراء صيانة: -{0:N0}$");
            Add("maint_pay_button", "PAY MAINTENANCE (-${0:N0})", "دفع الصيانة (-{0:N0}$)");
            Add("maint_insufficient_funds", "INSUFFICIENT FUNDS (-${0:N0})", "رصيد غير كافٍ (-{0:N0}$)");

            // ==========================================
            // 14. 3D WORLD FLOATING INDICATORS
            // ==========================================
            Add("ind_collect", "COLLECT ${0:N0}", "تحصيل {0:N0}$");
            Add("ind_maint", "MAINTENANCE: -${0:N0}", "صيانة: -{0:N0}$");
            Add("ind_match_ready", "MATCH READY!", "مباراة جاهزة!");
            Add("ind_match_offer", "MATCH +${0:N0}!", "مباراة +{0:N0}$!");
            Add("ind_prod", "PROD +${0:N0}", "إنتاج +{0:N0}$");
            Add("ind_oil", "OIL +${0:N0}", "وقود +{0:N0}$");
            Add("ind_park", "PARK +${0:N0}", "حديقة +{0:N0}$");
            Add("ind_my_restaurant", "MY RESTAURANT", "مطعمي");
            Add("ind_my_cafe", "MY CAFE", "مقهى خاص بي");
            Add("ind_my_stadium", "MY STADIUM", "استادي الخاص");
            Add("ind_my_clinic", "MY CLINIC", "مستشفاي الخاص");
            Add("ind_my_factory", "MY FACTORY", "مصنعي الخاص");
            Add("ind_my_station", "MY STATION", "محطتي الخاصة");
            Add("ind_my_park", "MY PARK", "حديقتي الخاصة");
            Add("ind_owned", "OWNED", "مملوك لك");
            Add("ind_flats_count", "FLATS: {0}/{1}", "الشقق: {0}/{1}");
            Add("ind_civic_school", "CIVIC: SCHOOL", "مدرسة عامة");
            Add("ind_civic_police", "CIVIC: POLICE", "مقر الشرطة");
            Add("ind_civic_hospital", "CIVIC: HOSPITAL", "مستشفى عام");

            // ==========================================
            // 15. NOTICE BAR OCCURRENCES
            // ==========================================
            Add("notice_bank_interest_title", "BANK INTEREST CREDITED", "إيداع الفائدة البنكية");
            Add("notice_bank_interest_desc", "Earned +${0:N0} compound interest credited to your bank account.", "حصلت على +{0:N0}$ فائدة مركبة أودعت في حسابك البنكي.");
            Add("notice_match_req_title", "MATCH RENT OFFER", "عرض استضافة مباراة");
            Add("notice_match_req_desc", "Club offers ${0:N0} venue rent to host tournament match at {1}!", "النادي يعرض {0:N0}$ كأجرة استضافة مباراة في {1}!");
            Add("notice_maint_title", "MAINTENANCE OVERDUE", "تأخر موعد الصيانة");
            Add("notice_maint_desc", "{0} requires structural maintenance (-${1:N0})!", "{0} بحاجة ماسة للصيانة الهيكلية (-{1:N0}$)!");
            Add("notice_health_title", "CRITICAL HEALTH ALERT", "تحذير صحي حرج");
            Add("notice_health_desc", "Mayor health is low! Visit Capital General Hospital or dine at a restaurant to replenish HP.", "صحة العمدة منخفضة! زر مستشفى العاصمة أو تناول الطعام لاستعادة طاقتك.");

            // ==========================================
            // 16. DAILY REPORT MODAL
            // ==========================================
            Add("report_title", "DAILY FINANCIAL REPORT - DAY {0}", "التقرير المالي اليومي - اليوم {0}");
            Add("report_gross", "Gross Revenue: +${0:N0}", "إجمالي الإيرادات: +{0:N0}$");
            Add("report_operating", "Operating Costs: -${0:N0}", "تكاليف التشغيل: -{0:N0}$");
            Add("report_repair", "Maintenance Costs: -${0:N0}", "تكاليف الصيانة: -{0:N0}$");
            Add("report_net", "Net Cash Flow: {0}${1:N0}", "صافي التدفق النقدي: {0}{1:N0}$");
            Add("report_cash", "Pocket Cash: ${0:N0}", "النقد المتوفر: {0:N0}$");
            Add("report_networth", "Total Net Worth: ${0:N0}", "إجمالي صافي الثروة: {0:N0}$");
            Add("report_lesson", "Financial Lesson: {0}", "الدرس المالي: {0}");
            Add("report_lesson_header", "FINANCIAL LITERACY INSIGHT", "نصيحة الذكاء المالي");
            Add("report_continue_btn", "CONTINUE TO NEXT DAY", "المتابعة لليوم التالي");

            // ==========================================
            // 17. STADIUM MATCH REQUEST MODAL
            // ==========================================
            Add("stadium_modal_title", "MATCH RENTAL INVITATION", "دعوة استضافة مباراة");
            Add("stadium_modal_offer_label", "Offered Venue Rent: +${0:N0}", "أجرة الاستضافة المعروضة: +{0:N0}$");
            Add("stadium_modal_desc", "A premier sports league is requesting to rent your venue ({0}) for an exhibition match. Accept to collect instant venue rent!",
                                      "رابطة رياضية كبرى تطلب استئجار منشأتك ({0}) لإقامة مباراة رسمية. وافق لتحصيل قيمة التأجير فوراً!");
            Add("stadium_modal_accept_btn", "ACCEPT & COLLECT (+${0:N0})", "قبول وتحصيل (+{0:N0}$)");
            Add("stadium_modal_decline_btn", "DECLINE MATCH", "رفض العرض");

            // ==========================================
            // 17B. PLAYER HEALTH & VITALITY
            // ==========================================
            Add("hp_sick", "SICK!", "مريض!");
            Add("hp_critical", "CRITICAL", "حرج!");
            Add("hp_hungry", "HUNGRY", "جائع!");
            Add("hp_ill_display", "ILL: {0}/{1} HP", "مريض: {0}/{1} طاقة");
            Add("hp_normal_display", "{0}/{1} HP", "{0}/{1} طاقة");

            // ==========================================
            // 17C. ASSET SHOP & DETAIL PANEL
            // ==========================================
            Add("asset_category", "Category: {0}", "الفئة: {0}");
            Add("asset_condition", "Condition: {0:F0}%", "الحالة الفنية: {0:F0}%");
            Add("asset_gross_rev", "Gross Revenue: +${0:N0}/day", "إجمالي الإيرادات: +{0:N0}$/يوم");
            Add("asset_op_cost", "Operating Costs: -${0:N0}/day", "تكاليف التشغيل: -{0:N0}$/يوم");
            Add("asset_net_flow", "Net Cash Flow: {0}${1:N0}/day", "صافي التدفق النقدي: {0}{1:N0}$/يوم");
            Add("asset_payback", "Estimated Payback: {0}", "فترة الاسترداد المقدرة: {0}");
            Add("asset_buy_btn", "Buy (${0:N0})", "شراء ({0:N0}$)");
            Add("asset_repair_btn", "Repair (${0:N0})", "صيانة ({0:N0}$)");
            Add("asset_repaired", "Repaired (100%)", "تمت الصيانة (100%)");
            Add("asset_sell_btn", "Sell (${0:N0})", "بيع ({0:N0}$)");

            Add("shop_gross", "Gross: +${0:N0}/day", "الإجمالي: +{0:N0}$/يوم");
            Add("shop_upkeep", "Upkeep: -${0:N0}/day", "الصيانة: -{0:N0}$/يوم");
            Add("shop_net", "Net: {0}${1:N0}/day", "الصافي: {0}{1:N0}$/يوم");
            Add("shop_payback", "Payback: {0}", "الاسترداد: {0}");
            Add("shop_price", "${0:N0}", "{0:N0}$");

            // ==========================================
            // 18. TUTORIAL / GUIDE
            // ==========================================
            Add("tut_welcome_title", "WELCOME TO CAPITAL KINGDOM", "مرحباً بك في المملكة الرأسمالية");
            Add("tut_welcome_desc", "Greetings Mayor! You have arrived in Capital Kingdom with $5,000 starting capital.\n\nYour mission is to build a massive financial real estate empire! Acquire properties, purchase residential flats, collect rent, run factories, deploy taxis, and manage your health to conquer the city.",
                                   "تحياتنا يا سيادة العمدة! لقد وصلت إلى المملكة الرأسمالية مع 5,000$ كرأس مال ابتدائي.\n\nمهمتك هي بناء إمبراطورية عقارية ضخمة! اشترِ العقارات، واستحوذ على الشقق السكنية، وحصّل الإيجارات، وشغّل المصانع، وأطلق سيارات الأجرة، واعتنِ بصحتك لتسيطر على المدينة.");

            Add("tut_camera_title", "NAVIGATING THE METROPOLIS", "التنقل في المدينة");
            Add("tut_camera_desc", "Use <b>W, A, S, D</b> or arrow keys to pan the camera across the skyline.\n\nHold <b>Right Mouse Button</b> to rotate your isometric perspective, and use the <b>Scroll Wheel</b> to zoom in on bustling avenues or out for a grand strategic overview.",
                                   "استخدم مفاتيح <b>W, A, S, D</b> أو الأسهم لتحريك الكاميرا في أرجاء المدينة.\n\nاضغط مع الاستمرار على <b>زر الفأرة الأيمن</b> لتدوير المنظور، واستخدم <b>عجلة الفأرة</b> للتكبير أو التصغير لرؤية بانورامية شاملة.");

            Add("tut_properties_title", "ACQUIRING PRIME PROPERTIES", "شراء العقارات المميزة");
            Add("tut_properties_desc", "Click on any building across the districts to inspect its deed and financial profile.\n\nPurchase commercial stores, hotels, oil rigs, and factories to generate steady recurring daily cashflow. Look for the floating cash icons indicating rent ready for pickup!",
                                       "انقر على أي مبنى في المدينة لمعاينة تفاصيله وملفه المالي.\n\nاشترِ المتاجر والفنادق ومنشآت النفط والمصانع لتوليد تدفق نقدي يومي مستمر. ابحث عن أيقونات النقد العائمة التي تشير إلى جاهزية الإيجار للتحصيل!");

            Add("tut_bank_title", "NATIONAL BANK & COMPOUND SAVINGS", "البنك الوطني والادخار المركب");
            Add("tut_bank_desc", "Visit Capital National Bank to open a savings account for a small 100 coin fee.\n\nDeposit your surplus earnings to protect your capital and earn <b>0.33% daily compound interest</b> that grows your wealth automatically every single morning!",
                                   "زر بنك العاصمة الوطني لفتح حساب توفير مقابل رسوم بسيطة قدرها 100 عملة.\n\nأودع أرباحك الإضافية لحماية أموالك وكسب <b>فائدة مركبة يومية بنسبة 0.33٪</b> تنمي ثروتك تلقائياً كل صباح!");

            Add("tut_flats_title", "RESIDENTIAL APARTMENTS & UPGRADES", "الشقق السكنية والترقيات");
            Add("tut_flats_desc", "Expand residential buildings by adding individual luxury apartments.\n\nEach apartment you construct attracts new tenants, dramatically boosting your daily rent yields. Watch your profits soar as your residential towers reach full occupancy!",
                                   "وسّع المباني السكنية بإضافة شقق فاخرة جديدة.\n\nكل شقة تبنيها تجذب مستأجرين جدداً، مما يضاعف عائدات الإيجار اليومية. شاهد أرباحك تنمو كلما امتلأت الأبراج بالسكان!");

            Add("tut_health_title", "HEALTH, VITALITY & EMERGENCY CARE", "الصحة والطاقة والرعاية الطبية الطارئة");
            Add("tut_health_desc", "Managing an empire is demanding! Watch your Health and energy meter on the top bar as hunger and stamina drain.\n\nDine at local Restaurants and Cafes for hearty meals to restore HP. If your health drops critically or you faint, Capital Hospital will admit you for emergency stabilization—always keep cash reserves for healthcare!",
                                   "إدارة الإمبراطورية تتطلب جهداً كبيراً! راقب مقياس صحتك وطاقتك في الشريط العلوي.\n\nتناول الطعام والشراب في المطاعم والمقاهي لاستعادة طاقتك. إذا تدهورت صحتك أو أغمي عليك، سيتم نقلك لمستشفى العاصمة لتلقي العلاج الطارئ، فاحرص دائماً على الاحتفاظ باحتياطي مالي للطوارئ الطبية!");

            Add("tut_industry_title", "INDUSTRIAL PLANTS & OIL REFINERIES", "المصانع ومنشآت النفط");
            Add("tut_industry_desc", "Diversify beyond real estate into industrial manufacturing and energy production!\n\nHeavy Industrial Plants fabricate commercial goods batches, while Offshore Oil Rigs extract crude petroleum barrels. Collect both regularly to generate substantial production revenues to accelerate your capital expansion.",
                                     "نوّع استثماراتك إلى ما هو أبعد من العقارات عبر التصنيع الصناعي وإنتاج الطاقة!\n\nتنتج المصانع الصناعية دفعات من البضائع التجارية، بينما تستخرج منصات النفط براميل النفط الخام. حصّل عوائدها بانتظام لتوليد أرباح إنتاجية ضخمة تسهم في تسريع نمو ثروتك.");

            Add("tut_taxi_title", "CITY TAXI FLEET & LOGISTICS", "أسطول سيارات الأجرة والخدمات اللوجستية");
            Add("tut_taxi_desc", "Step into urban transit! Visit the Taxi Station headquarters to pay a daily fleet rental fee and dispatch city cabs.\n\nYour taxis patrol city avenues picking up waiting passengers, earning steady fare revenue throughout the day. Balance operating rental costs against gross fare revenues to maximize net logistics profit!",
                                 "ادخل عالم النقل الحضري! زر مقر محطة سيارات الأجرة لدفع رسوم تأجير الأسطول وإطلاق سيارات الأجرة.\n\nتجوب سياراتك شوارع المدينة لنقل الركاب وتحصيل الأجور على مدار اليوم. وازن بين تكاليف التشغيل وإجمالي الإيرادات لتحقيق أعلى أرباح لوجستية صافية!");

            Add("tut_stadium_title", "SPORTS ARENAS & EXHIBITION MATCHES", "الملاعب واستضافة البطولات الرياضية");
            Add("tut_stadium_desc", "Unlock high-yield event hosting! The Grand Stadium and Tennis Courts receive lucrative match and tournament booking invitations.\n\nWhen a match notice appears, inspect the arena and accept the exhibition hosting contract to earn a massive lump-sum venue rent payout and boost civic excitement!",
                                    "استفد من استضافة الفعاليات الرياضية الكبرى! يتلقى الاستاد الكبير وملاعب التنس دعوات مدفوعة لاستضافة المباريات والبطولات.\n\nعند ظهور إشعار المباراة، عاين الملعب واقبل عقد الاستضافة لتحصيل دفعات مالية ضخمة فورية وإنعاش النشاط في المدينة!");

            Add("tut_alerts_title", "MAINTENANCE, SINKING FUNDS & ALERTS", "الصيانة وصناديق الطوارئ والتنبيهات");
            Add("tut_alerts_desc", "Real estate structures suffer natural wear and tear! The City Notice Bar in the top-right corner will flag buildings requiring urgent structural repairs.\n\nPay maintenance orders promptly to protect building integrity and keep daily cashflows flowing. Smart investors always maintain an emergency reserve fund for unforeseen repair expenses.",
                                   "تتعرض المباني العقارية للاستهلاك الطبيعي بمرور الوقت! سينبهك شريط أحداث المدينة في الزاوية العلوية اليمنى بالمباني التي تحتاج لصيانة عاجلة.\n\nادفع تكاليف الصيانة فوراً للحفاظ على كفاءة المبنى واستمرار تدفق الإيجارات. المستثمر الذكي يحتفظ دائماً بصندوق طوارئ لتغطية نفقات الصيانة غير المتوقعة.");

            Add("tut_lang_title", "BILINGUAL LOCALIZATION & DUAL LANGUAGE", "الترجمة ثنائية اللغة ودعم اللغات");
            Add("tut_lang_desc", "Capital Kingdom features full bilingual support for English and Arabic!\n\nSwitch languages instantly at any time using the quick language toggle button on the HUD header or in the Main Menu Settings. All property portfolios, banking ledgers, health alerts, and financial metrics adapt automatically with right-to-left Arabic typography support.",
                                 "تتميز المملكة الرأسمالية بدعم كامل وثنائي لكل من اللغتين الإنجليزية والعربية!\n\nيمكنك تبديل اللغة فوراً في أي وقت باستخدام زر التبديل السريع في شريط الرأس أو من إعدادات القائمة الرئيسية. تتكيف جميع محافظ العقارات وسجلات البنوك وتنبيهات الصحة والمقاييس المالية تلقائياً مع دعم اتجاه الكتابة من اليمين إلى اليسار.");

            Add("tut_btn_next", "NEXT", "التالي");
            Add("tut_btn_back", "BACK", "السابق");
            Add("tut_btn_skip", "SKIP GUIDE", "تخطي الدليل");
            Add("tut_btn_finish", "START PLAYING", "ابدأ اللعب");
            Add("tut_step_count", "STEP {0} OF {1}", "الخطوة {0} من {1}");
        }
    }
}
