using System;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using UnityEngine;

namespace EmpireBuilder.World
{
    [RequireComponent(typeof(Collider))]
    public class WorldAssetSlot : MonoBehaviour
    {
        public static event Action<WorldAssetSlot> OnSlotSelected;

        [Header("Slot Identity")]
        public string slotId;
        public string slotDisplayName = "Prime Downtown Plot";
        public AssetDefinition defaultPurchasableAsset;

        [Header("Visual Anchors")]
        [SerializeField] private GameObject vacantVisual;
        [SerializeField] private Transform buildingSpawnPoint;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private bool hideModelWhenVacant = false;

        private OwnedAsset boundAsset;
        private GameObject currentBuiltInstance;

        public OwnedAsset BoundAsset => boundAsset;
        public bool IsOccupied => boundAsset != null && boundAsset.state != AssetState.Sold;

        private void Start()
        {
            UpdateVisuals();
            SetHighlight(false);
        }

        public void BindAsset(OwnedAsset asset)
        {
            boundAsset = asset;
            UpdateVisuals();
        }

        public void ClearAsset()
        {
            boundAsset = null;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (IsOccupied)
            {
                if (vacantVisual != null) vacantVisual.SetActive(false);

                if (currentBuiltInstance == null && defaultPurchasableAsset != null && defaultPurchasableAsset.worldPrefab != null)
                {
                    Transform parent = buildingSpawnPoint != null ? buildingSpawnPoint : transform;
                    // Check if already has an instantiated instance in children
                    if (transform.childCount > 0)
                    {
                        currentBuiltInstance = transform.GetChild(0).gameObject;
                    }
                    else
                    {
                        currentBuiltInstance = Instantiate(defaultPurchasableAsset.worldPrefab, parent.position, parent.rotation, parent);
                    }
                }
                
                if (currentBuiltInstance != null)
                {
                    currentBuiltInstance.SetActive(true);
                }
            }
            else
            {
                if (vacantVisual != null) vacantVisual.SetActive(true);
                if (currentBuiltInstance != null && hideModelWhenVacant)
                {
                    currentBuiltInstance.SetActive(false);
                }
            }
        }

        public void SetHighlight(bool active)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(active);
            }
        }

        private void OnMouseDown()
        {
            // Do not trigger if pointer is over UI
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            OnSlotSelected?.Invoke(this);
        }
    }
}

