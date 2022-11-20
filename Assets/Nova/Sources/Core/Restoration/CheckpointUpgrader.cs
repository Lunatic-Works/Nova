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

            var newOffset = offset;
            var needResetParent = false;
            if (changedNodes.TryGetValue(nodeRecord.name, out var differ))
            {
                if (differ == null)
                {
                    // Debug.Log($"remove nodeRecord @{offset}");
                    // node is removed
                    nodeRecordMap.Add(offset, 0);
                    return checkpointManager.DeleteNodeRecord(nodeRecord);
                }

                // normally we map beginDialogue to its rightMap
                // and endDialogue to its leftMap
                // several special cases are below
                var st0 = nodeRecord.beginDialogue;
                var ed0 = nodeRecord.endDialogue;

                // case 0: if this is the start of a node
                // it must map beginDialogue to 0
                var st1 = st0 == 0 ? 0 : differ.rightMap[st0];
                // ed1 is the mapped endDialogue - 1
                var ed1 = differ.leftMap[ed0 - 1];
                if (nodeRecord.child != 0)
                {
                    var child = checkpointManager.GetNodeRecord(nodeRecord.child);
                    // case 1: if it has a child of a different node
                    // it must map endDialogue to the end of the node
                    if (child.name != nodeRecord.name)
                    {
                        ed1 = differ.remap.Count - 1;
                    }
                    // case 2: if it has a child of the same node
                    // it must map endDialogue to the last node of that node
                    else
                    {
                        ed1 = child.beginDialogue - 1;
                    }
                }

                // Debug.Log($"map nodeRecord @{offset} [{st0}, {ed0}) -> [{st1}, {ed1}]");
                if (st1 <= ed1)
                {
                    newOffset = checkpointManager.UpgradeNodeRecord(nodeRecord, st1);
                    needResetParent = true;
                    // Debug.Log($"map nodeRecord @{offset} -> @{newOffset}");
                    nodeRecordMap.Add(offset, newOffset);
                    gameState.MoveToUpgrade(nodeRecord, ed1);
                }
                else
                {
                    // we need to delete this node
                    // but if both this node and its child has siblings
                    // we cannot do such operation
                    // this only happens in mini game cases
                    // Debug.Log($"remove nodeRecord @{offset}");
                    nodeRecordMap.Add(offset, 0);
                    if (nodeRecord.child != 0)
                    {
                        var child = checkpointManager.GetNodeRecord(nodeRecord.child);
                        if (nodeRecord.sibling != 0 && child.sibling != 0)
                        {
                            throw CheckpointCorruptedException.CannotUpgrade;
                        }

                        if (nodeRecord.sibling == 0)
                        {
                            nodeRecord.sibling = child.sibling;
                        }

                        newOffset = nodeRecord.child;
                    }
                    else
                    {
                        newOffset = nodeRecord.sibling;
                    }
                }
            }

            if (needResetParent)
            {
                checkpointManager.ResetChildParent(newOffset);
            }

            return newOffset;
        }

        private bool UpgradeBookmark(int key, Bookmark bookmark)
        {
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
                Debug.LogWarning($"Nova: Cannot upgrade bookmark {key} because dialogue {newDialogueIndex} is deleted.");
                return false;
            }

            bookmark.nodeOffset = newOffset;
            bookmark.dialogueIndex = newDialogueIndex;
            var checkpointOffset = checkpointManager.NextRecord(newOffset);
            long lastOffset = -1;
            while (true)
            {
                var dialogue = checkpointManager.GetCheckpointDialogue(checkpointOffset);
                if (dialogue >= newNodeRecord.lastCheckpointDialogue)
                {
                    bookmark.checkpointOffset = checkpointOffset;
                    return true;
                }

                if (dialogue > newDialogueIndex)
                {
                    bookmark.checkpointOffset = lastOffset;
                    return true;
                }

                lastOffset = checkpointOffset;
                checkpointOffset = checkpointManager.NextCheckpoint(checkpointOffset);
            }
        }

        public void UpgradeSaves()
        {
            foreach (var nodeName in changedNodes.Keys)
            {
                checkpointManager.InvalidateReachedData(nodeName);
            }

            checkpointManager.beginNodeOffset = UpgradeNodeTree(checkpointManager.beginNodeOffset);
            foreach (var id in checkpointManager.saveSlotsMetadata.Keys)
            {
                var bookmark = checkpointManager.LoadBookmark(id, false);
                if (UpgradeBookmark(id, bookmark))
                {
                    checkpointManager.SaveBookmark(id, bookmark, false);
                }
            }
        }
    }
}
