using System.Collections.Generic;
using EmpireBuilder.Assets;
using EmpireBuilder.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireBuilder.UI
{
    public class AssetShopModalUI : MonoBehaviour
    {
        [Header("Modal Root")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private Button closeButton;

        [Header("Catalog Container")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject shopCardPrefab;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        public void Open()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }
            PopulateCards();
        }

        public void Close()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        public void PopulateCards()
        {
            if (GameManager.Instance == null || cardContainer == null || shopCardPrefab == null) return;

            // Clear old items
            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            // Spawn a card for each asset in catalog
            foreach (var def in GameManager.Instance.Assets.Catalog)
            {
                if (def == null) continue;

                var cardObj = Instantiate(shopCardPrefab, cardContainer);
                var card = cardObj.GetComponent<ShopCardItemUI>();
                if (card != null)
                {
                    card.Setup(def, this);
                }
            }
        }
    }
}

