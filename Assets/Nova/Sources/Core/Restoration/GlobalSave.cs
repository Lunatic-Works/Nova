using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class GlobalSave
    {
        public readonly byte[] fileHeader;
        public readonly int version;
        public readonly long identifier = DateTime.Now.ToBinary();
        public long beginReached;
        public long endReached;
        public long beginCheckpoint;
        public long endCheckpoint;
        public readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public GlobalSave(CheckpointSerializer serializer)
        {
            this.fileHeader = CheckpointSerializer.FileHeader;
            this.version = CheckpointSerializer.Version;
            beginReached = endReached = serializer.BeginRecord();
            beginCheckpoint = endCheckpoint = serializer.BeginRecord();
        }
    }


    [Serializable]
    public readonly struct ReachedDialogueData
    {
        public readonly string nodeName;
        public readonly int dialogueIndex;

        public ReachedDialogueData(string nodeName, int dialogueIndex)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
        }
    }
}
