using System;
using System.Collections.Generic;
using System.IO;
using EmpireBuilder.Assets;
using EmpireBuilder.Economy;
using UnityEngine;

namespace EmpireBuilder.SaveLoad
{
    [Serializable]
    public class SaveData
    {
        public int currentDay = 1;
        public double cash = 10000.0;
        public List<OwnedAsset> ownedAssets = new List<OwnedAsset>();
        public List<Transaction> transactions = new List<Transaction>();
        public string savedTimestamp;

        public SaveData()
        {
            savedTimestamp = DateTime.UtcNow.ToString("O");
        }
    }

    public class SaveService : MonoBehaviour
    {
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "empire_builder_save.json");

        public static bool HasSaveFile()
        {
            return WebGLSaveManager.HasSaveData() || File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            WebGLSaveManager.ClearSaveData();

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[SaveService] Save file deleted.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveService] Exception deleting save file: {ex.Message}");
            }
        }

        public bool SaveGame(int currentDay, double cash, IEnumerable<OwnedAsset> assets, IEnumerable<Transaction> txs)
        {
            try
            {
                var data = new SaveData
                {
                    currentDay = currentDay,
                    cash = cash,
                    savedTimestamp = DateTime.UtcNow.ToString("O")
                };

                if (assets != null) data.ownedAssets.AddRange(assets);
                if (txs != null) data.transactions.AddRange(txs);

                string json = JsonUtility.ToJson(data, true);

                // Save to PlayerPrefs for WebGL persistence
                PlayerPrefs.SetString("EmpireBuilder_SaveService_Json", json);
                PlayerPrefs.SetFloat("SavedPlayerCash", (float)cash);
                PlayerPrefs.SetInt("CurrentDay", currentDay);
                PlayerPrefs.Save();

                // Trigger WebGL full city save
                WebGLSaveManager.Instance?.SaveGame();

#if !UNITY_WEBGL
                try
                {
                    File.WriteAllText(SaveFilePath, json);
                }
                catch { }
#endif

                Debug.Log($"[SaveService] Game successfully saved (Day {currentDay}, Cash ${cash:N0})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to save game: {ex.Message}");
                return false;
            }
        }

        public SaveData LoadGame()
        {
            if (!HasSaveFile())
            {
                Debug.LogWarning("[SaveService] No save file found.");
                return null;
            }

            try
            {
                string json = "";
                if (PlayerPrefs.HasKey("EmpireBuilder_SaveService_Json"))
                {
                    json = PlayerPrefs.GetString("EmpireBuilder_SaveService_Json");
                }

#if !UNITY_WEBGL
                if (string.IsNullOrEmpty(json) && File.Exists(SaveFilePath))
                {
                    json = File.ReadAllText(SaveFilePath);
                }
#endif

                if (string.IsNullOrEmpty(json))
                {
                    // Synthesize from WebGLSaveData if available
                    var webData = WebGLSaveManager.PeekSaveData();
                    if (webData != null)
                    {
                        var data = new SaveData
                        {
                            currentDay = webData.currentDay,
                            cash = webData.cash,
                            savedTimestamp = webData.timestamp
                        };
                        return data;
                    }
                    return null;
                }

                var loadedData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveService] Game loaded successfully (Day {loadedData.currentDay}, Cash ${loadedData.cash:N0})");
                return loadedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to load save file: {ex.Message}");
                return null;
            }
        }
    }
}

