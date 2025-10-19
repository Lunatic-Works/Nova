using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class CheckpointUpgrader
    {
        private readonly GameState gameState;
        private readonly FlowChartGraph flowChartGraph;
        private readonly CheckpointManager checkpointManager;
        private readonly Dictionary<string, Differ> changedNodes;
        private readonly Dictionary<long, long> nodeRecordMap = new Dictionary<long, long>();

        public CheckpointUpgrader(GameState gameState, FlowChartGraph flowChartGraph,
            CheckpointManager checkpointManager, Dictionary<string, Differ> changedNodes)
        {
            this.gameState = gameState;
            this.flowChartGraph = flowChartGraph;
            this.checkpointManager = checkpointManager;
            this.changedNodes = changedNodes;
        }

        // We need to do the upgrade in 2 passes:
        //   - Pass 1: Calculate the new topology of the node record tree, and the new begin and end dialogues.
        //             This needs to be done in reverse Polish order.
        //             Some nodes may be deleted in this pass.
        //   - Pass 2: Rerun scripts in each of the node that needs upgrade and create new node records.
        //             In the meanwhile, update parent-child link.
        private class UpgradeTreeNode
        {
            public NodeRecord nodeRecord;
            public string name => nodeRecord.name;
            // whether the FlowChartNode is changed
            public bool nodeChanged;
            // start and end dialogues of the new node record
            public int st, ed;
            public UpgradeTreeNode child;
            public UpgradeTreeNode sibling;
            // new node record offset
            public long offset;
            public bool upgraded => offset != nodeRecord.offset;
            public GameStateCheckpoint endCheckpoint;

            public bool needUpgrade(FlowChartGraph flowChartGraph, UpgradeTreeNode parent)
            {
                // If the FlowChartNode changed, then this node record needs upgrade.
                if (nodeChanged)
                {
                    return true;
                }

                // Otherwise if no parent or parent is not upgraded, then this node record is unchanged.
                if (parent == null || !parent.upgraded)
                {
                    return false;
                }

                // The beginning of a chapter is "stable", meaning that the change is supposed to stop here.
                return st != 0 || !flowChartGraph.GetNode(name).isChapter;
            }
        }

        // return the UpgradeTreeNode in case nodeRecord will be deleted
        private UpgradeTreeNode DeletedUpgradeNode(NodeRecord nodeRecord, UpgradeTreeNode child, UpgradeTreeNode sibling)
        {
            // if both child and sibiling exist, then we need to concat all children into siblings
            if (child != null && sibling != null)
            {
                var lastSibling = sibling;
                while (lastSibling.sibling != null)
                {
                    lastSibling = lastSibling.sibling;
                }

                lastSibling.sibling = child;
                if (child.st != sibling.st)
                {
                    // This may happen because of minigame
                    Debug.LogWarning(
                        $"Nova: Node record {nodeRecord} needs delete, " +
                        $"but child {child.nodeRecord} and sibling {sibling.nodeRecord} have different beginDialogue."
                    );
                }
            }

            return sibling ?? child;
        }

        private UpgradeTreeNode BuildUpgradeTree(long offset)
        {
            if (offset == 0)
            {
                return null;
            }

            var nodeRecord = checkpointManager.GetNodeRecord(offset);
            var child = BuildUpgradeTree(nodeRecord.child);
            var sibling = BuildUpgradeTree(nodeRecord.sibling);

            if (changedNodes.TryGetValue(nodeRecord.name, out var differ))
            {
                if (differ == null)
                {
                    // Debug.Log($"remove nodeRecord @{offset}");
                    return DeletedUpgradeNode(nodeRecord, child, sibling);
                    // After deleting a node record, its parent may have duplicate children
                    // It does not lead to error but can be optimized
                }

                // normally we map beginDialogue to its rightMap
                // and endDialogue to its leftMap
                // several special cases are below
                var st0 = nodeRecord.beginDialogue;
                var ed0 = nodeRecord.endDialogue;

                int st1;
                if (st0 <= 0 || differ.rightMap.Count == 0)
                {
                    // beginDialogue is at the beginning of the node,
                    // then map it to the beginning of the upgraded node
                    st1 = 0;
                }
                else
                {
                    st1 = differ.rightMap[Math.Min(st0, differ.rightMap.Count - 1)];
                }

                int ed1;
                if (nodeRecord.child == 0)
                {
                    if (ed0 <= 0 || differ.leftMap.Count == 0)
                    {
                        ed1 = 0;
                    }
                    else
                    {
                        ed1 = differ.leftMap[Math.Min(ed0, differ.leftMap.Count) - 1] + 1;
                    }
                }
                else
                {
                    if (child.name != nodeRecord.name)
                    {
                        // nodeRecord has a child of a different node,
                        // then map endDialogue to the end of the upgraded nodeRecord
                        ed1 = differ.remap.Count;
                    }
                    else
                    {
                        // nodeRecord has a child of the same node,
                        // then map endDialogue to the beginning of that node
                        ed1 = child.st;
                    }
                }

                // Debug.Log($"map nodeRecord @{offset} [{st0}, {ed0}) -> [{st1}, {ed1})");
                if (st1 < ed1)
                {
                    // Debug.Log($"nodeRecord @{offset} needs upgrade");
                    return new UpgradeTreeNode()
                    {
                        nodeRecord = nodeRecord,
                        nodeChanged = true,
                        st = st1,
                        ed = ed1,
                        child = child,
                        sibling = sibling,
                        offset = nodeRecord.offset
                    };
                }
                else
                {
                    // Debug.Log($"remove nodeRecord @{offset}");
                    return DeletedUpgradeNode(nodeRecord, child, sibling);
                }
            }
            else
            {
                // Debug.Log($"nodeRecord @{offset} does not need upgrade");
                return new UpgradeTreeNode()
                {
                    nodeRecord = nodeRecord,
                    nodeChanged = false,
                    st = nodeRecord.beginDialogue,
                    ed = nodeRecord.endDialogue,
                    child = child,
                    sibling = sibling,
                    offset = nodeRecord.offset
                };
            }
        }

        private void UpgradeNodeTree(UpgradeTreeNode node, UpgradeTreeNode parent)
        {
            if (node == null)
            {
                return;
            }

            var needUpgrade = node.needUpgrade(flowChartGraph, parent);
            var newNodeRecord = node.nodeRecord;
            if (needUpgrade)
            {
                var beginCheckpoint = parent?.endCheckpoint;
                newNodeRecord = checkpointManager.UpgradeNodeRecord(node.nodeRecord, node.st, ref beginCheckpoint);
                var oldOffset = node.offset;
                node.offset = newNodeRecord.offset;
                nodeRecordMap.Add(oldOffset, node.offset);

                // Debug.Log($"upgrade nodeRecord @{oldOffset} -> @{node.offset} [{node.st}, {node.ed})");

                node.endCheckpoint = gameState.MoveUpgrade(newNodeRecord, node.ed - 1, beginCheckpoint);
            }

            UpgradeNodeTree(node.child, node);
            UpgradeNodeTree(node.sibling, parent);

            var childOffset = node.child?.offset ?? 0;
            var siblingOffset = node.sibling?.offset ?? 0;
            var parentOffset = parent?.offset ?? 0;

            // Debug.Log($"reset nodeRecord @{newNodeRecord.offset} child={childOffset}, " +
            //     $"sibling={siblingOffset}, parent={parentOffset}");

            newNodeRecord.child = childOffset;
            newNodeRecord.sibling = siblingOffset;
            newNodeRecord.parent = parentOffset;
            checkpointManager.UpdateNodeRecord(newNodeRecord);
        }

        private long UpgradeNodeTree()
        {
            var root = BuildUpgradeTree(checkpointManager.beginCheckpoint);
            UpgradeNodeTree(root, null);
            return root.offset;
        }

        public bool TryUpgradeBookmark(Bookmark bookmark)
        {
            return TryUpgradeBookmark(0, bookmark);
        }

        // Returns whether the updated bookmark is valid
        private bool TryUpgradeBookmark(int key, Bookmark bookmark)
        {
            // Debug.Log($"Nova: Upgrade bookmark {key} @{bookmark.nodeOffset} {bookmark.dialogueIndex}");

            if (!nodeRecordMap.TryGetValue(bookmark.nodeOffset, out var newOffset))
            {
                return true;
            }

            if (newOffset == 0)
            {
                Debug.LogWarning($"Nova: Cannot upgrade bookmark {key} because nodeRecord is deleted.");
                return false;
            }

            var nodeRecord = checkpointManager.GetNodeRecord(bookmark.nodeOffset);
            var newNodeRecord = checkpointManager.GetNodeRecord(newOffset);
            int newDialogueIndex;
            if (changedNodes.ContainsKey(nodeRecord.name))
            {
                newDialogueIndex = changedNodes[nodeRecord.name].leftMap[bookmark.dialogueIndex];
            }
            else
            {
                newDialogueIndex = bookmark.dialogueIndex;
            }

            if (newDialogueIndex < newNodeRecord.beginDialogue || newDialogueIndex >= newNodeRecord.endDialogue)
            {
                // This may happen when the bookmark is at the beginning or the end of a node,
                // and the original dialogue is deleted
                Debug.LogWarning(
                    $"Nova: Dialogue {newDialogueIndex} is out of range " +
                    $"[{newNodeRecord.beginDialogue}, {newNodeRecord.endDialogue})."
                );
                if (newDialogueIndex < newNodeRecord.beginDialogue)
                {
                    newDialogueIndex = newNodeRecord.beginDialogue;
                }
                else
                {
                    newDialogueIndex = newNodeRecord.endDialogue - 1;
                }
            }

            Debug.Log($"Nova: Upgrade bookmark {key}: {nodeRecord} {bookmark.dialogueIndex} -> {newNodeRecord} {newDialogueIndex}");

            bookmark.nodeOffset = newOffset;
            bookmark.dialogueIndex = newDialogueIndex;
            return true;
        }

        public void UpgradeSaves()
        {
            foreach (var nodeName in changedNodes.Keys)
            {
                checkpointManager.InvalidateReachedDialogues(nodeName);
            }

            var newRoot = UpgradeNodeTree();
            checkpointManager.beginCheckpoint = newRoot == 0 ? checkpointManager.endCheckpoint : newRoot;

            // Copy Keys because it may be changed in the loop
            foreach (var id in checkpointManager.bookmarksMetadata.Keys.ToList())
            {
                try
                {
                    var bookmark = checkpointManager.LoadBookmark(id, true);
                    if (TryUpgradeBookmark(id, bookmark))
                    {
                        checkpointManager.SaveBookmark(id, bookmark, true);
                    }
                    else
                    {
                        checkpointManager.DeleteBookmark(id);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Nova: Failed to upgrade bookmark {id}: {e}");
                    checkpointManager.DeleteBookmark(id);
                }
            }
        }
    }
}
