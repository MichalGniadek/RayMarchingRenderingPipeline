using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        Rect nameRect = rect;
        nameRect.width /= 3;
        nameRect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;
        Rect parameterRect = rect;
        parameterRect.x += rect.width / 3 + 10;
        parameterRect.width *= (2f / 3f);
        parameterRect.width -= 10;
        parameterRect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(nameRect, item.FindPropertyRelative("name"), GUIContent.none);
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(parameterRect, item.FindPropertyRelative("value"), GUIContent.none);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            (serializedObject.targetObject as SDFScene).ChangedParameterDefault();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        //https://github.com/joshcamas/UnityCodeEditor/blob/master/Editor/CodeEditor.cs

        GUIStyle backStyle = new GUIStyle(GUI.skin.textArea);
        backStyle.fontSize = 15;
        backStyle.normal.textColor = Color.clear;
        backStyle.hover.textColor = Color.clear;
        backStyle.active.textColor = Color.clear;
        backStyle.focused.textColor = Color.clear;

        string text = serializedObject.FindProperty("code").stringValue;
        text = EditorGUILayout.TextArea(text, backStyle, GUILayout.ExpandHeight(true));
        serializedObject.FindProperty("code").stringValue = text;

        Color prevBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.clear;
        var foreStyle = new GUIStyle(GUI.skin.textArea);
        foreStyle.fontSize = 15;
        foreStyle.richText = true;
        EditorGUI.TextArea(GUILayoutUtility.GetLastRect(), Highlight(text), foreStyle);
        GUI.backgroundColor = prevBackgroundColor;

        //EditorGUILayout.PropertyField(serializedObject.FindProperty("code"));
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

    string Highlight(string text)
    {
        string s = text;

        foreach (var keyword in new string[] { "float ", "float2 ", "float3 ", "float4 " })
        {
            s = s.Replace(keyword, $"<color=teal>{keyword}</color>");
        }

        foreach (var keyword in new string[] { "return ", "if ", "for " })
        {
            s = s.Replace(keyword, $"<color=purple>{keyword}</color>");
        }

        s = Regex.Replace(s, @"([\w\d_]+)( *\()", (m) =>
        {
            return $"<color=orange>{m.Groups[1]}</color>{m.Groups[2]}";
        });

        s = Regex.Replace(s, @"(?<!\w)([\d\.]+)(?!\w)", (m) =>
        {
            return $"<color=olive>{m.Groups[1]}</color>";
        });

        s = Regex.Replace(s, @"//.*\n", (m) =>
        {
            return $"<color=green>{m}</color>";
        });

        return s;
    }
}
