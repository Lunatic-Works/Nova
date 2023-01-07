using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class GlobalSave
    {
        public long identifier;
        public long beginReached;
        public long endReached;
        public long beginCheckpoint;
        public long endCheckpoint;

        public Dictionary<string, ulong> nodeHashes;
        public readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public GlobalSave(CheckpointSerializer serializer)
        {
            identifier = DateTime.Now.ToBinary();
            beginReached = endReached = serializer.BeginRecord();
            beginCheckpoint = endCheckpoint = serializer.BeginRecord();
        }
    }
}
