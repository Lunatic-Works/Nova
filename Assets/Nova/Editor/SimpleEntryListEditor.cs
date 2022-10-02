using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Nova.Editor
{
    public abstract class SimpleEntryListEditor : UnityEditor.Editor
    {
        protected abstract SerializedProperty GetEntriesProperty();

        protected virtual GUIContent GetEntryLabelContent(int i)
        {
            return new GUIContent($"Entry {i:D2}");
        }

        protected virtual GUIContent GetHeaderContent()
        {
            return new GUIContent();
        }

        private SerializedProperty entries;
        private ReorderableList reorderableList;

        protected virtual void Init() { }

        private void OnEnable()
        {
            entries = GetEntriesProperty();
            reorderableList = new ReorderableList(serializedObject, entries, true, false, true, true)
            {
                drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.height -= 2;
                    var center = rect.center;
                    center.y += 1;
                    rect.center = center;
                    EditorGUI.PropertyField(rect, entries.GetArrayElementAtIndex(index),
                        GetEntryLabelContent(index));
                },
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, GetHeaderContent()),
                elementHeight = 20
            };

            Init();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            reorderableList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
