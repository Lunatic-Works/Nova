using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class GlobalSave
    {
        public readonly long identifier;
        public long beginReached;
        public long endReached;
        public long beginCheckpoint;
        public long endCheckpoint;

        public readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public GlobalSave(CheckpointSerializer serializer)
        {
            identifier = DateTime.Now.ToBinary();
            beginReached = endReached = serializer.BeginRecord();
            beginCheckpoint = endCheckpoint = serializer.BeginRecord();
        }
    }
}
