using System;
using System.IO;
// using Stopwatch = System.Diagnostics.Stopwatch;

namespace Nova
{
    public class CheckpointBlock : IDisposable
    {
        public const int BlockSize = 4096;
        public const int HeaderSize = 8; // sizeof(long)
        public const int DataSize = BlockSize - HeaderSize;

        public static long GetBlockID(long offset)
        {
            return offset / BlockSize;
        }

        public static long GetBlockIDIndex(long offset, out int index)
        {
            var id = offset / BlockSize;
            index = (int)(offset - id * BlockSize) - HeaderSize;
            var minIndex = id == 0 ? CheckpointSerializer.FileHeaderSize : 0;
            if (index < minIndex)
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

        public bool dirty { get; private set; } = true;

        private readonly Action onFlush;

        private readonly Stream stream;
        private readonly byte[] data = new byte[BlockSize];

        // initialize existing block from file
        public static CheckpointBlock FromFile(Stream stream, long id, Action onFlush)
        {
            // var start = Stopwatch.GetTimestamp();

            var block = new CheckpointBlock(stream, id, onFlush);
            stream.Seek(block.offset, SeekOrigin.Begin);
            stream.Read(block.data, 0, BlockSize);
            var index = 0;
            if (id == 0)
            {
                index += CheckpointSerializer.FileHeaderSize;
            }

            block._nextBlock = BitConverter.ToInt64(block.data, index);
            block.dirty = false;

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"read {start}->{end}");

            return block;
        }

        public CheckpointBlock(Stream stream, long id, Action onFlush)
        {
            this.stream = stream;
            this.id = id;
            this.onFlush = onFlush;
            _nextBlock = 0;
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void Flush(bool callback = true)
        {
            if (!dirty || stream == null || !stream.CanRead)
            {
                return;
            }

            // Debug.Log($"flush block {id}");
            // var start = Stopwatch.GetTimestamp();

            var startIndex = 0;
            var index = 0;
            if (id == 0)
            {
                startIndex += CheckpointSerializer.FileHeaderSize;
                index += CheckpointSerializer.FileHeaderSize;
            }

            var x = BitConverter.GetBytes(_nextBlock);
            Buffer.BlockCopy(x, 0, data, index, HeaderSize);
            stream.Seek(offset + startIndex, SeekOrigin.Begin);
            stream.Write(data, startIndex, BlockSize - startIndex);
            dirty = false;
            if (callback)
            {
                onFlush.Invoke();
            }

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"write {start}->{end}");
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
