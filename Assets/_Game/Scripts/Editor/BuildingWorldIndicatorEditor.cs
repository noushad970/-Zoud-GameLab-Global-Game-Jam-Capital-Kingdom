#if UNITY_EDITOR
using EmpireBuilder.World;
using UnityEditor;
using UnityEngine;

namespace EmpireBuilder.Editor
{
    [CustomEditor(typeof(BuildingWorldIndicator))]
    public class BuildingWorldIndicatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BuildingWorldIndicator indicator = (BuildingWorldIndicator)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Static Building Indicator:\n" +
                "• You can freely move, rotate, or scale this GameObject in the Scene View or Hierarchy.\n" +
                "• When 'Use Manual Position' is TRUE, your custom position will NEVER be overwritten at runtime.\n" +
                "• The indicator billboards to the camera and stays stationary in Edit Mode so you can position it easily.",
                MessageType.Info
            );

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f, 1f);
            if (GUILayout.Button("Snap To Rooftop", GUILayout.Height(28)))
            {
                var parentPb = indicator.GetComponentInParent<PropertyBuilding>();
                if (parentPb != null)
                {
                    Undo.RecordObject(indicator.transform, "Snap Indicator To Rooftop");
                    indicator.transform.localPosition = parentPb.GetRoofPosition();
                    EditorUtility.SetDirty(indicator.transform);
                }
            }

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f, 1f);
            if (GUILayout.Button("Refresh Visuals", GUILayout.Height(28)))
            {
                indicator.RefreshVisuals();
                EditorUtility.SetDirty(indicator.gameObject);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }
    }
}
#endif

