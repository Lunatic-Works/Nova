using System.Collections.Generic;

namespace Nova
{
    class CheckpointUpgrader
    {
        private readonly GameState gameState;
        private readonly CheckpointManager checkpointManager;
        private readonly Dictionary<string, Differ> changedNodes;

        public CheckpointUpgrader(GameState gameState, CheckpointManager checkpointSerializer, Dictionary<string, Differ> changedNodes)
        {
            this.gameState = gameState;
            this.checkpointManager = checkpointSerializer;
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
                    return checkpointManager.DeleteNodeRecord(nodeRecord);
                }
                var st = differ.rightMap[nodeRecord.beginDialogue];
                // if this node record has a child that in another node
                // it means that the old save has reached this entire node
                // in that case we must also reach the entire node in new save
                var ed = checkpointManager.IsNodeRecordTillEnd(nodeRecord) ?
                    differ.remap.Count : differ.leftMap[nodeRecord.endDialogue - 1];
                if (nodeRecord.beginDialogue < ed)
                {
                    newOffset = checkpointManager.UpgradeNodeRecord(nodeRecord, st);
                    // TODO: step to ed
                }
                else
                {
                    // we need to delete this node
                    // but if both this node and its child has siblings
                    // we cannot do such operation
                    // this only happens in mini game cases
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

        public void UpgradeCheckpoint()
        {
            foreach (var nodeName in changedNodes.Keys)
            {
                checkpointManager.InvalidateReachedData(nodeName);
            }
            checkpointManager.beginNodeOffset = UpgradeNodeTree(checkpointManager.beginNodeOffset);
        }
    }
}
