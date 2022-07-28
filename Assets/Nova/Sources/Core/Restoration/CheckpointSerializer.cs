using System;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace Nova
{
    public class CheckpointCorruptedException : Exception
    {

    }

    public class CheckpointSerializer : IDisposable
    {
        private const int Version = 3;
        private static readonly TimeSpan BackupTime = TimeSpan.FromMinutes(5);
        private static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");

        private readonly BinaryFormatter formatter = new BinaryFormatter();
        private bool disposed = false;
        private readonly string path;
        private FileStream file;
        private string backupPath => path + ".old";
        private DateTime lastBackup = DateTime.Now;

        public CheckpointSerializer() { }

        public CheckpointSerializer(string path)
        {
            this.path = path;
            file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
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
            file.Close();
            file.Dispose();
        }

        public T SafeRead<T>(string path)
        {
            try
            {
                try
                {
                    using (var fs = File.OpenRead(path))
                    {
                        var result = Read<T>(fs);
                        return result;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Nova: {path} is corrupted.\n{e.Message}\nTry to recover...");
                    var oldPath = path + ".old";
                    using (var fs = File.OpenRead(oldPath))
                    {
                        var result = Read<T>(fs);

                        // Recover only if the old file is good
                        File.Delete(path); // no exception if not exist
                        File.Move(oldPath, path); // exception if exists

                        return result;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Nova: Error loading {path}.\n{e.Message}");
                throw; // Nested exception cannot display full message here
            }
        }

        private bool alertOnSafeWriteFail = true;

        public void SafeWrite<T>(T obj, string path)
        {
#if UNITY_EDITOR
            Debug.Log($"SafeWrite {obj:GetType()} {path}");
#endif

            try
            {
                var oldPath = path + ".old";
                if (File.Exists(path))
                {
                    File.Delete(oldPath); // no exception if not exist
                    File.Move(path, oldPath); // exception if exists
                }

                using (var fs = File.OpenWrite(path)) // overwrite if needed
                {
                    Write(obj, fs); // May be interrupted
                }

                File.Delete(oldPath);
            }
            catch (Exception)
            {
                // If there is some problem with Alert.Show, we need to avoid infinite recursion
                if (alertOnSafeWriteFail)
                {
                    alertOnSafeWriteFail = false;
                    Alert.Show(null, "bookmark.save.fail");
                }

                throw;
            }
        }

        private T Read<T>(Stream s)
        {
            using (var bw = new BinaryReader(s))
            {
                var fileHeader = bw.ReadBytes(FileHeader.Length);
                Utils.RuntimeAssert(FileHeader.SequenceEqual(fileHeader), "Invalid save file format.");

                int version = bw.ReadInt32();
                Utils.RuntimeAssert(Version >= version, "Save file is incompatible with the current version of Nova.");

                using (var compressed = new DeflateStream(s, CompressionMode.Decompress))
                using (var uncompressed = new MemoryStream())
                {
                    compressed.CopyTo(uncompressed);
                    uncompressed.Position = 0;
                    return (T)formatter.Deserialize(uncompressed);
                }
            }
        }

        private void Write<T>(T obj, Stream s)
        {
            using (var bw = new BinaryWriter(s))
            {
                bw.Write(FileHeader);
                bw.Write(Version);

                using (var compressed = new DeflateStream(s, CompressionMode.Compress))
                using (var uncompressed = new MemoryStream())
                {
                    formatter.Serialize(uncompressed, obj);
                    uncompressed.Position = 0;
                    uncompressed.CopyTo(compressed);
                }
            }
        }
    }
}
