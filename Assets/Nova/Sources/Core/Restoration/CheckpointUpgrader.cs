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

        public CheckpointUpgrader(GameState gameState, CheckpointManager checkpointManager, Dictionary<string, Differ> changedNodes)
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
            NodeRecord nodeRecord = checkpointManager.GetNodeRecord(offset);
            nodeRecord.child = UpgradeNodeTree(nodeRecord.child);
            nodeRecord.sibling = UpgradeNodeTree(nodeRecord.sibling);

            var newOffset = offset;
            if (changedNodes.TryGetValue(nodeRecord.name, out var differ))
            {
                if (differ == null)
                {
                    // node is deleted
                    nodeRecordMap.Add(offset, 0);
                    return checkpointManager.DeleteNodeRecord(nodeRecord);
                }
                var st = differ.rightMap[nodeRecord.beginDialogue];
                // if this node record has a child that in another node
                // it means that the old save has reached this entire node
                // in that case we must also reach the entire node in new save
                var ed = checkpointManager.IsNodeRecordTillEnd(nodeRecord) ?
                    differ.remap.Count : differ.leftMap[nodeRecord.endDialogue - 1];
                Debug.Log($"[{nodeRecord.beginDialogue}, {nodeRecord.endDialogue}) -> [{st}, {ed}]");
                if (nodeRecord.beginDialogue < ed)
                {
                    newOffset = checkpointManager.UpgradeNodeRecord(nodeRecord, st);
                    nodeRecordMap.Add(offset, newOffset);
                    gameState.MoveToUpgrade(nodeRecord, ed);
                }
                else
                {
                    // we need to delete this node
                    // but if both this node and its child has siblings
                    // we cannot do such operation
                    // this only happens in mini game cases
                    nodeRecordMap.Add(offset, 0);
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
            }
            checkpointManager.ResetChildParent(newOffset);
            return newOffset;
        }

        private bool UpgradeBookmark(Bookmark bookmark)
        {
            if (nodeRecordMap.TryGetValue(bookmark.nodeOffset, out var newOffset))
            {
                if (newOffset == 0)
                {
                    return false;
                }
                NodeRecord nodeRecord = checkpointManager.GetNodeRecord(bookmark.nodeOffset);
                NodeRecord newNodeRecord = checkpointManager.GetNodeRecord(newOffset);
                var newDialogueIndex = changedNodes[nodeRecord.name].leftMap[bookmark.dialogueIndex];
                if (newDialogueIndex < newNodeRecord.beginDialogue || newDialogueIndex >= newNodeRecord.endDialogue)
                {
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
            return true;
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
                Bookmark bookmark = checkpointManager.LoadBookmark(id, false);
                if (UpgradeBookmark(bookmark))
                {
                    checkpointManager.SaveBookmark(id, bookmark, false);
                }
            }
        }
    }
}
