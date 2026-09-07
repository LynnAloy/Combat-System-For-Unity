#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FlexibleTaskSystem.Editor
{
    [CustomEditor(typeof(AllTasks))]
    public sealed class AllTasksEditor : UnityEditor.Editor
    {
        private ReorderableList _taskList;

        private void OnEnable()
        {
            SerializedProperty tasks = serializedObject.FindProperty("tasks");
            _taskList = new ReorderableList(serializedObject, tasks, true, true, true, true);
            _taskList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Ordered Tasks");
            _taskList.elementHeightCallback = index =>
            {
                SerializedProperty element = tasks.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 6f;
            };
            _taskList.drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = tasks.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                string typeName = element.managedReferenceValue?.GetType().Name ?? "Missing Task";
                EditorGUI.PropertyField(rect, element, new GUIContent($"{index}. {typeName}"), true);
            };
            _taskList.onAddDropdownCallback = (rect, list) => ShowAddMenu(tasks);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("failurePolicy"));
            EditorGUILayout.Space();
            _taskList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowAddMenu(SerializedProperty tasks)
        {
            GenericMenu menu = new GenericMenu();
            Type[] types = TypeCache.GetTypesDerivedFrom<GameTask>()
                .Where(type => !type.IsAbstract && !type.IsGenericType &&
                               type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToArray();

            foreach (Type type in types)
            {
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    serializedObject.Update();
                    int index = tasks.arraySize;
                    tasks.InsertArrayElementAtIndex(index);
                    tasks.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(target);
                });
            }

            if (types.Length == 0)
                menu.AddDisabledItem(new GUIContent("No GameTask subclasses found"));

            menu.ShowAsContext();
        }
    }
}
#endif
