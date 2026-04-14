using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomBehaviorTable_SO))]
public class RoomBehaviorTableEditor : Editor
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
        DrawBehaviorList("普通房间", GetListProperty("normalRoomEntries"));
        DrawBehaviorList("精英房间", GetListProperty("eliteRoomEntries"));
        DrawBehaviorList("Boss房间", GetListProperty("bossRoomEntries"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBehaviorList(string header, SerializedProperty listProperty)
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
            ShowAddMenu(listProperty);
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
        // 绘制 managedReference 修改后的字段
        var property = element.Copy();
        var endProperty = element.GetEndProperty();

        while (property.NextVisible(true) && !SerializedProperty.EqualContents(property, endProperty))
        {
            // 跳过 managedReferenceFullTypename 和 managedReferenceId
            if (property.propertyPath.Contains("managedReference"))
                continue;

            EditorGUILayout.PropertyField(property, true);
        }
    }

    private void ShowAddMenu(SerializedProperty listProperty)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("敌人生成 (EnemyBehaviorEntry)"), false, () =>
        {
            serializedObject.Update();
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);
            var element = listProperty.GetArrayElementAtIndex(index);

            // 设置类型为 EnemyBehaviorEntry
            string fullTypeName = $"{typeof(EnemyBehaviorEntry).Assembly.GetName().Name}.{typeof(EnemyBehaviorEntry).FullName}";
            element.managedReferenceValue = Activator.CreateInstance(typeof(EnemyBehaviorEntry));
            serializedObject.ApplyModifiedProperties();
        });

        menu.ShowAsContext();
    }
}
