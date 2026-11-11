using System.Collections.Generic;
using EmpireBuilder.Assets;
using EmpireBuilder.Economy;
using EmpireBuilder.Maintenance;
using EmpireBuilder.SaveLoad;
using EmpireBuilder.Simulation;
using EmpireBuilder.UI;
using UnityEngine;

namespace EmpireBuilder.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Services")]
        [SerializeField] private EconomyService economyService;
        [SerializeField] private AssetManager assetManager;
        [SerializeField] private SimulationService simulationService;
        [SerializeField] private MaintenanceService maintenanceService;
        [SerializeField] private SaveService saveService;

        [Header("Catalog Assets to Register")]
        [SerializeField] private List<AssetDefinition> defaultAssetDefinitions = new List<AssetDefinition>();

        public EconomyService Economy => economyService;
        public AssetManager Assets => assetManager;
        public SimulationService Simulation => simulationService;
        public MaintenanceService Maintenance => maintenanceService;
        public SaveService Save => saveService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureServices();
        }

        private void Start()
        {
            // Register catalog definitions
            if (defaultAssetDefinitions.Count > 0)
            {
                assetManager.RegisterDefinitions(defaultAssetDefinitions);
            }

            // Bind daily simulation to auto-save and net worth recalculation
            simulationService.OnDayCompleted += HandleDayCompleted;

            // Start new game by default if not loaded
            if (PlayerPrefs.GetInt("LoadOnStart", 0) == 1 && SaveService.HasSaveFile())
            {
                LoadExistingGame();
            }
            else
            {
                StartNewGame();
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "GamePlay")
            {
                EnsureServices();
                if (PlayerPrefs.GetInt("LoadOnStart", 0) == 1 && SaveService.HasSaveFile())
                {
                    LoadExistingGame();
                }
                else if (PlayerPrefs.GetInt("LoadOnStart", 0) == 0)
                {
                    StartNewGame();
                }
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();
            }
        }

        private void OnDestroy()
        {
            if (simulationService != null)
            {
                simulationService.OnDayCompleted -= HandleDayCompleted;
            }
        }

        private void EnsureServices()
        {
            if (economyService == null) economyService = GetComponentInChildren<EconomyService>(true) ?? gameObject.AddComponent<EconomyService>();
            if (assetManager == null) assetManager = GetComponentInChildren<AssetManager>(true) ?? gameObject.AddComponent<AssetManager>();
            if (maintenanceService == null) maintenanceService = GetComponentInChildren<MaintenanceService>(true) ?? gameObject.AddComponent<MaintenanceService>();
            if (simulationService == null) simulationService = GetComponentInChildren<SimulationService>(true) ?? gameObject.AddComponent<SimulationService>();
            if (saveService == null) saveService = GetComponentInChildren<SaveService>(true) ?? gameObject.AddComponent<SaveService>();

            simulationService.SetDependencies(economyService, assetManager, maintenanceService);
        }

        public void StartNewGame()
        {
            economyService.Initialize(10000.0); // $10,000 starting cash (Appendix A)
            assetManager.LoadOwnedAssets(new List<OwnedAsset>());
            simulationService.Initialize(1);
            economyService.RecalculateNetWorth(new List<OwnedAsset>());
            Debug.Log("[GameManager] New game started with $10,000 on Day 1.");
        }

        public bool LoadExistingGame()
        {
            var data = saveService.LoadGame();
            if (data == null) return false;

            simulationService.Initialize(data.currentDay);
            assetManager.LoadOwnedAssets(data.ownedAssets);
            economyService.LoadState(data.cash, data.transactions, data.ownedAssets);
            Debug.Log($"[GameManager] Loaded game successfully at Day {data.currentDay}.");
            return true;
        }

        private void HandleDayCompleted(DailyReport report)
        {
            // Auto-save on day advancement (GDD Section 8)
            saveService.SaveGame(
                simulationService.CurrentDay,
                economyService.Cash,
                assetManager.OwnedAssets,
                economyService.Transactions
            );
        }
    }
}

