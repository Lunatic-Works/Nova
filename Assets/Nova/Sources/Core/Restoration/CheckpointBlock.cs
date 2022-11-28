using System;
using System.IO;
using System.Linq;
// using Stopwatch = System.Diagnostics.Stopwatch;

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

        private bool dirty = true;

        private readonly Stream stream;
        private readonly byte[] data = new byte[BlockSize];

        // initialize existing block from file
        public static CheckpointBlock FromFile(Stream stream, long id)
        {
            // var start = Stopwatch.GetTimestamp();

            var block = new CheckpointBlock(stream, id);
            stream.Seek(block.offset, SeekOrigin.Begin);
            stream.Read(block.data, 0, BlockSize);
            var index = 0;
            if (id == 0)
            {
                var header = CheckpointSerializer.FileHeader;
                var version = BitConverter.ToInt32(block.data, header.Length);
                if (version != CheckpointSerializer.Version || !header.SequenceEqual(block.data.Take(header.Length)))
                {
                    throw CheckpointCorruptedException.BadHeader;
                }

                index += CheckpointSerializer.FileHeaderSize;
            }

            block._nextBlock = BitConverter.ToInt64(block.data, index);
            block.dirty = false;

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"read {start}->{end}");

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
            if (!dirty || stream == null)
            {
                return;
            }

            // Debug.Log($"flush block {id}");
            // var start = Stopwatch.GetTimestamp();

            var index = 0;
            if (id == 0)
            {
                var version = BitConverter.GetBytes(CheckpointSerializer.Version);
                var header = CheckpointSerializer.FileHeader;
                Buffer.BlockCopy(header, 0, data, 0, header.Length);
                Buffer.BlockCopy(version, 0, data, header.Length, 4);
                index += CheckpointSerializer.FileHeaderSize;
            }

            var x = BitConverter.GetBytes(_nextBlock);
            Buffer.BlockCopy(x, 0, data, index, HeaderSize);
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Write(data, 0, BlockSize);
            dirty = false;

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"write {start}->{end}");
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
