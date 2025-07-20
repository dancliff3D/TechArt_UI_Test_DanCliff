using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

// Custom inspector for BottomBarController
[CustomEditor(typeof(BottomBarController))]
public class BottomBarControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Reference to the controller we're editing
        BottomBarController controller = (BottomBarController)target;

        // Start tracking serialized fields
        serializedObject.Update();

        // Draw all default fields except the ones we want custom handling for
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            if (prop.name == "eventMethodName" || prop.name == "buttons" || prop.name == "defaultSelectedIndex")
            {
                continue; // Skip these, they get custom UI below
            }
            EditorGUILayout.PropertyField(prop, true);
            enterChildren = false;
        }

        // Add a spacer and draw a button for auto-setup
        GUILayout.Space(10);
        if (GUILayout.Button("Auto-Fill Buttons + Set Widths"))
        {
            controller.AutoFillAndSetWidths(); 
        }

        // References to serialized fields we need
        SerializedProperty buttonsProp = serializedObject.FindProperty("buttons");
        SerializedProperty lockedButtonsProp = serializedObject.FindProperty("lockedButtons");
        SerializedProperty defaultIndexProp = serializedObject.FindProperty("defaultSelectedIndex");
        
        // ----------------- Default button dropdown (skips locked ones) -----------------

        List<int> dropdownIndices = new() { -1 };  // Index list (None = -1)
        List<string> dropdownLabels = new() { "None" }; // Labels for dropdown

        for (int i = 0; i < buttonsProp.arraySize; i++)
        {
            var buttonProp = buttonsProp.GetArrayElementAtIndex(i);
            var panelProp = buttonProp.FindPropertyRelative("panel");

            if (panelProp.objectReferenceValue == null)
                continue;

            bool isLocked = IsLocked(lockedButtonsProp, panelProp.objectReferenceValue as RectTransform);
            if (isLocked) continue;

            // Try to use the button's label text, fallback to "Button X"
            string label = $"Button {i}";
            var labelProp = buttonProp.FindPropertyRelative("label");
            if (labelProp.objectReferenceValue is Text labelObj && !string.IsNullOrEmpty(labelObj.text))
                label = labelObj.text;

            dropdownIndices.Add(i);
            dropdownLabels.Add(label);
        }

        // Sync current default button with dropdown
        int currentRawIndex = defaultIndexProp.intValue;
        int currentDropdownIndex = dropdownIndices.IndexOf(currentRawIndex);
        if (currentDropdownIndex == -1) currentDropdownIndex = 0;

        int newDropdownIndex = EditorGUILayout.Popup("Default Selected Button", currentDropdownIndex, dropdownLabels.ToArray());
        int newDefaultIndex = dropdownIndices[newDropdownIndex];

        // Apply if the default changed
        controller.ApplyDefaultSelectionInEditor();
        if (newDefaultIndex != defaultIndexProp.intValue)
        {
            defaultIndexProp.intValue = newDefaultIndex;
            controller.ApplyDefaultSelectionInEditor();
        } 

        // ----------------- Event method dropdowns -----------------

        if (controller.uiEventManager != null)
        {
            // Reflection to find public void methods in UIEventManager
            MethodInfo[] methods = controller.uiEventManager.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            string[] methodNames = methods
                .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                .Select(m => m.Name)
                .ToArray();
            
            // Loop through buttons and draw their foldouts
            for (int i = 0; i < buttonsProp.arraySize; i++)
            {
                SerializedProperty buttonProp = buttonsProp.GetArrayElementAtIndex(i);
                var panelProp = buttonProp.FindPropertyRelative("panel");
                if (panelProp.objectReferenceValue == null)
                    continue;

                bool isLocked = IsLocked(lockedButtonsProp, panelProp.objectReferenceValue as RectTransform);

                // Grab method name property
                SerializedProperty methodNameProp = buttonProp.FindPropertyRelative("eventMethodName");
                if (string.IsNullOrEmpty(methodNameProp.stringValue) && methodNames.Length > 0)
                {
                    methodNameProp.stringValue = methodNames[0];
                }

                // Foldout per button
                string foldoutLabel = $"Button {i}" + (isLocked ? " (Locked)" : "");
                buttonProp.isExpanded = EditorGUILayout.Foldout(buttonProp.isExpanded, foldoutLabel, true);
                if (buttonProp.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    // Draw all child fields except eventMethodName
                    SerializedProperty childProp = buttonProp.Copy();
                    bool childEnter = true;
                    while (childProp.NextVisible(childEnter) && childProp.propertyPath.StartsWith(buttonProp.propertyPath))
                    {
                        childEnter = false;
                        if (childProp.name == "eventMethodName") continue;
                        EditorGUILayout.PropertyField(childProp, true);
                    }

                    // Event method dropdown
                    int selectedIndex = Array.IndexOf(methodNames, methodNameProp.stringValue);
                    if (selectedIndex == -1) selectedIndex = 0;
                    int newIndex = EditorGUILayout.Popup("Event Method", selectedIndex, methodNames);
                    if (newIndex != selectedIndex)
                    {
                        methodNameProp.stringValue = methodNames[newIndex];
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a UIEventManager to enable method dropdowns.", MessageType.Info);
        }

        // Save all property changes
        serializedObject.ApplyModifiedProperties();
    }

    // Check if a button is locked by comparing its panel to the lockedButtons list
    private bool IsLocked(SerializedProperty lockedButtonsProp, RectTransform panel)
    {
        if (panel == null) return true;
        for (int i = 0; i < lockedButtonsProp.arraySize; i++)
        {
            var lockedObj = lockedButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (lockedObj == panel.gameObject)
                return true;
        }
        return false;
    }
}
