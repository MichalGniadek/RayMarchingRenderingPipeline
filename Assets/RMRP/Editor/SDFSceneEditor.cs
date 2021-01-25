using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;

//https://github.com/joshcamas/UnityCodeEditor/blob/master/Editor/CodeEditor.cs

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
            drawElementCallback = DrawListElement,
            drawHeaderCallback =
                rect => GUI.Label(rect, $"Parameters ({parameters.arraySize})"),
            elementHeightCallback =
                index => EditorGUIUtility.singleLineHeight +
                            2 * EditorGUIUtility.standardVerticalSpacing,
        };
    }

    void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var item = parameters.GetArrayElementAtIndex(index);
        var name = item.FindPropertyRelative("name");
        var value = item.FindPropertyRelative("value");

        rect.y += EditorGUIUtility.standardVerticalSpacing;
        rect.height -= 2 * EditorGUIUtility.standardVerticalSpacing;

        Rect nameRect = rect;
        nameRect.width /= 3;
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(nameRect, name, GUIContent.none);
        if (EditorGUI.EndChangeCheck()) QueueRecompile();

        Rect parameterRect = rect;
        parameterRect.x += rect.width / 3 + 5;
        parameterRect.width = rect.width * 2 / 3 - 10;

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 15;

        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(parameterRect, value, new GUIContent("V"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            (serializedObject.targetObject as SDFScene).SetAllParameters();
        }

        EditorGUIUtility.labelWidth = prevLabelWidth;
    }

    EditorCoroutine recompileCoroutine = null;
    void QueueRecompile()
    {
        if (recompileCoroutine != null)
            EditorCoroutineUtility.StopCoroutine(recompileCoroutine);
        recompileCoroutine = EditorCoroutineUtility.StartCoroutine(Recompile(), this);
    }

    IEnumerator Recompile()
    {
        yield return new EditorWaitForSeconds(1f);
        serializedObject.ApplyModifiedProperties();
        (serializedObject.targetObject as SDFScene).Compile();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var text = serializedObject.FindProperty("code");

        DisableSelectAllOnMouseUp();
        var (backStyle, foreStyle) = GetStyles();

        EditorGUILayout.BeginHorizontal();
        DrawLineNumbers(text.stringValue.Count(c => c == '\n'));

        EditorGUI.BeginChangeCheck();
        text.stringValue = EditorGUILayout.TextArea(text.stringValue,
                                                    backStyle,
                                                    GUILayout.ExpandHeight(true));
        if (EditorGUI.EndChangeCheck()) QueueRecompile();


        Color prevBackgroundColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.clear;

        EditorGUI.TextArea(GUILayoutUtility.GetLastRect(),
                           Highlight(text.stringValue),
                           foreStyle);

        GUI.backgroundColor = prevBackgroundColor;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        list.DoLayoutList();
        EditorGUILayout.Space();
        CreateDynamicParameterButtons();

        if (GUILayout.Button("Recompile"))
        {
            (serializedObject.targetObject as SDFScene).Compile();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawLineNumbers(int count)
    {
        Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(33), GUILayout.ExpandHeight(true));

        string lineString = "<color=grey>";

        for (int i = 1; i <= count + 1; i++)
        {

            lineString += i + "\n";
        }
        lineString += "</color>";

        GUIStyle style = new GUIStyle(GUI.skin.textArea);
        style.fontSize = 15;

        style.alignment = TextAnchor.UpperRight;
        style.richText = true;
        GUI.Label(rect, new GUIContent(lineString), style);
    }

    void CreateDynamicParameterButtons()
    {
        List<Type> types = typeof(DynamicParameter).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(DynamicParameter))).ToList();

        GUILayout.BeginHorizontal();
        int columnCount = 0;
        foreach (var type in types)
        {
            if (GUILayout.Button(ObjectNames.NicifyVariableName(type.Name)))
            {
                parameters.InsertArrayElementAtIndex(parameters.arraySize);
                parameters.GetArrayElementAtIndex(parameters.arraySize - 1)
                    .managedReferenceValue = Activator.CreateInstance(type);
            }
            columnCount++;
            if (columnCount == 4)
            {
                columnCount = 0;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();
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

    FieldInfo cachedSelectAll = null;
    void DisableSelectAllOnMouseUp()
    {
        cachedSelectAll ??= typeof(EditorGUI).GetField("s_SelectAllOnMouseUp",
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Default);

        cachedSelectAll.SetValue(null, false);
    }

    (GUIStyle, GUIStyle) GetStyles()
    {
        GUIStyle backStyle = new GUIStyle(GUI.skin.textArea);
        backStyle.fontSize = 15;
        backStyle.normal.textColor = Color.clear;
        backStyle.hover.textColor = Color.clear;
        backStyle.active.textColor = Color.clear;
        backStyle.focused.textColor = Color.clear;

        GUIStyle foreStyle = new GUIStyle(GUI.skin.textArea);
        foreStyle.fontSize = 15;
        foreStyle.richText = true;

        return (backStyle, foreStyle);
    }
}
