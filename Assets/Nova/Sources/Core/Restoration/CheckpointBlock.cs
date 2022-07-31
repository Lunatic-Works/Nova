using System;
using System.IO;
using System.Text;

namespace Nova
{
    public readonly ref struct ByteSegment
    {
        public readonly byte[] array;
        public readonly int offset;
        public readonly int count;

        public ByteSegment(byte[] data)
        {
            array = data;
            offset = 0;
            count = data.Length;
        }

        public ByteSegment(byte[] data, int offset, int count)
        {
            this.array = data;
            this.offset = offset;
            this.count = count;
        }

        /* no check, be careful */
        public byte this[int index]
        {
            get => array[offset + index];
            set => array[offset + index] = value;
        }

        public ByteSegment Slice(int offset, int count)
        {
            return new ByteSegment(array, this.offset + offset, count);
        }

        public ByteSegment Slice(int offset)
        {
            return new ByteSegment(array, this.offset + offset, count - offset);
        }

        public MemoryStream ToStream()
        {
            return new MemoryStream(array, this.offset, this.count, true, true);
        }

        public int ReadInt(int offset)
        {
            return BitConverter.ToInt32(array, this.offset + offset);
        }

        public long ReadLong(int offset)
        {
            return BitConverter.ToInt64(array, this.offset + offset);
        }

        public ulong ReadUlong(int offset)
        {
            return BitConverter.ToUInt64(array, this.offset + offset);
        }

        public void WriteInt(int offset, int value)
        {
            var d = BitConverter.GetBytes(value);
            d.CopyTo(array, this.offset + offset);
        }

        public void WriteLong(int offset, long value)
        {
            var d = BitConverter.GetBytes(value);
            d.CopyTo(array, this.offset + offset);
        }

        public void WriteUlong(int offset, ulong value)
        {
            var d = BitConverter.GetBytes(value);
            d.CopyTo(array, this.offset + offset);
        }

        public void ReadBytes(int offset, byte[] bytes)
        {
            Buffer.BlockCopy(array, this.offset + offset, bytes, 0, bytes.Length);
        }

        public void ReadBytes(int offset, ByteSegment bytes)
        {
            Buffer.BlockCopy(array, this.offset + offset, bytes.array, bytes.offset, bytes.count);
        }

        public string ReadString(int offset, int count)
        {
            return Encoding.UTF8.GetString(array, this.offset + offset, count);
        }

        public string ReadString(int offset)
        {
            return Encoding.UTF8.GetString(array, this.offset + offset, count - offset);
        }

        public void WriteBytes(int offset, byte[] bytes)
        {
            Buffer.BlockCopy(bytes, 0, array, this.offset + offset, bytes.Length);
        }

        public void WriteBytes(int offset, ByteSegment bytes)
        {
            Buffer.BlockCopy(bytes.array, bytes.offset, array, this.offset + offset, bytes.count);
        }

        public void WriteString(int offset, string str)
        {
            Encoding.UTF8.GetBytes(str, 0, str.Length, array, this.offset + offset);
        }
    }

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
