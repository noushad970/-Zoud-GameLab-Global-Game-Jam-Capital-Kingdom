#if UNITY_EDITOR
using EmpireBuilder.World;
using UnityEditor;
using UnityEngine;

namespace EmpireBuilder.Editor
{
    [CustomEditor(typeof(PropertyBuilding))]
    public class PropertyBuildingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PropertyBuilding building = (PropertyBuilding)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                $"Building: {building.PropertyName} ({building.PropertyType})\n" +
                $"Owned: {building.IsOwned} | Has Flats: {building.HasFlats} | Flats Owned: {building.FlatsOwned}/{building.TotalFlats}\n" +
                $"Price: ${building.PurchasePrice:N0} | Rent: ${building.DailyRentIncome:N0}/day",
                MessageType.Info
            );

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f, 1f);
            if (GUILayout.Button("Select World Indicator", GUILayout.Height(28)))
            {
                var ind = building.GetComponentInChildren<BuildingWorldIndicator>(true);
                if (ind != null)
                {
                    Selection.activeGameObject = ind.gameObject;
                    EditorGUIUtility.PingObject(ind.gameObject);
                }
                else
                {
                    Debug.LogWarning($"[PropertyBuildingEditor] No static indicator found under {building.name}. Use 'Build Static Indicator' below.");
                }
            }

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f, 1f);
            if (GUILayout.Button("Snap Indicator to Rooftop", GUILayout.Height(28)))
            {
                var ind = building.GetComponentInChildren<BuildingWorldIndicator>(true);
                if (ind != null)
                {
                    Undo.RecordObject(ind.transform, "Snap Indicator To Rooftop");
                    ind.transform.localPosition = building.GetRoofPosition();
                    EditorUtility.SetDirty(ind.transform);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Rebuild Static Indicator Hierarchy Under This Building", GUILayout.Height(26)))
            {
                building.EnsureWorldIndicator();
                var ind = building.GetComponentInChildren<BuildingWorldIndicator>(true);
                if (ind != null)
                {
                    ind.EnsureUIHierarchy();
                    ind.RefreshVisuals();
                    EditorUtility.SetDirty(ind.gameObject);
                    EditorUtility.SetDirty(building.gameObject);
                }
            }

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }
    }
}
#endif

