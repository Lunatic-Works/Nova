using System;
using System.IO;
using UnityEngine;

namespace Nova
{
    public class CheckpointBlock : IDisposable
    {
        public const int BlockSize = 4096;
        public const int HeaderSize = 8; // sizeof(long)
        public const int DataSize = BlockSize - HeaderSize;

        public static long GetBlockId(long offset)
        {
            return offset / BlockSize;
        }

        public static long GetBlockIdIndex(long offset, out int index)
        {
            var id = offset / BlockSize;
            index = (int)(offset - id * BlockSize) - HeaderSize;
            if (index < 0)
            {
                throw CheckpointCorruptedException.BadOffset(offset);
            }

            return id;
        }

        public readonly long id;
        private long offset => id * BlockSize;
        public long dataOffset => id * BlockSize + HeaderSize;

        private long _nextBlock;

        public long nextBlock
        {
            get => _nextBlock;
            set
            {
                _nextBlock = value;
                MarkDirty();
            }
        }

        public ByteSegment segment => new ByteSegment(data, HeaderSize, DataSize);

        private bool dirty = true;

        private readonly Stream stream;
        private readonly byte[] data = new byte[BlockSize];

        // initialize existing block from file
        public static CheckpointBlock FromFile(Stream stream, long id)
        {
            var block = new CheckpointBlock(stream, id);
            stream.Seek(block.offset, SeekOrigin.Begin);
            stream.Read(block.data, 0, BlockSize);
            block._nextBlock = BitConverter.ToInt64(block.data, 0);
            block.dirty = false;
            return block;
        }

        public CheckpointBlock(Stream stream, long id)
        {
            this.stream = stream;
            this.id = id;
            _nextBlock = 0;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void Flush()
        {
            if (!dirty)
            {
                return;
            }

            Debug.Log($"flush block {id}");
            var x = BitConverter.GetBytes(_nextBlock);
            Buffer.BlockCopy(x, 0, data, 0, HeaderSize);
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(data, 0, BlockSize);
            dirty = false;
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
