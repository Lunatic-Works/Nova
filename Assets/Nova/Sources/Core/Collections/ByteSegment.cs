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
}
