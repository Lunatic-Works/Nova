using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
// using Stopwatch = System.Diagnostics.Stopwatch;

namespace Nova
{
    public class CheckpointCorruptedException : Exception
    {
        public CheckpointCorruptedException(string message) : base(message) { }

        public static readonly CheckpointCorruptedException BadHeader =
            new CheckpointCorruptedException("File header or version mismatch.");

        public static CheckpointCorruptedException BadOffset(long offset)
        {
            return new CheckpointCorruptedException($"Bad offset @{offset}");
        }

        public static CheckpointCorruptedException RecordOverflow(long offset)
        {
            return new CheckpointCorruptedException($"Record @{offset} overflow.");
        }

        public static CheckpointCorruptedException SerializationError(long offset, string reason)
        {
            return new CheckpointCorruptedException($"Serialization failed @{offset}: {reason}");
        }
    }

    public class CheckpointSerializer : IDisposable
    {
        public const int Version = 3;
        public static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
        public const long GlobalSaveOffset = CheckpointBlock.HeaderSize;

        private const int RecordHeader = 4; // sizeof(int)

        private readonly IFormatter formatter = new BinaryFormatter();
        private readonly string path;
        private FileStream file;
        private long endBlock;
        private readonly LRUCache<long, CheckpointBlock> cachedBlocks;

        public CheckpointSerializer(string path)
        {
            this.path = path;
            // 1M block cache
            cachedBlocks = new LRUCache<long, CheckpointBlock>(256, true);
        }

        public void Open()
        {
            file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            endBlock = CheckpointBlock.GetBlockId(file.Length);
            if (endBlock < 1)
            {
                AppendBlock();
            }
        }

        public void Dispose()
        {
            cachedBlocks.Clear();
            if (file != null)
            {
                file.Close();
                file.Dispose();
            }
        }

        private CheckpointBlock GetBlock(long id)
        {
            if (!cachedBlocks.TryGetValue(id, out var block))
            {
                block = CheckpointBlock.FromFile(file, id);
                cachedBlocks[id] = block;
            }

            return block;
        }

        private CheckpointBlock GetBlockIndex(long offset, out int index)
        {
            return GetBlock(CheckpointBlock.GetBlockIdIndex(offset, out index));
        }

        public ByteSegment GetRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var segment = block.segment;

            if (segment.Count < index + RecordHeader)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            var size = segment.ReadInt(index);
            index += RecordHeader;

            if (index + size <= segment.Count)
            {
                return segment.Slice(index, size);
            }

            // need concat multiple blocks
            var buf = new byte[size];
            var head = 0;
            while (head < size)
            {
                var count = Math.Min(size - head, segment.Count - index);
                segment.ReadBytes(index, new ByteSegment(buf, head, count));
                head += count;
                if (head < size && block.nextBlock == 0)
                {
                    throw CheckpointCorruptedException.RecordOverflow(offset);
                }

                block = GetBlock(block.nextBlock);
                segment = block.segment;
                index = 0;
            }

            return new ByteSegment(buf);
        }

        public NodeRecord GetNodeRecord(long offset)
        {
            return new NodeRecord(offset, GetRecord(offset));
        }

        private CheckpointBlock AppendBlock()
        {
            var id = endBlock++;
            var block = new CheckpointBlock(file, id);
            cachedBlocks[id] = block;
            return block;
        }

        private CheckpointBlock NextBlock(CheckpointBlock block)
        {
            if (block.nextBlock == 0)
            {
                var newBlock = AppendBlock();
                block.nextBlock = newBlock.id;
                return newBlock;
            }

            return GetBlock(block.nextBlock);
        }

        public long BeginRecord()
        {
            var block = AppendBlock();
            return block.dataOffset;
        }

        public long NextRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var size = block.segment.ReadInt(index);
            index += RecordHeader + size;
            while (index + RecordHeader > CheckpointBlock.DataSize)
            {
                index -= CheckpointBlock.DataSize;
                block = NextBlock(block);
            }

            index = Math.Max(index, 0);
            return block.dataOffset + index;
        }

        public void AppendRecord(long offset, ByteSegment bytes)
        {
            // Debug.Log($"append record @{offset} size={bytes.Count}");

            var block = GetBlockIndex(offset, out var index);
            var segment = block.segment;
            if (index + RecordHeader > segment.Count)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            segment.WriteInt(index, bytes.Count);
            index += RecordHeader;

            var pos = 0;
            while (pos < bytes.Count)
            {
                var size = Math.Min(segment.Count - index, bytes.Count - pos);
                segment.WriteBytes(index, bytes.Slice(pos, size));
                block.MarkDirty();
                pos += size;
                if (pos < bytes.Count)
                {
                    block = NextBlock(block);
                    segment = block.segment;
                    index = 0;
                }
            }
        }

        public void UpdateNodeRecord(NodeRecord record)
        {
            AppendRecord(record.offset, record.ToByteSegment());
        }

        public void SerializeRecord(long offset, object data, bool compress)
        {
            using var mem = new MemoryStream();
            if (compress)
            {
                using var compressor = new DeflateStream(mem, CompressionMode.Compress, true);
                formatter.Serialize(compressor, data);
            }
            else
            {
                formatter.Serialize(mem, data);
            }

            AppendRecord(offset, new ByteSegment(mem.GetBuffer(), 0, (int)mem.Position));
        }

        public object DeserializeRecord(long offset, bool compress)
        {
            using var mem = GetRecord(offset).ToStream();
            try
            {
                object obj;
                if (compress)
                {
                    using var decompressor = new DeflateStream(mem, CompressionMode.Decompress);
                    obj = formatter.Deserialize(decompressor);
                }
                else
                {
                    obj = formatter.Deserialize(mem);
                }

                return obj;
            }
            catch (Exception e)
            {
                throw CheckpointCorruptedException.SerializationError(offset, e.Message);
            }
        }

        public T DeserializeRecord<T>(long offset, bool compress)
        {
            if (DeserializeRecord(offset, compress) is T val)
            {
                return val;
            }

            throw CheckpointCorruptedException.SerializationError(offset, $"Type mismatch, need {typeof(T)}");
        }

        public void Flush()
        {
            // var start = Stopwatch.GetTimestamp();

            foreach (var block in cachedBlocks)
            {
                block.Value.Flush();
            }

            file.Flush();

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"flush {start}->{end}");
        }

        public Bookmark ReadBookmark(string path)
        {
            using var fs = File.OpenRead(path);
            using var r = new BinaryReader(fs);
            var fileHeader = r.ReadBytes(FileHeader.Length);
            var version = r.ReadInt32();

            if (version != Version || !fileHeader.SequenceEqual(FileHeader))
            {
                throw CheckpointCorruptedException.BadHeader;
            }

            return (Bookmark)formatter.Deserialize(fs);
        }

        public void WriteBookmark(string path, Bookmark obj)
        {
            using var fs = File.OpenWrite(path);
            using var r = new BinaryWriter(fs);
            r.Write(FileHeader);
            r.Write(Version);
            formatter.Serialize(fs, obj);
        }
    }
}
