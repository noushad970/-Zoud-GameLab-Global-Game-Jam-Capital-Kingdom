using System;
using System.Collections.Generic;
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

        public static TMP_FontAsset EnglishFont => Instance != null ? Instance.englishFont : null;
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

        private static void Add(string key, string en, string ar)
        {
            EnglishDict[key] = en;
            ArabicDict[key] = ar;
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
            // 3. PROPERTY DETAIL MODAL
            // ==========================================
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
            // 4. BANK SYSTEM SECTION
            // ==========================================
            Add("bank_title", "CAPITAL NATIONAL BANK & SAVINGS", "بنك العاصمة الوطني والمدخرات");
            Add("bank_notice_title", "BANK INTEREST", "فوائد البنك");
            Add("bank_status_closed", "Open a savings account to store coins and earn daily compound interest (0.33%/day).", "افتح حساب توفير لتخزين العملات وكسب فائدة مركبة يومية (0.33٪ يومياً).");
            Add("bank_status_active", "Account Active  •  Balance: <b><color=#70FF70>${0:N0}</color></b>  •  Total Interest: <color=#FFD700>+${1:N0}</color> (+0.33% Daily)",
                                      "الحساب نشط  •  الرصيد: <b><color=#70FF70>{0:N0}$</color></b>  •  إجمالي الأرباح: <color=#FFD700>+{1:N0}$</color> (+0.33٪ يومياً)");
            Add("bank_open_account_btn", "OPEN BANK ACCOUNT (FEE: 100 COINS)", "فتح حساب بنكي (رسوم: 100 عملة)");
            Add("bank_need_coins_open", "NEED 100 COINS TO OPEN ACCOUNT", "تحتاج 100 عملة لفتح الحساب");
            Add("bank_already_active", "You already have an active bank account with Capital National Bank!", "لديك بالفعل حساب بنكي نشط في بنك العاصمة الوطني!");
            Add("bank_need_account_first", "You must first open a bank account (100 coin fee) before banking!", "يجب عليك أولاً فتح حساب بنكي (رسوم 100 عملة)!");
            Add("bank_quick_100", "+$100", "+100$");
            Add("bank_quick_500", "+$500", "+500$");
            Add("bank_quick_1k", "+$1K", "+1 ألف$");
            Add("bank_quick_max", "MAX CASH", "أقصى نقد");
            Add("bank_input_placeholder", "Amount...", "المبلغ...");
            Add("bank_deposit_btn", "DEPOSIT", "إيداع");
            Add("bank_withdraw_btn", "WITHDRAW", "سحب");
            Add("bank_withdraw_all_btn", "WITHDRAW ALL TO POCKET CASH", "سحب كامل الرصيد إلى النقد");

            // Bank Toasts
            Add("bank_opened_success", "Bank account opened successfully! Paid 100 coins fee.", "تم فتح الحساب البنكي بنجاح! تم دفع 100 عملة رسوم.");
            Add("bank_fee_insufficient", "You need at least 100 coins to open a bank account!", "تحتاج إلى 100 عملة على الأقل لفتح حساب بنكي!");
            Add("bank_deposit_success", "Deposited ${0:N0} into your bank account.", "تم إيداع {0:N0}$ في حسابك البنكي.");
            Add("bank_withdraw_success", "Withdrew ${0:N0} from your bank account.", "تم سحب {0:N0}$ من حسابك البنكي.");
            Add("bank_deposit_insufficient", "Insufficient cash in pocket to deposit this amount!", "النقود المتوفرة معك غير كافية لهذا المبلغ!");
            Add("bank_withdraw_insufficient", "Insufficient balance in your bank account!", "رصيد حسابك البنكي غير كافٍ!");
            Add("bank_invalid_amount", "Please enter a valid coin amount!", "يرجى إدخال مبلغ صحيح من العملات!");
            Add("bank_interest_credited", "Bank daily interest of ${0:N0} credited to your account!", "تمت إضافة فائدة بنكية يومية قدرها {0:N0}$ إلى حسابك!");

            // ==========================================
            // 5. SPECIAL BUILDING SECTIONS
            // ==========================================
            // Hospital
            Add("hosp_title", "CAPITAL CITY HOSPITAL & MEDICAL CENTER", "مستشفى ومركز العاصمة الطبي");
            Add("hosp_status", "Emergency room open. Restores Mayor health to 100 HP.", "قسم الطوارئ مفتوح. استعد صحة العمدة كاملة 100 نقطة.");
            Add("hosp_treat_btn", "RECEIVE TREATMENT (FREE)", "تلقي العلاج (مجاناً)");
            Add("hosp_already_healthy", "Mayor is in excellent health! No treatment needed.", "صحة العمدة ممتازة! لا حاجة للعلاج.");
            Add("hosp_treated_success", "Treatment complete! Health restored to 100 HP.", "اكتمل العلاج! تمت استعادة الصحة إلى 100 نقطة.");

            // Factory
            Add("fact_title", "HEAVY INDUSTRIAL MANUFACTURING PLANT", "مصنع التصنيع الصناعي الثقيل");
            Add("fact_status", "Automated production line producing commercial goods.", "خط إنتاج آلي لإنتاج البضائع التجارية.");
            Add("fact_collect_btn", "COLLECT MANUFACTURED GOODS", "تحصيل البضائع المصنعة");

            // Oil
            Add("oil_title", "OFFSHORE OIL DRILLING & REFINERY", "منشأة استخراج وتكرير النفط");
            Add("oil_status", "High-capacity pumpjack extracting crude oil barrels.", "مضخات عالية الكفاءة لاستخراج براميل النفط الخام.");
            Add("oil_collect_btn", "COLLECT OIL REVENUE", "تحصيل عائدات النفط");

            // Park
            Add("park_title", "CENTRAL RECREATION PARK & BOTANICAL GARDENS", "حديقة الترفيه والحدائق النباتية المركزية");
            Add("park_status", "Citizens enjoying green spaces and recreational trails.", "المواطنون يستمتعون بالمساحات الخضراء والمسارات الترفيهية.");
            Add("park_collect_btn", "COLLECT PARK ADMISSION REVENUE", "تحصيل رسوم دخول الحديقة");

            // Taxi
            Add("taxi_title", "CAPITAL TAXI FLEET HEADQUARTERS", "المقر الرئيسي لأسطول سيارات الأجرة");
            Add("taxi_status", "City taxi cab network running 24/7 transportation.", "شبكة سيارات أجرة المدينة تعمل على مدار الساعة.");
            Add("taxi_deploy_btn", "DISPATCH RENTED TAXI", "إرسال سيارة أجرة");

            // Flats
            Add("flat_title", "LUXURY RESIDENTIAL APARTMENTS & SUITES", "شقق وأجنحة سكنية فاخرة");
            Add("flat_stats", "Occupied Flats: <b>{0}</b> / {1}  •  Unit Rent: <b>+${2:N0}/day</b>", "الشقق المؤجرة: <b>{0}</b> / {1}  •  إيجار الوحدة: <b>+{2:N0}$/يوم</b>");
            Add("flat_buy_btn", "PURCHASE FLAT (${0:N0})", "شراء شقة ({0:N0}$)");
            Add("flat_collect_btn", "COLLECT TENANT RENT", "تحصيل إيجار السكان");
            Add("flat_all_owned", "ALL FLATS OWNED", "جميع الشقق مملوكة");
            Add("flat_buy_first", "BUY A FLAT FIRST", "اشترِ شقة أولاً");
            Add("flat_all_leased", "ALL FLATS LEASED", "جميع الشقق مؤجرة");
            Add("flat_rent_out_btn", "RENT OUT FLAT (+${0:N0}/day)", "تأجير شقة (+{0:N0}$/يوم)");

            // Maintenance
            Add("maint_title", "PROPERTY MAINTENANCE & REPAIR", "صيانة وإصلاح الممتلكات");
            Add("maint_status", "Building operational and structural integrity intact.", "سلامة المبنى التشغيلية والهيكلية في حالة ممتازة.");
            Add("maint_pay_btn", "PERFORM STRUCTURAL REPAIRS", "إجراء إصلاحات هيكلية");
            Add("maint_required_title", "MAINTENANCE REQUIRED: -${0:N0}", "يلزم إجراء صيانة: -{0:N0}$");
            Add("maint_pay_button", "PAY MAINTENANCE (-${0:N0})", "دفع الصيانة (-{0:N0}$)");
            Add("maint_insufficient_funds", "INSUFFICIENT FUNDS (-${0:N0})", "رصيد غير كافٍ (-{0:N0}$)");

            // ==========================================
            // 6. HEALTH & OVERLAY NOTICES
            // ==========================================
            Add("health_low_warning", "CRITICAL HEALTH ALERT! Rest or visit the Hospital immediately!", "تحذير صحي خطير! استرح أو زر المستشفى فوراً!");
            Add("health_fainted", "MAYOR FAINTED! RUSHED TO CAPITAL HOSPITAL", "أغمي على العمدة! تم نقله إلى مستشفى العاصمة");
            Add("health_hospitalized_cost", "Treatment cost: {0} coins. Mayor stabilized at 50 HP.", "تكلفة العلاج: {0} عملة. استقرت حالة العمدة عند 50 نقطة صحة.");

            // ==========================================
            // 7. TUTORIAL / GUIDE
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
