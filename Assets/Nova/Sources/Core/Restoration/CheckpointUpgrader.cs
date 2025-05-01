using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    public class CheckpointUpgrader
    {
        private readonly GameState gameState;
        private readonly CheckpointManager checkpointManager;
        private readonly Dictionary<string, Differ> changedNodes;
        private readonly Dictionary<long, long> nodeRecordMap = new Dictionary<long, long>();

        public CheckpointUpgrader(GameState gameState, CheckpointManager checkpointManager,
            Dictionary<string, Differ> changedNodes)
        {
            this.gameState = gameState;
            this.checkpointManager = checkpointManager;
            this.changedNodes = changedNodes;
        }

        private long UpgradeNodeTree(long offset)
        {
            if (offset == 0)
            {
                return 0;
            }

            var nodeRecord = checkpointManager.GetNodeRecord(offset);
            nodeRecord.child = UpgradeNodeTree(nodeRecord.child);
            nodeRecord.sibling = UpgradeNodeTree(nodeRecord.sibling);

            if (changedNodes.TryGetValue(nodeRecord.name, out var differ))
            {
                if (differ == null)
                {
                    // Debug.Log($"remove nodeRecord @{offset}");
                    nodeRecordMap.Add(offset, 0);
                    return checkpointManager.DeleteNodeRecord(nodeRecord);
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
                    var child = checkpointManager.GetNodeRecord(nodeRecord.child);
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
                        ed1 = child.beginDialogue;
                    }
                }

                // Debug.Log($"map nodeRecord @{offset} [{st0}, {ed0}) -> [{st1}, {ed1})");
                if (st1 < ed1)
                {
                    var newOffset = checkpointManager.UpgradeNodeRecord(nodeRecord, st1);
                    // Debug.Log($"map nodeRecord @{offset} -> @{newOffset}");
                    nodeRecordMap.Add(offset, newOffset);
                    // We assume that nodeRecord is non-empty,
                    // so there must be a checkpoint at the first dialogue of the new node record,
                    // which is copied from the old node record
                    // We assume that this checkpoint is unchanged in the upgrade,
                    // and create the next checkpoints in this node record by jumping forward
                    // TODO: Upgrade this checkpoint
                    gameState.MoveUpgrade(nodeRecord, ed1 - 1);
                    return newOffset;
                }
                else
                {
                    // Debug.Log($"remove nodeRecord @{offset}");
                    nodeRecordMap.Add(offset, 0);
                    return checkpointManager.DeleteNodeRecord(nodeRecord);
                }
            }
            else
            {
                // just save this record with new child and sibling
                checkpointManager.UpdateNodeRecord(nodeRecord);
                return offset;
            }
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
            var newDialogueIndex = changedNodes[nodeRecord.name].leftMap[bookmark.dialogueIndex];
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

            var newRoot = UpgradeNodeTree(checkpointManager.beginCheckpoint);
            checkpointManager.beginCheckpoint = newRoot == 0 ? checkpointManager.endCheckpoint : newRoot;

            foreach (var id in checkpointManager.bookmarksMetadata.Keys)
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
