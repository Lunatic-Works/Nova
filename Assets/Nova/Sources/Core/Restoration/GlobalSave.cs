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

    public readonly struct ReachedDialoguePosition
    {
        public readonly NodeRecord nodeRecord;
        public readonly long checkpointOffset;
        public readonly int dialogueIndex;

        public ReachedDialoguePosition(NodeRecord nodeRecord, long checkpointOffset, int dialogueIndex)
        {
            this.nodeRecord = nodeRecord;
            this.checkpointOffset = checkpointOffset;
            this.dialogueIndex = dialogueIndex;
        }
    }

    public readonly struct ReachedDialogueKey
    {
        public readonly string nodeName;
        public readonly int dialogueIndex;

        public ReachedDialogueKey(string nodeName, int dialogueIndex)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
        }

        public ReachedDialogueKey(ReachedDialogueData data) : this(data.nodeName, data.dialogueIndex) { }
    }


    [Serializable]
    public class ReachedDialogueData
    {
        public readonly string nodeName;
        public readonly int dialogueIndex;
        public readonly IReadOnlyDictionary<string, VoiceEntry> voices;
        public readonly bool needInterpolate;

        public ReachedDialogueData(string nodeName, int dialogueIndex, IReadOnlyDictionary<string, VoiceEntry> voices, bool needInterpolate)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
            this.voices = voices;
            this.needInterpolate = needInterpolate;
        }
    }
}
