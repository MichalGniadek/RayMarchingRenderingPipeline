using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(SDFScene))]
class SDFSceneEditor : Editor
{
    SerializedProperty parameters;
    ReorderableList list;

    void OnEnable()
    {
        parameters = serializedObject.FindProperty("parameters");
        list = new ReorderableList(serializedObject, parameters, true, true, false, true)
        {
            drawHeaderCallback = DrawListHeader,
            drawElementCallback = DrawListElement,
            elementHeightCallback = ListElementHeight,
        };
    }

    private float ListElementHeight(int index)
    {
        return EditorGUIUtility.singleLineHeight +
            2 * EditorGUIUtility.standardVerticalSpacing;
    }

    void DrawListHeader(Rect rect)
    {
        GUI.Label(rect, $"Parameters ({parameters.arraySize})");
    }

    void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty item = parameters.GetArrayElementAtIndex(index);
        GUIContent[] labels = new GUIContent[] { GUIContent.none, new GUIContent("Val") };
        rect.y += EditorGUIUtility.standardVerticalSpacing;
        EditorGUI.MultiPropertyField(rect, labels, item.FindPropertyRelative("name"), GUIContent.none);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("code"));
        EditorGUILayout.Space();
        list.DoLayoutList();

        List<Type> types = typeof(DynamicParameter).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(DynamicParameter))).ToList();

        foreach (var type in types)
        {
            if (GUILayout.Button("New " + ObjectNames.NicifyVariableName(type.Name)))
            {
                parameters.InsertArrayElementAtIndex(parameters.arraySize);
                parameters.GetArrayElementAtIndex(parameters.arraySize - 1).managedReferenceValue =
                        Activator.CreateInstance(type);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
