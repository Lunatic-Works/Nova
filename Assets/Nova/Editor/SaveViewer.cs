using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Nova.Editor
{
    public class SaveViewer : EditorWindow
    {
        private GameObject root;

        [MenuItem("Nova/Save Viewer")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SaveViewer), false, "Save Viewer");
        }

        private class SaveTreeView : TreeView
        {
            private List<long> rows = new List<long>();
            private CheckpointManager checkpointManager;
            private int nextId;

            public SaveTreeView(CheckpointManager checkpointManager) : base(new TreeViewState())
            {
                this.checkpointManager = checkpointManager;
                useScrollView = true;
                Reload();
            }

            private void BuildTree(long offset, TreeViewItem parent)
            {
                if (offset == 0)
                {
                    return;
                }
                var nodeRecord = checkpointManager.GetNodeRecord(offset);
                var item = new TreeViewItem { id = nextId++, displayName = $"{nodeRecord.name} @{offset}" };
                rows.Add(offset);
                parent.AddChild(item);
                BuildTree(nodeRecord.child, item);
                BuildTree(nodeRecord.sibling, parent);
            }

            protected override TreeViewItem BuildRoot()
            {
                rows.Clear();
                var root = new TreeViewItem { id = 0, depth = -1, displayName = "root" };
                nextId = 1;
                BuildTree(checkpointManager.beginNodeOffset, root);
                if (nextId == 1)
                {
                    root.AddChild(new TreeViewItem { id = nextId++, displayName = "empty" });
                }
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            public long GetSelected()
            {
                if (HasSelection() && rows.Count > 0)
                {
                    return rows[GetSelection()[0] - 1];
                }
                return -1;
            }
        }

        private CheckpointManager checkpointManager;
        private SaveTreeView treeView;
        private Vector2 scrollPos;

        private void OnEnable()
        {
            root = new GameObject("SaveViewer")
            {
                hideFlags = HideFlags.DontSave
            };
            checkpointManager = root.Ensure<CheckpointManager>();
            checkpointManager.runInEditMode = true;
            checkpointManager.Init();

            treeView = new SaveTreeView(checkpointManager);
        }

        private void OnDisable()
        {
            DestroyImmediate(root);
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect(false, position.height, GUILayout.Width(position.width - 300));
            treeView.OnGUI(rect);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();

            var selected = treeView.GetSelected();
            if (selected >= 0)
            {
                var nodeRecord = checkpointManager.GetNodeRecord(selected);
                GUILayout.Label($"dialogues: [{nodeRecord.beginDialogue}, {nodeRecord.endDialogue})");
                GUILayout.Label($"last checkpoint dialogue: {nodeRecord.lastCheckpointDialogue}");
                GUILayout.Label($"variable: {nodeRecord.variablesHash}");


                var offset = checkpointManager.NextRecord(selected);
                var dialogue = -1;
                while (dialogue < nodeRecord.lastCheckpointDialogue)
                {
                    dialogue = checkpointManager.GetCheckpointDialogue(offset);
                    GUILayout.Label($"checkpoint.{dialogue} @{offset}");
                    offset = checkpointManager.NextCheckpoint(offset);
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }
    }
}
