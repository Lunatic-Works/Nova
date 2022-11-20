using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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
            new CheckpointCorruptedException(
                $"File header or version mismatch, expected version={CheckpointSerializer.Version}.");

        public static CheckpointCorruptedException BadReachedData(string nodeName)
        {
            return new CheckpointCorruptedException($"Bad reached data, node={nodeName}");
        }

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

        public static CheckpointCorruptedException JsonTypeDenied(string typeName, string assemblyName)
        {
            return new CheckpointCorruptedException(
                $"JSON type {typeName} in {assemblyName} is not permitted to (de)serialize.");
        }

        public static readonly CheckpointCorruptedException CannotUpgrade =
            new CheckpointCorruptedException("Unable to upgrade global save.");
    }

    public sealed class CheckpointJsonSerializer : JsonSerializer
    {
        // Not to allow other assembly for security reason
        private class JsonTypeBinder : ISerializationBinder
        {
            private static readonly Assembly CurAssembly = Assembly.GetExecutingAssembly();

            private static readonly HashSet<Assembly> AllowedAssembly = new HashSet<Assembly>
            {
                CurAssembly,
                // mscorlib,
                typeof(List<>).Assembly,
            };

            private static bool IsPrimitiveType(Type serializedType, bool checkAssembly = true)
            {
                return serializedType.IsPrimitive || serializedType == typeof(string) ||
                       (serializedType.IsEnum && (!checkAssembly || IsAllowedAssembly(serializedType)));
            }

            private static bool IsAllowedAssembly(Type serializedType)
            {
                return serializedType != null && !serializedType.IsGenericParameter &&
                       AllowedAssembly.Contains(serializedType.Assembly);
            }

            private bool IsNovaType(Type serializedType)
            {
                if (!typeof(ISerializedData).IsAssignableFrom(serializedType) || serializedType.Assembly != CurAssembly)
                {
                    return false;
                }

                if (serializedType.IsGenericType && serializedType.GetGenericArguments().Any(x => !IsAllowedType(x)))
                {
                    return false;
                }

                return true;
            }

            private bool IsAllowedType(Type serializedType)
            {
                if (!IsAllowedAssembly(serializedType))
                {
                    return false;
                }

                // case 0: primitives
                if (IsPrimitiveType(serializedType, false))
                {
                    return true;
                }

                // case 1: all Nova types inheriting ISerializedData
                if (IsNovaType(serializedType))
                {
                    return true;
                }

                // case 2: Dictionary<K, V>
                if (serializedType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] kv = serializedType.GetGenericArguments();
                    if (IsPrimitiveType(kv[0]) && IsAllowedType(kv[1]))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
                if (!IsAllowedType(serializedType))
                {
                    throw CheckpointCorruptedException.JsonTypeDenied(typeName, assemblyName);
                }
            }

            public Type BindToType(string assemblyName, string typeName)
            {
                var assembly = AllowedAssembly.SingleOrDefault(x => x.GetName().Name == assemblyName);
                var type = assembly?.GetType(typeName);
                if (!IsAllowedType(type))
                {
                    throw CheckpointCorruptedException.JsonTypeDenied(typeName, assemblyName);
                }

                return type;
            }
        }

        public CheckpointJsonSerializer()
        {
            TypeNameHandling = TypeNameHandling.Auto;
            SerializationBinder = new JsonTypeBinder();
            ContractResolver = new DefaultContractResolver()
            {
                // By default, public fields and properties are serialized
                // Use this option to enable Fields serialization mode automatically for [Serializable] objects
                // i.e. all private and public fields are serialized
                // It also enables the use of uninitialized constructor
                IgnoreSerializableAttribute = false,
                // This seems to make ISerializable the same behavior as [Serializable]
                IgnoreSerializableInterface = false,
            };
        }
    }

    public class CheckpointSerializer : IDisposable
    {
        public const int Version = 4;

        public static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");
        public static readonly int FileHeaderSize = 4 + FileHeader.Length; // sizeof(int) + sizeof(FileHeader)
        public static readonly int GlobalSaveOffset = FileHeaderSize + CheckpointBlock.HeaderSize;

        private const bool DefaultCompress = true;
        private const int RecordHeader = 4; // sizeof(int)

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
            jsonSerializer = new CheckpointJsonSerializer();
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

        public void SerializeRecord<T>(long offset, T data, bool compress = DefaultCompress)
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

            // Debug.Log($"serialize type={typeof(T)} json={Encoding.UTF8.GetString(mem.GetBuffer(), 0, (int)mem.Position)}");
            AppendRecord(offset, new ByteSegment(mem.GetBuffer(), 0, (int)mem.Position));
        }

        public T DeserializeRecord<T>(long offset, bool compress = DefaultCompress)
        {
            try
            {
                var record = GetRecord(offset);
                // Debug.Log($"deserialize type={typeof(T)} json={record.ReadString(0)})");
                using var mem = record.ToStream();
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

            if (!fileHeader.SequenceEqual(FileHeader) || version != Version)
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
            using var sr = new StringWriter();
            using var jr = new JsonTextWriter(sr);
            jsonSerializer.Serialize(jr, obj);
            jr.Flush();
            jr.Close();
            r.Write(sr.ToString());
        }
    }
}
