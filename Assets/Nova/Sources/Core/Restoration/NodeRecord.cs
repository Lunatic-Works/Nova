using System;
using System.Text;

namespace Nova
{
    public class NodeRecord
    {
        // 4 * sizeof(long) + 2 * sizeof(int)
        private const int HeaderSize = 4 * 8 + 2 * 4;

        public readonly string name;
        public readonly long offset;
        public long parent;
        public long child;
        public long brother;
        public readonly int beginDialogue;
        public int endDialogue;
        public readonly ulong variableHash;

        // we need a fixed length serialization
        // maybe using a code generator?
        public ByteSegment ToByteSegment()
        {
            var buf = new byte[HeaderSize + Encoding.UTF8.GetByteCount(name)];
            var segment = new ByteSegment(buf);
            segment.WriteLong(0, parent);
            segment.WriteLong(8, child);
            segment.WriteLong(16, brother);
            segment.WriteInt(24, beginDialogue);
            segment.WriteInt(28, endDialogue);
            segment.WriteUlong(32, variableHash);
            segment.WriteString(HeaderSize, name);
            return segment;
        }

        public NodeRecord(long offset, string name, int beginDialogue, ulong variableHash)
        {
            this.offset = offset;
            this.name = name;
            this.beginDialogue = this.endDialogue = beginDialogue;
            this.variableHash = variableHash;
        }

        public NodeRecord(long offset, ByteSegment segment)
        {
            this.offset = offset;
            parent = segment.ReadLong(0);
            child = segment.ReadLong(8);
            brother = segment.ReadLong(16);
            beginDialogue = segment.ReadInt(24);
            endDialogue = segment.ReadInt(28);
            variableHash = segment.ReadUlong(32);
            name = segment.ReadString(HeaderSize);
        }
    }
}
