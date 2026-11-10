using System.Collections.Generic;
using UnityEngine;

namespace EmpireBuilder.Traffic
{
    /// <summary>
    /// Represents a waypoint node in the character walking network.
    /// Attach this script to waypoints under 'AI Waypoints for character'.
    /// In the Unity Inspector, select or link next waypoints in 'nextNodes' to define walking paths.
    /// Characters will navigate between connected waypoints and randomly choose branches.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PedestrianWaypointNode : MonoBehaviour
    {
        [Header("Route Connections")]
        [Tooltip("Next possible waypoint nodes that characters can walk to from this node. If multiple, character randomly branches.")]
        public List<PedestrianWaypointNode> nextNodes = new List<PedestrianWaypointNode>();

        [Header("Node Settings")]
        [Tooltip("Optional pause/idle duration (in seconds) when a character arrives at this waypoint. Set to 0 for continuous walking.")]
        public float pauseDuration = 0f;

        [Tooltip("Optional custom walking speed at this node. If 0, character uses default walk speed.")]
        public float customSpeed = 0f;

        [Tooltip("Optional label or description (e.g. 'Park Entrance', 'Crosswalk', 'Hotel Corner').")]
        public string nodeLabel = "";

        public Vector3 Position => transform.position;

        /// <summary>
        /// Gets a random next node from the available branches.
        /// Filters out null or destroyed entries safely.
        /// Optionally avoids immediately doubling back to the node the character just came from.
        /// </summary>
        public PedestrianWaypointNode GetRandomNextNode(PedestrianWaypointNode previousNode = null)
        {
            if (nextNodes == null || nextNodes.Count == 0) return null;

            List<PedestrianWaypointNode> validNodes = null;
            for (int i = 0; i < nextNodes.Count; i++)
            {
                var n = nextNodes[i];
                if (n != null && n != this)
                {
                    if (validNodes == null) validNodes = new List<PedestrianWaypointNode>();
                    validNodes.Add(n);
                }
            }

            if (validNodes == null || validNodes.Count == 0) return null;

            // If we have multiple choices, avoid immediately doubling back 85% of the time
            if (validNodes.Count > 1 && previousNode != null && validNodes.Contains(previousNode))
            {
                if (Random.value < 0.85f)
                {
                    validNodes.Remove(previousNode);
                }
            }

            if (validNodes.Count == 1) return validNodes[0];

            int index = Random.Range(0, validNodes.Count);
            return validNodes[index];
        }

        public void AddNextNode(PedestrianWaypointNode node)
        {
            if (node == null || node == this) return;
            if (nextNodes == null) nextNodes = new List<PedestrianWaypointNode>();
            if (!nextNodes.Contains(node))
            {
                nextNodes.Add(node);
            }
        }

        public void RemoveNextNode(PedestrianWaypointNode node)
        {
            if (nextNodes != null)
            {
                nextNodes.Remove(node);
            }
        }

        public void ClearNextNodes()
        {
            if (nextNodes != null)
            {
                nextNodes.Clear();
            }
        }

        private void OnDrawGizmos()
        {
            // Green for walking nodes, gold/amber if it has an idle pause duration
            Gizmos.color = pauseDuration > 0f ? new Color(1f, 0.75f, 0.2f, 0.9f) : new Color(0.12f, 0.82f, 0.38f, 0.85f);
            Gizmos.DrawSphere(transform.position, 0.5f);

            if (nextNodes != null)
            {
                Gizmos.color = new Color(0.2f, 0.95f, 0.45f, 0.85f);
                for (int i = 0; i < nextNodes.Count; i++)
                {
                    var next = nextNodes[i];
                    if (next != null)
                    {
                        DrawArrow(transform.position, next.transform.position);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.75f);

            if (nextNodes != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < nextNodes.Count; i++)
                {
                    var next = nextNodes[i];
                    if (next != null)
                    {
                        Gizmos.DrawLine(transform.position, next.transform.position);
                    }
                }
            }
        }

        private void DrawArrow(Vector3 from, Vector3 to)
        {
            Gizmos.DrawLine(from, to);
            Vector3 direction = (to - from).normalized;
            if (direction != Vector3.zero)
            {
                Vector3 midPoint = Vector3.Lerp(from, to, 0.6f);
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                if (right == Vector3.zero) right = Vector3.Cross(direction, Vector3.forward).normalized;

                float arrowLength = 0.9f;
                float arrowWidth = 0.45f;

                Vector3 p1 = midPoint - direction * arrowLength + right * arrowWidth;
                Vector3 p2 = midPoint - direction * arrowLength - right * arrowWidth;

                Gizmos.DrawLine(midPoint, p1);
                Gizmos.DrawLine(midPoint, p2);
            }
        }
    }
}

