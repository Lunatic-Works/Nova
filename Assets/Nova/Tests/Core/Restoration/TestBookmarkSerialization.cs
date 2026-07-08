using System.Collections.Generic;
using System.IO;
using Nova;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class TestBookmarkSerialization
    {
        private string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "NovaBookmarkTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [Test]
        public void TestBookmarkRoundTripWithoutScreenshot()
        {
            var path = Path.Combine(tempDirectory, "sav301.nsav");
            var serializer = new CheckpointSerializer(Path.Combine(tempDirectory, "global.nsav"), false);
            var bookmark = new Bookmark(4096, 12)
            {
                globalSaveIdentifier = 12345,
                description = new DialogueDisplayData(
                    new Dictionary<SystemLanguage, string> {[SystemLanguage.English] = "Alice"},
                    new Dictionary<SystemLanguage, string> {[SystemLanguage.English] = "Hello"})
            };

            serializer.WriteBookmark(path, bookmark);
            var restored = serializer.ReadBookmark(path);

            Assert.AreEqual(bookmark.nodeOffset, restored.nodeOffset);
            Assert.AreEqual(bookmark.dialogueIndex, restored.dialogueIndex);
            Assert.AreEqual(bookmark.globalSaveIdentifier, restored.globalSaveIdentifier);
            Assert.AreEqual(bookmark.creationTime, restored.creationTime);
            Assert.AreEqual("Alice", restored.description.displayNames[SystemLanguage.English]);
            Assert.AreEqual("Hello", restored.description.dialogues[SystemLanguage.English]);
            Assert.IsNull(restored.screenshot);
        }

        [Test]
        public void TestBookmarkRejectsBadHeader()
        {
            var path = Path.Combine(tempDirectory, "sav301.nsav");
            File.WriteAllBytes(path, new byte[CheckpointSerializer.FileHeaderSize]);
            var serializer = new CheckpointSerializer(Path.Combine(tempDirectory, "global.nsav"), false);

            Assert.Throws<CheckpointCorruptedException>(() => serializer.ReadBookmark(path));
        }

        [Test]
        public void TestBookmarkWriteTruncatesExistingFile()
        {
            var path = Path.Combine(tempDirectory, "sav301.nsav");
            var serializer = new CheckpointSerializer(Path.Combine(tempDirectory, "global.nsav"), false);
            var largeBookmark = new Bookmark(4096, 12)
            {
                description = new DialogueDisplayData(null, new Dictionary<SystemLanguage, string>
                {
                    [SystemLanguage.English] = new string('x', 2048)
                })
            };
            var smallBookmark = new Bookmark(8192, 3);

            serializer.WriteBookmark(path, largeBookmark, false);
            var largeSize = new FileInfo(path).Length;
            serializer.WriteBookmark(path, smallBookmark, false);
            var smallSize = new FileInfo(path).Length;
            var restored = serializer.ReadBookmark(path, false);

            Assert.Less(smallSize, largeSize);
            Assert.AreEqual(smallBookmark.nodeOffset, restored.nodeOffset);
            Assert.AreEqual(smallBookmark.dialogueIndex, restored.dialogueIndex);
        }
    }
}
