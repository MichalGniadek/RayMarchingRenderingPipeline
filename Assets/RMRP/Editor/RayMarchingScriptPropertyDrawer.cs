using UnityEngine;
using UnityEditor;
using System;

[CustomPropertyDrawer(typeof(RayMarchingScript), true)]
class RayMarchingScriptPropertyDrawer : PropertyDrawer
{
    float LineHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedObject so = new SerializedObject(property.objectReferenceValue);
        int arguments = so.FindProperty("functionArguments").arraySize;
        return (arguments + 1) * LineHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RayMarchingVolume go = (RayMarchingVolume)property.serializedObject.targetObject;
        EditorGUI.BeginProperty(position, label, property);

        Rect propertyRect = position;
        propertyRect.height = LineHeight;
        EditorGUI.PropertyField(propertyRect, property, label);
        propertyRect.y += LineHeight + EditorGUIUtility.standardVerticalSpacing;

        SerializedObject so = new SerializedObject(property.objectReferenceValue);
        SerializedProperty arguments = so.FindProperty("functionArguments");

        for (int i = 0; i < arguments.arraySize; i++)
        {
            SerializedProperty arg = arguments.GetArrayElementAtIndex(i);
            string name = arg.FindPropertyRelative("name").stringValue;
            string type = arg.FindPropertyRelative("type").stringValue;

            switch (type)
            {
                case "float":
                    ShowField<float>(go, name, propertyRect, EditorGUI.FloatField);
                    break;
            }
        }

        EditorGUI.EndProperty();
    }

    void ShowField<T>(RayMarchingVolume go, string name, Rect rect, Func<Rect, string, T, T> guiFunc)
    {
        T current_value = ShaderArgument<T>.GetArgument(go, name);

        Rect fieldRect = rect;
        fieldRect.x += 25;
        fieldRect.width -= 25;

        Rect dynamicToggleRect = rect;
        dynamicToggleRect.x += 5;
        dynamicToggleRect.width = 25;

        T new_value = guiFunc(fieldRect,
                ObjectNames.NicifyVariableName(name),
                current_value);

        EditorGUI.Toggle(dynamicToggleRect, false);

        ShaderArgument<T>.SetArgument(go, name, new_value);
    }
}