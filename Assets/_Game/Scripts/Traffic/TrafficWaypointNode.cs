using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Represents a point on the road network that AI cars follow.
    /// Can branch into multiple next nodes at intersections for random route selection.
    /// </summary>
    public class TrafficWaypointNode : MonoBehaviour
    {
        [Header("Node Connections")]
        [Tooltip("Next possible waypoint nodes. If more than 1, car randomly picks one.")]
        public List<TrafficWaypointNode> nextNodes = new List<TrafficWaypointNode>();

        [Header("Node Configuration")]
        [Tooltip("Optional speed limit override for this waypoint (e.g. sharp turn). If 0, car uses its own speed.")]
        public float speedLimit = 0f;

        [Tooltip("If true, this node is an intersection where only one car should pass at a time.")]
        public bool isIntersection = false;

        [Tooltip("Radius around this node considered inside the intersection.")]
        public float intersectionRadius = 4f;

        [Header("Runtime State")]
        private AICarController occupyingCar = null;
        private float reservationTime = 0f;

        public Vector3 Position => transform.position;
        public Transform NetworkRoot => transform.parent;

        /// <summary>
        /// Gets a random next node from the available branches.
        /// Optionally excludes a specific node (e.g. previous node) to prevent unwanted 180-degree backtracking.
        /// Filters out null or destroyed entries safely.
        /// </summary>
        public TrafficWaypointNode GetRandomNextNode(TrafficWaypointNode excludeNode = null)
        {
            if (nextNodes == null || nextNodes.Count == 0) return null;

            List<TrafficWaypointNode> validNodes = null;
            for (int i = 0; i < nextNodes.Count; i++)
            {
                if (nextNodes[i] != null)
                {
                    if (validNodes == null) validNodes = new List<TrafficWaypointNode>();
                    validNodes.Add(nextNodes[i]);
                }
            }

            if (validNodes == null || validNodes.Count == 0) return null;
            if (validNodes.Count == 1) return validNodes[0];

            // If an exclude node is provided and other forward choices exist, avoid backtracking
            if (excludeNode != null && validNodes.Count > 1)
            {
                validNodes.Remove(excludeNode);
                if (validNodes.Count == 1) return validNodes[0];
            }

            int index = Random.Range(0, validNodes.Count);
            return validNodes[index];
        }

        /// <summary>
        /// Attempts to reserve this intersection for a car.
        /// Auto-clears stale or expired reservations (> 2.5s or vehicle out of range) to prevent deadlock.
        /// Returns true if reserved successfully or already reserved by this car.
        /// </summary>
        public bool TryReserveIntersection(AICarController car)
        {
            if (!isIntersection) return true;

            // Auto-clear stale reservation if previous car is inactive, has departed, or timed out
            if (occupyingCar != null)
            {
                if (!occupyingCar.gameObject.activeInHierarchy ||
                    Vector3.Distance(transform.position, occupyingCar.transform.position) > (intersectionRadius + 5.0f) ||
                    (Time.time - reservationTime) > 2.5f)
                {
                    occupyingCar = null;
                }
            }

            if (occupyingCar == null || occupyingCar == car)
            {
                occupyingCar = car;
                reservationTime = Time.time;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Releases the intersection reservation.
        /// </summary>
        public void ReleaseIntersection(AICarController car)
        {
            if (occupyingCar == car)
            {
                occupyingCar = null;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isIntersection ? Color.yellow : Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.4f);

            if (nextNodes != null)
            {
                Gizmos.color = isIntersection ? Color.red : Color.blue;
                foreach (var next in nextNodes)
                {
                    if (next != null)
                    {
                        Gizmos.DrawLine(transform.position, next.transform.position);
                    }
                }
            }
        }
    }
}

