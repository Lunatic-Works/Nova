using System;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
// using Stopwatch = System.Diagnostics.Stopwatch;

namespace Nova
{
    public class CheckpointCorruptedException : Exception
    {
        public CheckpointCorruptedException(string message) : base(message) { }

        public static readonly CheckpointCorruptedException BadHeader =
            new CheckpointCorruptedException("File header or version mismatch");

        public static CheckpointCorruptedException BadOffset(long offset)
        {
            return new CheckpointCorruptedException($"Bad offset @{offset}");
        }

        public static CheckpointCorruptedException RecordOverflow(long offset)
        {
            return new CheckpointCorruptedException($"Record @{offset} overflow");
        }

        public static CheckpointCorruptedException SerializationError(long offset, string reason)
        {
            return new CheckpointCorruptedException($"Serialization failed @{offset}: {reason}");
        }

        public static CheckpointCorruptedException JsonTypeDenied(string typeName)
        {
            return new CheckpointCorruptedException($"json type {typeName} is not permitted to (de)serialize");
        }
    }

    public class CheckpointSerializer : IDisposable
    {
        public const int Version = 4;
        public const bool defaultCompress = false;
        public static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
        public static readonly int FileHeaderSize = 4 + FileHeader.Length; // sizeof(int) + sizeof(FileHeader)
        public static readonly int GlobalSaveOffset = FileHeaderSize + CheckpointBlock.HeaderSize;

        private const int RecordHeader = 4; // sizeof(int)

        // Not to allow other assembly for security reason
        private class JsonTypeBinder : ISerializationBinder
        {
            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                var curAssembly = Assembly.GetExecutingAssembly();
                if (!typeof(ISerializedData).IsAssignableFrom(serializedType) || serializedType.Assembly != curAssembly)
                {
                    throw CheckpointCorruptedException.JsonTypeDenied(serializedType.Name);
                }
                assemblyName = curAssembly.GetName().Name;
                typeName = serializedType.FullName;
            }

            public Type BindToType(string assemblyName, string typeName)
            {
                var curAssembly = Assembly.GetExecutingAssembly();
                var type = curAssembly.GetType(typeName);
                if (assemblyName != curAssembly.GetName().Name || type == null || !typeof(ISerializedData).IsAssignableFrom(type))
                {
                    throw CheckpointCorruptedException.JsonTypeDenied(typeName);
                }
                return type;
            }
        }

        private readonly JsonSerializer jsonSerializer;
        private readonly string path;
        private FileStream file;
        private long endBlock;
        private readonly LRUCache<long, CheckpointBlock> cachedBlocks;

        public CheckpointSerializer(string path)
        {
            this.path = path;
            // 1M block cache
            cachedBlocks = new LRUCache<long, CheckpointBlock>(256, true);
            jsonSerializer = new JsonSerializer()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new JsonTypeBinder(),
                ContractResolver = new DefaultContractResolver()
                {
                    // By default, public fields and properties are serialized
                    // use this option to enable Fields serialization mode automatically for [Serializable] objects
                    // i.e. all private and public fields are serialized
                    // it also enables the use of uninitialized constructor
                    IgnoreSerializableAttribute = false,
                    // this seems to make ISerializable the same behavior as [Serializable]
                    IgnoreSerializableInterface = false,
                },
            };
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

        public void SerializeRecord<T>(long offset, T data, bool compress = defaultCompress)
        {
            using var mem = new MemoryStream();
            if (compress)
            {
                using var compressor = new DeflateStream(mem, CompressionMode.Compress, true);
                using var sw = new StreamWriter(compressor, Encoding.Default, 1024, true);
                jsonSerializer.Serialize(sw, data, typeof(T));
            }
            else
            {
                using var sw = new StreamWriter(mem, Encoding.Default, 1024, true);
                jsonSerializer.Serialize(sw, data, typeof(T));
            }

            Debug.Log($"serialize type={typeof(T)} json={Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Position)}");
            AppendRecord(offset, new ByteSegment(mem.GetBuffer(), 0, (int)mem.Position));
        }

        public T DeserializeRecord<T>(long offset, bool compress = defaultCompress)
        {
            var record = GetRecord(offset);
            using var mem = record.ToStream();
            Debug.Log($"deserialize type={typeof(T)} json={record.ReadString(0)})");
            try
            {
                T obj;
                if (compress)
                {
                    using var decompressor = new DeflateStream(mem, CompressionMode.Decompress);
                    using var sr = new StreamReader(decompressor);
                    using var jr = new JsonTextReader(sr);
                    obj = jsonSerializer.Deserialize<T>(jr);
                }
                else
                {
                    using var sr = new StreamReader(mem);
                    using var jr = new JsonTextReader(sr);
                    obj = jsonSerializer.Deserialize<T>(jr);
                }

                return obj;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw CheckpointCorruptedException.SerializationError(offset, e.Message);
            }
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

            var data = r.ReadString();
            using var sr = new StringReader(data);
            using var jr = new JsonTextReader(sr);
            return jsonSerializer.Deserialize<Bookmark>(jr);
        }

        public void WriteBookmark(string path, Bookmark obj)
        {
            using var fs = File.OpenWrite(path);
            using var r = new BinaryWriter(fs);
            r.Write(FileHeader);
            r.Write(Version);
            using (var sr = new StringWriter())
            using (var jr = new JsonTextWriter(sr))
            {
                jsonSerializer.Serialize(jr, obj);
                jr.Flush();
                jr.Close();
                r.Write(sr.ToString());
            }
        }
    }
}
