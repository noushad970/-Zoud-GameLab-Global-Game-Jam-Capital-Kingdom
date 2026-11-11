using System;
using System.Collections.Generic;
using System.IO;
using EmpireBuilder.Core;
using EmpireBuilder.UI;
using EmpireBuilder.World;
using UnityEngine;

namespace EmpireBuilder.SaveLoad
{
    [Serializable]
    public class PropertySaveEntry
    {
        public string objectName;
        public string propertyName;
        public int propertyType;
        public bool isOwned;
        public int flatsOwned;
        public int flatsRentedOut;
        public bool hasPendingMaintenance;
        public double pendingMaintenanceCost;
        public string maintenanceReason;
        public int totalMaintenanceRepairs;
        public double totalMaintenanceCostPaid;
        public double pendingProductionEarnings;
        public int productionBatchesCompleted;
        public double totalProductionRevenueCollected;
        public double pendingOilRevenue;
        public double totalOilRevenueCollected;
        public int matchesHosted;
        public double totalMatchRevenue;
        public bool isTaxiRentedToday;
        public int currentlyRentedTaxis;
        public int lastTaxiRentDay;
    }

    [Serializable]
    public class WebGLGameSaveData
    {
        public int currentDay = 1;
        public double cash = 10000.0;
        public float health = 100f;
        public bool tutorialCompleted = false;
        public string timestamp;
        public List<PropertySaveEntry> properties = new List<PropertySaveEntry>();

        public WebGLGameSaveData()
        {
            timestamp = DateTime.UtcNow.ToString("O");
        }
    }

    /// <summary>
    /// High-reliability persistence manager optimized for Unity WebGL (IndexedDB/PlayerPrefs)
    /// and Standalone/Editor (dual JSON backup).
    /// Handles full state persistence of economy, player health, city real estate, and tutorial progress.
    /// </summary>
    public class WebGLSaveManager : MonoBehaviour
    {
        public static WebGLSaveManager Instance { get; private set; }

        private const string SAVE_KEY = "CapitalKingdom_WebGL_SaveData_v2";
        private const string BACKUP_FILE_NAME = "capital_kingdom_webgl_save.json";

        [Header("Auto Save Settings")]
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 25f;

        private float autoSaveTimer = 0f;

        private static string LocalBackupPath => Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            autoSaveTimer = autoSaveInterval;

            // Check if we should load existing save
            int loadFlag = PlayerPrefs.GetInt("LoadOnStart", 1);
            if (loadFlag == 1 && HasSaveData())
            {
                LoadGame();
            }
        }

        private void Update()
        {
            if (!autoSaveEnabled) return;

            autoSaveTimer -= Time.deltaTime;
            if (autoSaveTimer <= 0f)
            {
                autoSaveTimer = autoSaveInterval;
                SaveGame();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        public static bool HasSaveData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY, "");
                if (!string.IsNullOrEmpty(json)) return true;
            }

#if !UNITY_WEBGL
            if (File.Exists(LocalBackupPath))
            {
                return true;
            }
#endif
            return false;
        }

        public static void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey("SavedPlayerCash");
            PlayerPrefs.DeleteKey("SavedPlayerHealth");
            PlayerPrefs.SetInt("LoadOnStart", 0);
            PlayerPrefs.Save();

#if !UNITY_WEBGL
            try
            {
                if (File.Exists(LocalBackupPath))
                {
                    File.Delete(LocalBackupPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebGLSaveManager] Could not delete local backup: {ex.Message}");
            }
#endif
            Debug.Log("[WebGLSaveManager] All save data successfully cleared for fresh new game.");
        }

        public static WebGLGameSaveData PeekSaveData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    var data = JsonUtility.FromJson<WebGLGameSaveData>(json);
                    if (data != null) return data;
                }
                catch { }
            }

#if !UNITY_WEBGL
            try
            {
                if (File.Exists(LocalBackupPath))
                {
                    string json = File.ReadAllText(LocalBackupPath);
                    var data = JsonUtility.FromJson<WebGLGameSaveData>(json);
                    if (data != null) return data;
                }
            }
            catch { }
#endif
            return null;
        }

        public bool SaveGame()
        {
            try
            {
                var data = new WebGLGameSaveData();

                // 1. Economy / Cash
                if (GameManager.Instance != null && GameManager.Instance.Economy != null)
                {
                    data.cash = GameManager.Instance.Economy.Cash;
                }
                else
                {
                    data.cash = PlayerPrefs.GetFloat("SavedPlayerCash", 10000f);
                }
                PlayerPrefs.SetFloat("SavedPlayerCash", (float)data.cash);

                // 2. Simulation Day
                if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
                {
                    data.currentDay = GameManager.Instance.Simulation.CurrentDay;
                }
                else
                {
                    data.currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
                }

                // 3. Player Health
                if (PlayerHealthManager.Instance != null)
                {
                    data.health = PlayerHealthManager.Instance.CurrentHealth;
                    PlayerHealthManager.Instance.SaveHealth();
                }
                else
                {
                    data.health = PlayerPrefs.GetFloat("SavedPlayerHealth", 100f);
                }

                // 4. Tutorial
                data.tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

                // 5. Real Estate Properties
                PropertyBuilding[] allBuildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in allBuildings)
                {
                    if (b == null) continue;

                    var entry = new PropertySaveEntry
                    {
                        objectName = b.gameObject.name,
                        propertyName = b.PropertyName,
                        propertyType = (int)b.PropertyType,
                        isOwned = b.IsOwned,
                        flatsOwned = b.FlatsOwned,
                        flatsRentedOut = b.FlatsRentedOut,
                        hasPendingMaintenance = b.HasPendingMaintenance,
                        pendingMaintenanceCost = b.PendingMaintenanceCost,
                        maintenanceReason = b.MaintenanceReason,
                        totalMaintenanceRepairs = b.TotalMaintenanceRepairs,
                        totalMaintenanceCostPaid = b.TotalMaintenanceCostPaid,
                        pendingProductionEarnings = b.PendingProductionEarnings,
                        productionBatchesCompleted = b.ProductionBatchesCompleted,
                        totalProductionRevenueCollected = b.TotalProductionRevenueCollected,
                        pendingOilRevenue = b.PendingOilRevenue,
                        totalOilRevenueCollected = b.TotalOilRevenueCollected,
                        matchesHosted = b.MatchesHosted,
                        totalMatchRevenue = b.TotalMatchRevenue,
                        isTaxiRentedToday = b.IsTaxiRentedToday,
                        currentlyRentedTaxis = b.CurrentlyRentedTaxis,
                        lastTaxiRentDay = b.LastTaxiRentDay
                    };

                    data.properties.Add(entry);
                }

                // Serialize
                string json = JsonUtility.ToJson(data, true);

                // Save to PlayerPrefs (Official WebGL standard)
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();

#if !UNITY_WEBGL
                try
                {
                    File.WriteAllText(LocalBackupPath, json);
                }
                catch { }
#endif

                Debug.Log($"[WebGLSaveManager] Game saved successfully! Cash: ${data.cash:N0}, Day: {data.currentDay}, Health: {data.health:F0} HP, Buildings: {data.properties.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSaveManager] Failed to save game: {ex.Message}");
                return false;
            }
        }

        public bool LoadGame()
        {
            try
            {
                WebGLGameSaveData data = PeekSaveData();
                if (data == null)
                {
                    Debug.LogWarning("[WebGLSaveManager] No valid save data found to load.");
                    return false;
                }

                // 1. Cash
                if (GameManager.Instance != null && GameManager.Instance.Economy != null)
                {
                    GameManager.Instance.Economy.SetCash(data.cash);
                }
                PlayerPrefs.SetFloat("SavedPlayerCash", (float)data.cash);

                // 2. Simulation Day
                if (GameManager.Instance != null && GameManager.Instance.Simulation != null)
                {
                    GameManager.Instance.Simulation.Initialize(data.currentDay);
                }

                // 3. Health
                if (PlayerHealthManager.Instance != null)
                {
                    PlayerHealthManager.Instance.SetHealth(data.health);
                }
                PlayerPrefs.SetFloat("SavedPlayerHealth", data.health);

                // 4. Properties
                if (data.properties != null && data.properties.Count > 0)
                {
                    Dictionary<string, PropertySaveEntry> map = new Dictionary<string, PropertySaveEntry>();
                    foreach (var p in data.properties)
                    {
                        if (!string.IsNullOrEmpty(p.objectName))
                        {
                            map[p.objectName] = p;
                        }
                    }

                    PropertyBuilding[] allBuildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var b in allBuildings)
                    {
                        if (b != null && map.TryGetValue(b.gameObject.name, out var savedEntry))
                        {
                            b.ApplySavedState(savedEntry);
                        }
                    }
                }

                // Refresh UI
                GamePlayHeaderUI.Instance?.UpdateMoneyDisplay();

                Debug.Log($"[WebGLSaveManager] Game loaded successfully! Day {data.currentDay}, Cash ${data.cash:N0}, Health {data.health:F0} HP.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebGLSaveManager] Failed to load game: {ex.Message}");
                return false;
            }
        }
    }
}
