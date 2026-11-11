using System;
using System.Collections;
using System.Collections.Generic;
using EmpireBuilder.Audio;
using EmpireBuilder.Core;
using EmpireBuilder.UI;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Coordinates periodic random events across player-owned real estate in Capital Kingdom:
    /// 1. Random Maintenance Costs for owned buildings (more expensive buildings have higher maintenance costs).
    /// 2. Industrial production revenue payouts for owned Factories at random intervals.
    /// 3. Oil & fuel distribution profits for owned Oil Stations at random intervals.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingMaintenanceManager : MonoBehaviour
    {
        public static BuildingMaintenanceManager Instance { get; private set; }

        [Header("Maintenance Settings")]
        [Tooltip("Minimum seconds between random maintenance checks")]
        [SerializeField] private float minMaintenanceInterval = 35f;

        [Tooltip("Maximum seconds between random maintenance checks")]
        [SerializeField] private float maxMaintenanceInterval = 85f;

        [Header("Factory Production Settings")]
        [Tooltip("Minimum seconds between random factory production batches")]
        [SerializeField] private float minFactoryInterval = 45f;

        [Tooltip("Maximum seconds between random factory production batches")]
        [SerializeField] private float maxFactoryInterval = 90f;

        [Header("Oil Station Revenue Settings")]
        [Tooltip("Minimum seconds between random oil sales batches")]
        [SerializeField] private float minOilInterval = 40f;

        [Tooltip("Maximum seconds between random oil sales batches")]
        [SerializeField] private float maxOilInterval = 80f;

        private Coroutine maintenanceCoroutine;
        private Coroutine factoryCoroutine;
        private Coroutine oilCoroutine;

        private readonly string[] genericMaintenanceReasons = new string[]
        {
            "Plumbing overhaul & high-pressure pipe repair",
            "Electrical grid safety inspection & transformer certification",
            "Roof sealant restoration & gutter overhaul",
            "HVAC climate control servicing & air filter replacements",
            "Exterior facade tuckpointing & structural safety inspection",
            "Fire suppression system certification & alarm testing"
        };

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
            maintenanceCoroutine = StartCoroutine(MaintenanceLoop());
            factoryCoroutine = StartCoroutine(FactoryProductionLoop());
            oilCoroutine = StartCoroutine(OilStationRevenueLoop());
        }

        private void OnDestroy()
        {
            if (maintenanceCoroutine != null) StopCoroutine(maintenanceCoroutine);
            if (factoryCoroutine != null) StopCoroutine(factoryCoroutine);
            if (oilCoroutine != null) StopCoroutine(oilCoroutine);
        }

        #region Maintenance Loop
        private IEnumerator MaintenanceLoop()
        {
            while (true)
            {
                float delay = UnityEngine.Random.Range(minMaintenanceInterval, maxMaintenanceInterval);
                yield return new WaitForSeconds(delay);

                PropertyBuilding[] buildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                List<PropertyBuilding> ownedCandidates = new List<PropertyBuilding>();

                foreach (var b in buildings)
                {
                    if (b != null && b.IsOwned && !b.HasPendingMaintenance && !b.IsCivic)
                    {
                        ownedCandidates.Add(b);
                    }
                }

                if (ownedCandidates.Count == 0) continue;

                // Pick random owned property
                PropertyBuilding target = ownedCandidates[UnityEngine.Random.Range(0, ownedCandidates.Count)];

                // Calculate maintenance cost proportional to building purchase price
                // More expensive building = higher maintenance cost
                float costMultiplier = UnityEngine.Random.Range(0.015f, 0.040f);
                double calculatedCost = Math.Round((target.PurchasePrice * costMultiplier) / 25.0) * 25.0;
                calculatedCost = Math.Max(150.0, calculatedCost);

                string reason = GetAppropriateReason(target.PropertyType);
                target.TriggerMaintenanceRequired(calculatedCost, reason);

                // Play alert sound & show toast
                GameSoundManager.Instance?.PlayError(target.transform.position);
                PropertyDetailModalUI.Instance?.ShowToast(
                    $"🔧 Maintenance Needed for '{target.PropertyName}'! Cost: -${calculatedCost:N0} ({reason}). Tap building to service."
                );

                Debug.Log($"[BuildingMaintenanceManager] Generated maintenance order for '{target.PropertyName}': -${calculatedCost:N0} ({reason})");
            }
        }

        private string GetAppropriateReason(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Stadium:
                    return "Colosseum pitch resurfacing & stadium floodlight maintenance";
                case PropertyType.Hospital:
                    return "Sterilization wing certification & emergency backup generator servicing";
                case PropertyType.Factory:
                    return "Hydraulic assembly calibration & industrial conveyor lubrication";
                case PropertyType.OilStation:
                    return "Fuel pump flowmeter recalibration & underground tank pressure check";
                case PropertyType.RentalBuilding:
                    return "Central elevator inspection & primary heating boiler overhaul";
                case PropertyType.SmallRentalBuilding:
                    return "Residential plumbing leak repair & roof shingle servicing";
                case PropertyType.Restaurant:
                    return "Commercial kitchen grease trap extraction & ventilation servicing";
                case PropertyType.Cafe:
                    return "Specialty espresso machine descaling & commercial refrigeration tuneup";
                case PropertyType.TaxiStation:
                    return "Depot vehicle lift hydraulic inspection & dispatch radio calibration";
                default:
                    return genericMaintenanceReasons[UnityEngine.Random.Range(0, genericMaintenanceReasons.Length)];
            }
        }
        #endregion

        #region Factory Production Loop
        private IEnumerator FactoryProductionLoop()
        {
            while (true)
            {
                float delay = UnityEngine.Random.Range(minFactoryInterval, maxFactoryInterval);
                yield return new WaitForSeconds(delay);

                PropertyBuilding[] buildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var b in buildings)
                {
                    if (b != null && b.IsOwned && b.PropertyType == PropertyType.Factory)
                    {
                        double batchEarnings = Math.Round(UnityEngine.Random.Range(1800f, 4200f) / 50.0) * 50.0;
                        b.AddProductionEarnings(batchEarnings);

                        GameSoundManager.Instance?.PlayCollect(b.transform.position);
                        PropertyDetailModalUI.Instance?.ShowToast(
                            $"🏭 Factory Production Batch Completed at '{b.PropertyName}'! Revenue: +${batchEarnings:N0} ready to collect!"
                        );
                    }
                }
            }
        }
        #endregion

        #region Oil Station Revenue Loop
        private IEnumerator OilStationRevenueLoop()
        {
            while (true)
            {
                float delay = UnityEngine.Random.Range(minOilInterval, maxOilInterval);
                yield return new WaitForSeconds(delay);

                PropertyBuilding[] buildings = FindObjectsByType<PropertyBuilding>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var b in buildings)
                {
                    if (b != null && b.IsOwned && b.PropertyType == PropertyType.OilStation)
                    {
                        double oilEarnings = Math.Round(UnityEngine.Random.Range(1200f, 3200f) / 50.0) * 50.0;
                        b.AddOilRevenue(oilEarnings);

                        GameSoundManager.Instance?.PlayCollect(b.transform.position);
                        PropertyDetailModalUI.Instance?.ShowToast(
                            $"⛽ Oil & Fuel Sales Revenue at '{b.PropertyName}'! Sales: +${oilEarnings:N0} ready to collect!"
                        );
                    }
                }
            }
        }
        #endregion
    }
}
