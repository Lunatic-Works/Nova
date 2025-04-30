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

        public static CheckpointCorruptedException InvalidRecordSize(long offset, int size)
        {
            return new CheckpointCorruptedException($"Record @{offset} has invalid size {size}");
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
        private static readonly byte[] CorruptFileHeader = Enumerable.Repeat((byte)254, FileHeader.Length).ToArray();
        public static readonly int FileHeaderSize = FileHeader.Length + 4; // size of FileHeader + Version
        public static readonly int GlobalSaveOffset = FileHeaderSize + CheckpointBlock.HeaderSize;

        private const bool DefaultCompress = true;
        private const int RecordHeader = 4; // sizeof(int), storing the size of the record
        private const int MaxRecordSize = CheckpointBlock.BlockSize * 256;

        private readonly JsonSerializer jsonSerializer;
        private readonly string path;
        private FileStream file;
        private long endBlock;
        private readonly LRUCache<long, CheckpointBlock> cachedBlocks;
        private readonly bool frozen;

        // Hack: headerCorrupted is initialized to true,
        // so UpdateFileHeader can run when the file is written for the first time
        private bool headerCorrupted = true;

        public CheckpointSerializer(string path, bool frozen)
        {
            this.path = path;
            this.frozen = frozen;
            // 1M block cache
            cachedBlocks = new LRUCache<long, CheckpointBlock>(256, true);
            jsonSerializer = new CheckpointJsonSerializer();
        }

        public void Open()
        {
            file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            endBlock = CheckpointBlock.GetBlockID(file.Length);
            if (endBlock < 1)
            {
                AppendBlock();
            }
            else
            {
                ReadFileHeader(file, out var matchHeader, out var version);
                if (!matchHeader || version != Version)
                {
                    throw CheckpointCorruptedException.BadHeader;
                }

                headerCorrupted = false;
            }
        }

        public void Dispose()
        {
            cachedBlocks.Clear();
            if (file != null)
            {
                UpdateFileHeader(false);
                file.Close();
                file.Dispose();
            }

            headerCorrupted = true;
        }

        private CheckpointBlock GetBlock(long id)
        {
            if (!cachedBlocks.TryGetValue(id, out var block))
            {
                block = CheckpointBlock.FromFile(file, id, OnBlockFlush);
                cachedBlocks[id] = block;
            }

            return block;
        }

        private CheckpointBlock GetBlockIndex(long offset, out int index)
        {
            return GetBlock(CheckpointBlock.GetBlockIDIndex(offset, out index));
        }

        public ByteSegment GetRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var segment = block.segment;
            if (index + RecordHeader > segment.Count)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            var count = segment.ReadInt(index);
            if (count <= 0 || count > MaxRecordSize)
            {
                throw CheckpointCorruptedException.InvalidRecordSize(offset, count);
            }

            index += RecordHeader;
            if (index + count <= segment.Count)
            {
                return segment.Slice(index, count);
            }

            // need concat multiple blocks
            var buf = new byte[count];
            var pos = 0;
            while (true)
            {
                var _count = Math.Min(count - pos, segment.Count - index);
                segment.ReadBytes(index, new ByteSegment(buf, pos, _count));
                pos += _count;
                if (pos >= count)
                {
                    break;
                }

                if (block.nextBlock == 0)
                {
                    throw CheckpointCorruptedException.RecordOverflow(offset);
                }

                block = GetBlock(block.nextBlock);
                segment = block.segment;
                index = 0;
            }

            return new ByteSegment(buf);
        }

        public int GetRecordSize(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var segment = block.segment;
            if (index + RecordHeader > segment.Count)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            var count = segment.ReadInt(index);
            if (count <= 0 || count > MaxRecordSize)
            {
                throw CheckpointCorruptedException.InvalidRecordSize(offset, count);
            }

            return count;
        }

        private CheckpointBlock AppendBlock()
        {
            var id = endBlock;
            ++endBlock;
            var block = new CheckpointBlock(file, id, OnBlockFlush);
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

        // Create a new linked list
        public long BeginRecord()
        {
            var block = AppendBlock();
            return block.dataOffset;
        }

        // Get the offset of the next record in the same linked list
        // Allocates blocks until the next record starts
        public long NextRecord(long offset)
        {
            var block = GetBlockIndex(offset, out var index);
            var count = block.segment.ReadInt(index);
            if (count <= 0 || count > MaxRecordSize)
            {
                throw CheckpointCorruptedException.InvalidRecordSize(offset, count);
            }

            index += RecordHeader + count;
            while (index + RecordHeader > CheckpointBlock.DataSize)
            {
                index -= CheckpointBlock.DataSize;
                block = NextBlock(block);
            }

            // index < 0 if the last block does not have enough space for RecordHeader
            index = Math.Max(index, 0);
            return block.dataOffset + index;
        }

        // Create or update a record at the given offset
        // If update, assuming that the size is unchanged
        public void AppendRecord(long offset, ByteSegment bytes)
        {
            // Debug.Log($"append record @{offset} size={bytes.Count}");

            if (bytes.Count > MaxRecordSize)
            {
                throw CheckpointCorruptedException.InvalidRecordSize(offset, bytes.Count);
            }

            var block = GetBlockIndex(offset, out var index);
            var segment = block.segment;
            if (index + RecordHeader > segment.Count)
            {
                throw CheckpointCorruptedException.RecordOverflow(offset);
            }

            segment.WriteInt(index, bytes.Count);
            index += RecordHeader;

            var pos = 0;
            while (true)
            {
                var _count = Math.Min(bytes.Count - pos, segment.Count - index);
                segment.WriteBytes(index, bytes.Slice(pos, _count));
                block.MarkDirty();
                pos += _count;
                if (pos >= bytes.Count)
                {
                    break;
                }

                block = NextBlock(block);
                segment = block.segment;
                index = 0;
            }
        }

        public void SerializeRecord<T>(long offset, T data, bool compress = DefaultCompress)
        {
            using var mem = new MemoryStream();
            if (compress)
            {
                using var compressor = new DeflateStream(mem, CompressionMode.Compress, true);
                using var sw = new StreamWriter(compressor, Encoding.UTF8, 1024, true);
                jsonSerializer.Serialize(sw, data, typeof(T));
            }
            else
            {
                using var sw = new StreamWriter(mem, Encoding.UTF8, 1024, true);
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
                if (compress)
                {
                    using var decompressor = new DeflateStream(mem, CompressionMode.Decompress);
                    using var sr = new StreamReader(decompressor, Encoding.UTF8);
                    using var jr = new JsonTextReader(sr);
                    return jsonSerializer.Deserialize<T>(jr);
                }
                else
                {
                    using var sr = new StreamReader(mem, Encoding.UTF8);
                    using var jr = new JsonTextReader(sr);
                    return jsonSerializer.Deserialize<T>(jr);
                }
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
                block.Value.Flush(false);
            }

            UpdateFileHeader(false);
            file.Flush();

            // var end = Stopwatch.GetTimestamp();
            // Debug.Log($"flush {start}->{end}");
        }

        public Bookmark ReadBookmark(string path, bool compress = DefaultCompress)
        {
            using var fs = File.OpenRead(path);
            using var r = new BinaryReader(fs);
            var fileHeader = r.ReadBytes(FileHeader.Length);
            var version = r.ReadInt32();
            if (!fileHeader.SequenceEqual(FileHeader) || version != Version)
            {
                throw CheckpointCorruptedException.BadHeader;
            }

            if (compress)
            {
                using var decompressor = new DeflateStream(fs, CompressionMode.Decompress);
                using var sr = new StreamReader(decompressor, Encoding.UTF8);
                using var jr = new JsonTextReader(sr);
                return jsonSerializer.Deserialize<Bookmark>(jr);
            }
            else
            {
                using var sr = new StreamReader(fs, Encoding.UTF8);
                using var jr = new JsonTextReader(sr);
                return jsonSerializer.Deserialize<Bookmark>(jr);
            }
        }

        public void WriteBookmark(string path, Bookmark obj, bool compress = DefaultCompress)
        {
            using var fs = File.OpenWrite(path);
            using var w = new BinaryWriter(fs);
            w.Write(FileHeader);
            w.Write(Version);

            if (compress)
            {
                using var compressor = new DeflateStream(fs, CompressionMode.Compress);
                using var sw = new StreamWriter(compressor, Encoding.UTF8);
                jsonSerializer.Serialize(sw, obj);
            }
            else
            {
                using var sw = new StreamWriter(fs, Encoding.UTF8);
                jsonSerializer.Serialize(sw, obj);
            }
        }

        public void ReadFileHeader(Stream fs, out bool matchHeader, out int version)
        {
            var header = new byte[FileHeaderSize];
            fs.Read(header, 0, FileHeaderSize);

            matchHeader = FileHeader.SequenceEqual(header.Take(FileHeader.Length));
            version = BitConverter.ToInt32(header, FileHeader.Length);
        }

        private void OnBlockFlush()
        {
            bool corrupt = cachedBlocks.Any(pair => pair.Value.dirty);
            UpdateFileHeader(corrupt);
        }

        private void UpdateFileHeader(bool corrupt)
        {
            if (frozen || corrupt == headerCorrupted)
            {
                return;
            }

            // Debug.Log($"update header: {corrupt}");
            file.Seek(0, SeekOrigin.Begin);
            var header = corrupt ? CorruptFileHeader : FileHeader;
            var version = BitConverter.GetBytes(Version);
            file.Write(header, 0, FileHeader.Length);
            file.Write(version, 0, 4);
            file.Flush();
            headerCorrupted = corrupt;
        }
    }
}
