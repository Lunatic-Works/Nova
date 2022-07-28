using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Nova
{
    public class CheckpointBlock
    {
        public enum BlockType : byte
        {
            Invalid = 0,
            // global variables, etc.
            GlobalData,
            // a quick list to store reached
            ReachedHistory,
            // all checkpoints and node history
            CheckpointHistory,
        }

        public enum RecordType : byte
        {
            Invalid = 0,
            // the only record in GlobalData
            GlobalData,
            ReachedDialogue,
            // it is unused for now because reached branch needs to compare node history
            // so use checkpoint history directly
            ReachedBranch,
            ReachedEnd,
            Node,
            Checkpoint,
        }

        public const int BlockSize = 4096;
        public static readonly int NextBlockOffset;
        public static readonly int HeaderSize;
        public static readonly int DataSize;

        static CheckpointBlock()
        {
            NextBlockOffset = Marshal.SizeOf<BlockType>();
            HeaderSize = NextBlockOffset + Marshal.SizeOf<long>();
            DataSize = BlockSize - HeaderSize;
        }

        static long GetBlockId(long id)
        {
            return id / BlockSize;
        }

        public readonly long id;
        public readonly BlockType type;
        public long nextBlock;
        public readonly MemoryStream dataStream;

        private CheckpointSerializer serializer;
        private byte[] data = new byte[BlockSize];
        private long offset => id * BlockSize;

        // initialize existing block from file
        public CheckpointBlock(CheckpointSerializer serializer, long id)
        {
            this.serializer = serializer;
            this.id = id;

            serializer.SafeRead(data, offset, BlockSize);

            type = (BlockType)data[0];
            nextBlock = BitConverter.ToInt64(data, NextBlockOffset);
            dataStream = new MemoryStream(data, HeaderSize, DataSize, true);
        }

        public CheckpointBlock(CheckpointSerializer serializer, long id, BlockType type)
        {
            this.serializer = serializer;
            this.type = type;
            this.id = id;
            nextBlock = 0;
            data[0] = (byte)type;

            Flush();
            dataStream = new MemoryStream(data, HeaderSize, DataSize, true);
        }

        public void Flush()
        {
            var x = BitConverter.GetBytes(nextBlock);
            Buffer.BlockCopy(x, 0, data, NextBlockOffset, Marshal.SizeOf<long>());
            serializer.SafeWrite(data, offset, BlockSize);
        }
    }
}
