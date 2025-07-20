#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Custom inspector for the ToggleSwitch component
[CustomEditor(typeof(ToggleSwitch))]
public class ToggleSwitchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        base.OnInspectorGUI();
        
        // Get reference to the ToggleSwitch instance
        ToggleSwitch toggle = (ToggleSwitch)target;

        // Button to manually refresh this toggle
        if (GUILayout.Button("Refresh ToggleSwitch"))
        {
            toggle.EditorRefresh();
        }
        
        // Button to refresh all toggles in the scene
        if (GUILayout.Button("Refresh All ToggleSwitch"))
        {
            toggle.EditorRefreshAll();
        }
    }
}
#endif