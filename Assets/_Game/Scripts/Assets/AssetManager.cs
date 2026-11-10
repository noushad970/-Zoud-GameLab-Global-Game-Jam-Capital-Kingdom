using System;
using System.Collections.Generic;
using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.Assets
{
    public class AssetManager : MonoBehaviour
    {
        [Header("Catalog Definitions")]
        [SerializeField] private List<AssetDefinition> availableDefinitions = new List<AssetDefinition>();

        private readonly List<OwnedAsset> ownedAssets = new List<OwnedAsset>();
        private readonly Dictionary<string, AssetDefinition> definitionLookup = new Dictionary<string, AssetDefinition>();

        public IReadOnlyList<OwnedAsset> OwnedAssets => ownedAssets.AsReadOnly();
        public IReadOnlyList<AssetDefinition> Catalog => availableDefinitions.AsReadOnly();

        public event Action<OwnedAsset> OnAssetAdded;
        public event Action<OwnedAsset> OnAssetRemoved;
        public event Action<OwnedAsset> OnAssetUpdated;

        private void Awake()
        {
            BuildLookup();
        }

        public void RegisterDefinitions(IEnumerable<AssetDefinition> defs)
        {
            availableDefinitions.Clear();
            if (defs != null)
            {
                availableDefinitions.AddRange(defs);
            }
            BuildLookup();
        }

        private void BuildLookup()
        {
            definitionLookup.Clear();
            foreach (var def in availableDefinitions)
            {
                if (def != null && !string.IsNullOrEmpty(def.assetId))
                {
                    definitionLookup[def.assetId] = def;
                }
            }
        }

        public AssetDefinition GetDefinition(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return null;
            definitionLookup.TryGetValue(definitionId, out var def);
            return def;
        }

        public void AddAsset(OwnedAsset asset)
        {
            if (asset == null || ownedAssets.Contains(asset)) return;
            ownedAssets.Add(asset);
            OnAssetAdded?.Invoke(asset);
        }

        public void RemoveAsset(OwnedAsset asset)
        {
            if (asset == null) return;
            if (ownedAssets.Remove(asset))
            {
                OnAssetRemoved?.Invoke(asset);
            }
        }

        public void NotifyAssetUpdated(OwnedAsset asset)
        {
            if (asset != null)
            {
                OnAssetUpdated?.Invoke(asset);
            }
        }

        public void LoadOwnedAssets(List<OwnedAsset> loadedAssets)
        {
            ownedAssets.Clear();
            if (loadedAssets != null)
            {
                ownedAssets.AddRange(loadedAssets);
            }
        }

        public List<OwnedAsset> GetActiveOperatingAssets()
        {
            var list = new List<OwnedAsset>();
            foreach (var a in ownedAssets)
            {
                if (a.CanOperate())
                {
                    list.Add(a);
                }
            }
            return list;
        }
    }
}

