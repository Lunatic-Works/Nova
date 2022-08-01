using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace Nova
{
    public class CheckpointCorruptedException : Exception
    {
        public CheckpointCorruptedException(string message) : base(message) { }

        public static readonly CheckpointCorruptedException BadHeader = new CheckpointCorruptedException("file header or version mismatch");

        public static CheckpointCorruptedException BadOffset(long offset)
        {
            return new CheckpointCorruptedException($"bad offset @{offset}");
        }

        public static CheckpointCorruptedException RecordOverflow(long offset)
        {
            return new CheckpointCorruptedException($"record @{offset} overflow");
        }

        public static CheckpointCorruptedException SerializationError(long offset, string reason)
        {
            return new CheckpointCorruptedException($"serialization failed @{offset} because {reason}");
        }
    }

    public class CheckpointSerializer : IDisposable
    {
        public const int Version = 3;
        public static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
        public const long GlobalSaveOffset = CheckpointBlock.HeaderSize;

        private static readonly TimeSpan BackupTime = TimeSpan.FromMinutes(5);
        // sizeof(int)
        private const int RecordHeader = 4;

        private readonly IFormatter formatter = new BinaryFormatter();
        private readonly string path;
        private FileStream file;
        private long endBlock;
        private string backupPath => path + ".old";
        private DateTime lastBackup = DateTime.Now;
        private LRUCache<long, CheckpointBlock> cachedBlock;

        public CheckpointSerializer(string path)
        {
            this.path = path;
            // 1M block cache
            cachedBlock = new LRUCache<long, CheckpointBlock>(256, true);
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

        public void SafeWrite(byte[] data, long offset, int count)
        {
            var now = DateTime.Now;
            if (now.Subtract(lastBackup) > BackupTime)
            {
                File.Copy(path, backupPath);
                lastBackup = now;
            }
            file.Seek(offset, SeekOrigin.Begin);
            file.Write(data, 0, count);
            file.Flush();
        }

        public void SafeRead(byte[] data, long offset, int count)
        {
            file.Seek(offset, SeekOrigin.Begin);
            file.Read(data, 0, count);
        }

        public void Dispose()
        {
            cachedBlock.Clear();
            if (file != null)
            {
                file.Close();
                file.Dispose();
            }
        }

        public CheckpointBlock GetBlock(long id)
        {
            CheckpointBlock block;
            if (!cachedBlock.TryGetValue(id, out block))
            {
                block = CheckpointBlock.FromFile(file, id);
                cachedBlock[id] = block;
            }
            return block;
        }

        public CheckpointBlock GetBlockIndex(long offset, out int index)
        {
            return GetBlock(CheckpointBlock.GetBlockIdIndex(offset, out index));
        }

        public ByteSegment GetRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var segment = block.Segment;

            if (segment.count < index + RecordHeader)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }
            var size = segment.ReadInt(index);
            index += RecordHeader;

            if (index + size <= segment.count)
            {
                return segment.Slice(index, size);
            }

            // need concat multiple blocks
            var buf = new byte[size];
            var head = 0;
            while (head < size)
            {
                var count = Math.Min(size - head, segment.count - index);
                segment.ReadBytes(index, new ByteSegment(buf, head, count));
                head += count;
                if (head < size && block.NextBlock == 0)
                {
                    throw CheckpointCorruptedException.RecordOverflow(offset);
                }
                block = GetBlock(block.NextBlock);
                segment = block.Segment;
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
            cachedBlock[id] = block;
            return block;
        }

        private CheckpointBlock NextBlock(CheckpointBlock block)
        {
            if (block.NextBlock == 0)
            {
                var newBlock = AppendBlock();
                block.NextBlock = newBlock.id;
                return newBlock;
            }
            return GetBlock(block.NextBlock);
        }

        public long BeginRecord()
        {
            var block = AppendBlock();
            return block.DataOffset;
        }

        public long NextRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var size = block.Segment.ReadInt(index);
            index += RecordHeader + size;
            while (index + RecordHeader > CheckpointBlock.DataSize)
            {
                index -= CheckpointBlock.DataSize;
                block = NextBlock(block);
            }
            index = Math.Max(index, 0);
            return block.DataOffset + index;
        }

        public void AppendRecord(long offset, ByteSegment bytes)
        {
            Debug.Log($"append record @{offset} {bytes.count} bytes");

            var block = GetBlockIndex(offset, out var index);
            var segment = block.Segment;
            if (index + RecordHeader > segment.count)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            segment.WriteInt(index, bytes.count);
            index += RecordHeader;

            var pos = 0;
            while (pos < bytes.count)
            {
                var size = Math.Min(segment.count - index, bytes.count - pos);
                segment.WriteBytes(index, bytes.Slice(pos, size));
                block.MarkDirty();
                pos += size;
                if (pos < bytes.count)
                {
                    block = NextBlock(block);
                    segment = block.Segment;
                    index = 0;
                }
            }
        }

        public void UpdateNodeRecord(NodeRecord record)
        {
            AppendRecord(record.offset, record.ToByteSegment());
        }

        public void SerializeRecord(long offset, object data)
        {
            var stream = new MemoryStream();
            formatter.Serialize(stream, data);
            AppendRecord(offset, new ByteSegment(stream.GetBuffer(), 0, (int)stream.Position));
        }

        public object DeserializeRecord(long offset)
        {
            var stream = GetRecord(offset).ToStream();
            try
            {
                var obj = formatter.Deserialize(stream);
                return obj;
            }
            catch (Exception e)
            {
                throw CheckpointCorruptedException.SerializationError(offset, e.Message);
            }
        }

        public T DeserializeRecord<T>(long offset)
        {
            if (DeserializeRecord(offset) is T val)
            {
                return val;
            }

            throw CheckpointCorruptedException.SerializationError(offset, $"type mismatch, need {typeof(T)}");
        }

        public void Flush()
        {
            foreach (var block in cachedBlock)
            {
                block.Value.Flush();
            }
            file.Flush();
        }

        public Bookmark ReadBookmark(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var r = new BinaryReader(fs))
            {
                var fileHeader = r.ReadBytes(FileHeader.Length);
                var version = r.ReadInt32();

                if (version != Version || !fileHeader.SequenceEqual(FileHeader))
                {
                    throw CheckpointCorruptedException.BadHeader;
                }
                return (Bookmark)formatter.Deserialize(fs);
            }
        }

        public void WriteBookmark(string path, Bookmark obj)
        {
            using (var fs = File.OpenWrite(path))
            using (var r = new BinaryWriter(fs))
            {
                r.Write(FileHeader);
                r.Write(Version);
                formatter.Serialize(fs, obj);
            }
        }
    }
}
