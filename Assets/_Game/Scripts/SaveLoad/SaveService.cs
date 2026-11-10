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
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveService] Save file deleted.");
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
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveService] Game successfully saved to {SaveFilePath}");
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
                string json = File.ReadAllText(SaveFilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveService] Game loaded successfully (Day {data.currentDay}, Cash ${data.cash:N0})");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Failed to load save file: {ex.Message}");
                return null;
            }
        }
    }
}

