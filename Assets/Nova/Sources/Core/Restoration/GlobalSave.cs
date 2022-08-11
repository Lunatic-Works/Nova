using System;
using System.Collections.Generic;

namespace Nova
{
    [Serializable]
    public class GlobalSave
    {
        public readonly int version;
        public readonly byte[] fileHeader;
        public readonly long identifier = DateTime.Now.ToBinary();

        public long beginReached;
        public long endReached;
        public long beginCheckpoint;
        public long endCheckpoint;

        public readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public GlobalSave(CheckpointSerializer serializer)
        {
            version = CheckpointSerializer.Version;
            fileHeader = CheckpointSerializer.FileHeader;
            beginReached = endReached = serializer.BeginRecord();
            beginCheckpoint = endCheckpoint = serializer.BeginRecord();
        }
    }
}
