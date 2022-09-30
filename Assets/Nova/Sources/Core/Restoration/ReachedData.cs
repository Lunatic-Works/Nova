using System;
using System.Collections.Generic;

namespace Nova
{
    using VoiceEntries = Dictionary<string, VoiceEntry>;

    public interface IReachedData : ISerializedData { }

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
    public class ReachedEndData : IReachedData
    {
        public readonly string endName;

        public ReachedEndData(string endName)
        {
            this.endName = endName;
        }
    }

    [Serializable]
    public class ReachedDialogueData : IReachedData
    {
        public readonly string nodeName;
        public readonly int dialogueIndex;
        public readonly VoiceEntries voices;
        public readonly bool needInterpolate;

        public ReachedDialogueData(string nodeName, int dialogueIndex, VoiceEntries voices, bool needInterpolate)
        {
            this.nodeName = nodeName;
            this.dialogueIndex = dialogueIndex;
            this.voices = voices;
            this.needInterpolate = needInterpolate;
        }
    }
}