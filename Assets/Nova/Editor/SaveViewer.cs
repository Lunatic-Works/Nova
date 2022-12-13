using System.Collections.Generic;
using System.Linq;
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
            private readonly CheckpointManager checkpointManager;
            private readonly List<long> rows = new List<long>();

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
                var item = new TreeViewItem { id = rows.Count, displayName = $"{nodeRecord.name} @{offset}" };
                rows.Add(offset);
                parent.AddChild(item);
                BuildTree(nodeRecord.child, item);
                BuildTree(nodeRecord.sibling, parent);
            }

            protected override TreeViewItem BuildRoot()
            {
                rows.Clear();
                var root = new TreeViewItem { id = -1, depth = -1, displayName = "root" };
                BuildTree(checkpointManager.beginNodeOffset, root);
                if (rows.Count == 0)
                {
                    root.AddChild(new TreeViewItem { id = 0, displayName = "" });
                }

                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            public long GetSelected()
            {
                if (HasSelection() && rows.Count > 0)
                {
                    return rows[GetSelection()[0]];
                }

                return -1;
            }
        }

        private class CheckpointTreeView : TreeView
        {
            private readonly CheckpointManager checkpointManager;
            private NodeRecord nodeRecord;
            private readonly List<long> rows = new List<long>();

            public CheckpointTreeView(CheckpointManager checkpointManager) : base(new TreeViewState())
            {
                this.checkpointManager = checkpointManager;
                Reload(null);
            }

            public void Reload(NodeRecord nodeRecord)
            {
                this.nodeRecord = nodeRecord;
                Reload();
                SetSelection(new List<int>());
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = -1, depth = -1, displayName = "root" };

                rows.Clear();
                if (nodeRecord != null)
                {
                    var offset = checkpointManager.NextRecord(nodeRecord.offset);
                    var dialogue = -1;
                    while (dialogue < nodeRecord.lastCheckpointDialogue)
                    {
                        dialogue = checkpointManager.GetCheckpointDialogue(offset);
                        var item = new TreeViewItem { id = rows.Count, displayName = $"checkpoint.{dialogue} @{offset}" };
                        rows.Add(offset);
                        root.AddChild(item);
                        offset = checkpointManager.NextCheckpoint(offset);
                    }
                }

                if (rows.Count == 0)
                {
                    root.AddChild(new TreeViewItem { id = 0, displayName = "" });
                }

                SetupDepthsFromParentsAndChildren(root);

                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            public long GetSelected()
            {
                if (HasSelection() && rows.Count > 0)
                {
                    return rows[GetSelection()[0]];
                }

                return -1;
            }
        }

        private struct SelectedCheckpoint
        {
            public long offset;
            public int dialogueIndex;
            public GameStateCheckpoint checkpoint;
            public Dictionary<string, string> details;
        }

        private CheckpointManager checkpointManager;

        private SaveTreeView saveTreeView;
        private CheckpointTreeView checkpointTreeView;
        private GUIStyle textAreaStyle;

        private NodeRecord selectedNodeRecord;
        private SelectedCheckpoint selectedCheckpoint = new SelectedCheckpoint { offset = -1 };
        private Vector2 scrollPos;
        private bool showNodeRecord;
        private bool showCheckpoint;
        private readonly HashSet<string> showCheckpointDetails = new HashSet<string>();

        private void OnEnable()
        {
            root = new GameObject("SaveViewer")
            {
                hideFlags = HideFlags.DontSave
            };
            checkpointManager = root.Ensure<CheckpointManager>();
            checkpointManager.runInEditMode = true;
            checkpointManager.Init();

            saveTreeView = new SaveTreeView(checkpointManager);
            checkpointTreeView = new CheckpointTreeView(checkpointManager);

            selectedCheckpoint.details = new Dictionary<string, string>();
        }

        private void OnDisable()
        {
            DestroyImmediate(root);
        }

        private void ShowCheckpointDetail(string key, string value)
        {
            var show = EditorGUILayout.Foldout(showCheckpointDetails.Contains(key), key);
            if (show)
            {
                showCheckpointDetails.Add(key);
                if (textAreaStyle == null)
                {
                    textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                }

                EditorGUILayout.TextArea(value, textAreaStyle);
            }
            else
            {
                showCheckpointDetails.Remove(key);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(position.width / 2));
            saveTreeView.OnGUI(EditorGUILayout.GetControlRect(false, position.height / 2));

            var selected = saveTreeView.GetSelected();
            if (selected != (selectedNodeRecord?.offset ?? -1))
            {
                selectedNodeRecord = selected >= 0 ? checkpointManager.GetNodeRecord(selected) : null;
                checkpointTreeView.Reload(selectedNodeRecord);
            }

            checkpointTreeView.OnGUI(EditorGUILayout.GetControlRect(false, position.height / 2));

            selected = checkpointTreeView.GetSelected();
            if (selected != selectedCheckpoint.offset)
            {
                selectedCheckpoint.offset = selected;
                if (selected >= 0)
                {
                    selectedCheckpoint.dialogueIndex = checkpointManager.GetCheckpointDialogue(selected);
                    var checkpoint = checkpointManager.GetCheckpoint(selected);
                    selectedCheckpoint.checkpoint = checkpoint;
                    selectedCheckpoint.details.Clear();
                    selectedCheckpoint.details.Add("Variables", checkpoint.variables.PrettyPrint());
                    foreach (var x in checkpoint.restoreDatas.OrderBy(x => x.Key))
                    {
                        selectedCheckpoint.details.Add(x.Key, x.Value.PrettyPrint());
                    }
                }
            }

            GUILayout.EndVertical();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();

            if (selectedNodeRecord != null)
            {
                showNodeRecord = EditorGUILayout.Foldout(showNodeRecord, "NodeRecord");
                if (showNodeRecord)
                {
                    GUILayout.Label(
                        $"dialogues: [{selectedNodeRecord.beginDialogue}, {selectedNodeRecord.endDialogue})");
                    GUILayout.Label($"last checkpoint dialogue: {selectedNodeRecord.lastCheckpointDialogue}");
                    GUILayout.Label($"variable: {selectedNodeRecord.variablesHash}");
                }

                if (selectedCheckpoint.offset >= 0)
                {
                    showCheckpoint = EditorGUILayout.Foldout(showCheckpoint, "Checkpoint");
                    if (showCheckpoint)
                    {
                        var checkpoint = selectedCheckpoint.checkpoint;
                        GUILayout.Label($"dialogueIndex in header: {selectedCheckpoint.dialogueIndex}");
                        GUILayout.Label($"dialogueIndex in checkpoint: {checkpoint.dialogueIndex}");
                        GUILayout.Label($"stepsCheckpointRestrained: {checkpoint.stepsCheckpointRestrained}");

                        foreach (var x in selectedCheckpoint.details)
                        {
                            ShowCheckpointDetail(x.Key, x.Value);
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }
    }
}
