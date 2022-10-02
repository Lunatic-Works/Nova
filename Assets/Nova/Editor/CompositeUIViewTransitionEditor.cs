using UnityEditor;

namespace Nova.Editor
{
    [CustomEditor(typeof(CompositeUIViewTransition))]
    public class CompositeUIViewTransitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardChildTransitions"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backwardChildTransitions"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
