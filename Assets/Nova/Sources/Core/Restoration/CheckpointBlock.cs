using System;
using System.IO;
using UnityEngine;

namespace Nova
{
    public class CheckpointBlock : IDisposable
    {
        public const int BlockSize = 4096;
        // sizeof(long)
        public const int HeaderSize = 8;
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
        private long _nextBlock;
        public long NextBlock
        {
            get => _nextBlock;
            set
            {
                _nextBlock = value;
                MarkDirty();
            }
        }
        public ByteSegment Segment => new ByteSegment(data, HeaderSize, DataSize);
        public long DataOffset => id * BlockSize + HeaderSize;
        public bool Dirty { get; private set; } = true;

        private Stream stream;
        private byte[] data = new byte[BlockSize];
        private long Offset => id * BlockSize;

        // initialize existing block from file
        public static CheckpointBlock FromFile(Stream stream, long id)
        {
            CheckpointBlock block = new CheckpointBlock(stream, id);
            stream.Seek(block.Offset, SeekOrigin.Begin);
            stream.Read(block.data, 0, BlockSize);
            block._nextBlock = BitConverter.ToInt64(block.data, 0);
            block.Dirty = false;
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
            Dirty = true;
        }

        public void Flush()
        {
            if (!Dirty)
            {
                return;
            }
            Debug.Log($"flush block {id}");
            var x = BitConverter.GetBytes(_nextBlock);
            Buffer.BlockCopy(x, 0, data, 0, HeaderSize);
            stream.Seek(Offset, SeekOrigin.Begin);
            stream.Write(data, 0, BlockSize);
            Dirty = false;
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
