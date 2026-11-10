using UnityEngine;
using UnityEditor;
using EmpireBuilder.Traffic;
using System.Collections.Generic;

namespace EmpireBuilder.Traffic.Editor
{
    [CustomEditor(typeof(PedestrianWaypointNode))]
    [CanEditMultipleObjects]
    public class PedestrianWaypointNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PedestrianWaypointNode node = (PedestrianWaypointNode)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Pedestrian Waypoint Node\nSelect next waypoints in 'Next Nodes' to define walking paths for scene AI characters. Characters randomly pick between connected nodes.", MessageType.Info);
            EditorGUILayout.Space(4);

            SerializedProperty nextNodesProp = serializedObject.FindProperty("nextNodes");
            SerializedProperty pauseDurationProp = serializedObject.FindProperty("pauseDuration");
            SerializedProperty customSpeedProp = serializedObject.FindProperty("customSpeed");
            SerializedProperty nodeLabelProp = serializedObject.FindProperty("nodeLabel");

            EditorGUILayout.PropertyField(nodeLabelProp);
            EditorGUILayout.PropertyField(pauseDurationProp);
            EditorGUILayout.PropertyField(customSpeedProp);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Route Connections", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(nextNodesProp, true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Route Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect Next Sibling", GUILayout.Height(26)))
            {
                ConnectNextSibling(node);
            }
            if (GUILayout.Button("Clear Next Nodes", GUILayout.Height(26)))
            {
                Undo.RecordObject(node, "Clear Next Nodes");
                node.ClearNextNodes();
                EditorUtility.SetDirty(node);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Connect All Sibling Waypoints in Loop", GUILayout.Height(28)))
            {
                ConnectAllSiblingsInLoop(node);
            }

            EditorGUILayout.Space(4);
        }

        private void ConnectNextSibling(PedestrianWaypointNode node)
        {
            if (node.transform.parent == null)
            {
                EditorUtility.DisplayDialog("Notice", "Waypoint has no parent transform.", "OK");
                return;
            }

            Transform parent = node.transform.parent;
            int siblingIndex = node.transform.GetSiblingIndex();
            int nextIndex = (siblingIndex + 1) % parent.childCount;

            Transform nextTf = parent.GetChild(nextIndex);
            PedestrianWaypointNode nextNode = nextTf.GetComponent<PedestrianWaypointNode>();

            if (nextNode != null && nextNode != node)
            {
                Undo.RecordObject(node, "Connect Next Sibling");
                node.AddNextNode(nextNode);
                EditorUtility.SetDirty(node);
                Debug.Log($"[PedestrianWaypointNode] Connected '{node.name}' -> '{nextNode.name}'");
            }
            else
            {
                EditorUtility.DisplayDialog("Notice", $"Sibling '{nextTf.name}' does not have a PedestrianWaypointNode component.", "OK");
            }
        }

        private void ConnectAllSiblingsInLoop(PedestrianWaypointNode currentNode)
        {
            if (currentNode.transform.parent == null)
            {
                EditorUtility.DisplayDialog("Notice", "Waypoint has no parent transform.", "OK");
                return;
            }

            Transform parent = currentNode.transform.parent;
            List<PedestrianWaypointNode> siblings = new List<PedestrianWaypointNode>();

            for (int i = 0; i < parent.childCount; i++)
            {
                var n = parent.GetChild(i).GetComponent<PedestrianWaypointNode>();
                if (n != null) siblings.Add(n);
            }

            if (siblings.Count < 2)
            {
                EditorUtility.DisplayDialog("Notice", "Need at least 2 sibling PedestrianWaypointNodes to form a loop.", "OK");
                return;
            }

            Undo.RecordObjects(siblings.ToArray(), "Connect All Sibling Waypoints in Loop");

            for (int i = 0; i < siblings.Count; i++)
            {
                var current = siblings[i];
                var next = siblings[(i + 1) % siblings.Count];
                current.AddNextNode(next);
                EditorUtility.SetDirty(current);
            }

            Debug.Log($"[PedestrianWaypointNode] Successfully connected all {siblings.Count} sibling waypoints under '{parent.name}' in a complete loop!");
        }
    }
}

