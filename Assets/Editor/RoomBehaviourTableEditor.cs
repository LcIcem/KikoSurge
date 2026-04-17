using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomBehaviourTableConfig))]
public class RoomBehaviourTableEditor : Editor
{
    private SerializedProperty GetListProperty(string fieldName)
    {
        return serializedObject.FindProperty(fieldName);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制普通字段
        DrawPropertiesExcluding(serializedObject, "normalRoomEntries", "eliteRoomEntries", "bossRoomEntries");

        // 绘制每个房间类型的行为列表
        DrawBehaviourList("普通房间", GetListProperty("normalRoomEntries"));
        DrawBehaviourList("精英房间", GetListProperty("eliteRoomEntries"));
        DrawBehaviourList("Boss房间", GetListProperty("bossRoomEntries"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBehaviourList(string header, SerializedProperty listProperty)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical();

        if (listProperty.isArray)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"[{i}] {GetEntryTypeName(listProperty, i)}");
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    listProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // 绘制展开的字段
                EditorGUI.indentLevel++;
                var element = listProperty.GetArrayElementAtIndex(i);
                DrawElementFields(element);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndVertical();

        // 添加按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加行为", GUILayout.Width(100)))
        {
            ShowAddMenu(listProperty.propertyPath);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
    }

    private string GetEntryTypeName(SerializedProperty listProperty, int index)
    {
        var element = listProperty.GetArrayElementAtIndex(index);
        var typeName = element.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(typeName))
            return "Null";
        // 只取类名部分
        int dotIndex = typeName.LastIndexOf('.');
        return dotIndex >= 0 ? typeName.Substring(dotIndex + 1) : typeName;
    }

    private void DrawElementFields(SerializedProperty element)
    {
        // 直接绘制整个元素，让 Unity 处理 List/Array 的展开显示
        // 这样不会重复绘制子属性
        EditorGUILayout.PropertyField(element, true);
    }

    private void ShowAddMenu(string propertyPath)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("敌人生成 (EnemyBehaviourEntry)"), false, () =>
        {
            serializedObject.Update();
            var listProperty = serializedObject.FindProperty(propertyPath);

            // 直接增加数组大小
            listProperty.arraySize++;
            var element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);

            // 先置空，再设置新实例，避免复制上一个元素的数据
            element.managedReferenceValue = null;
            element.managedReferenceValue = Activator.CreateInstance(typeof(EnemyBehaviourEntry));
            serializedObject.ApplyModifiedProperties();
        });

        menu.ShowAsContext();
    }
}
