using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace Nova
{
    public class CheckpointSerializer
    {
        private const int Version = 2;

        private static readonly byte[] FileHeader = Encoding.ASCII.GetBytes("NOVASAVE");

        private readonly BinaryFormatter formatter = new BinaryFormatter();

        public T SafeRead<T>(string path, Action<T> assertion)
        {
            try
            {
                try
                {
                    using (var fs = File.OpenRead(path))
                    {
                        var result = Read<T>(fs);
                        assertion(result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Nova: {path} is corrupted: {ex.Message}\nTry to recover...");
                    var oldPath = path + ".old";
                    using (var fs = File.OpenRead(oldPath))
                    {
                        var result = Read<T>(fs);
                        assertion(result);

                        // Recover only if the old file is good
                        File.Delete(path); // no exception if not exist
                        File.Move(oldPath, path); // exception if exists

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Nova: Error loading {path}: {ex.Message}");
                throw; // Nested exception cannot display full message here
            }
        }

        public T SafeRead<T>(string path)
        {
            return SafeRead<T>(path, _ => { });
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
            catch (Exception ex)
            {
                // If there is some problem with Alert.Show, we need to avoid infinite recursion
                if (alertOnSafeWriteFail)
                {
                    alertOnSafeWriteFail = false;
                    Alert.Show(I18n.__("bookmark.save.fail"), ex.Message);
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