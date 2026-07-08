using System.Collections.Generic;
using System.IO;
using Nova;
using NUnit.Framework;

namespace Tests
{
    public class TestCheckpointSerializer
    {
        private string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "NovaRestorationTests", Path.GetRandomFileName());
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
        public void TestNodeRecordSerializationRoundTrip()
        {
            var original = new NodeRecord(1234, "ノードA", 3, 0x123456789abcdef0UL)
            {
                parent = 11,
                child = 22,
                sibling = 33,
                endDialogue = 8,
                lastCheckpointDialogue = 6
            };

            var restored = new NodeRecord(original.offset, original.ToByteSegment());

            Assert.AreEqual(original.offset, restored.offset);
            Assert.AreEqual(original.parent, restored.parent);
            Assert.AreEqual(original.child, restored.child);
            Assert.AreEqual(original.sibling, restored.sibling);
            Assert.AreEqual(original.beginDialogue, restored.beginDialogue);
            Assert.AreEqual(original.endDialogue, restored.endDialogue);
            Assert.AreEqual(original.lastCheckpointDialogue, restored.lastCheckpointDialogue);
            Assert.AreEqual(original.variablesHash, restored.variablesHash);
            Assert.AreEqual(original.name, restored.name);
        }

        [Test]
        public void TestRecordsRoundTripAcrossBlocks()
        {
            var path = Path.Combine(tempDirectory, "global.nsav");
            var serializer = new CheckpointSerializer(path, false);
            serializer.Open();
            var offset = serializer.BeginRecord();
            var largeText = new string('x', CheckpointBlock.DataSize + 512);
            var data = new ReachedDialogueData("node", 7, null, true, 42UL);

            serializer.SerializeRecord<IReachedData>(offset, data);
            var nextOffset = serializer.NextRecord(offset);
            serializer.SerializeRecord<IReachedData>(nextOffset, new ReachedEndData(largeText), false);
            var endOffset = serializer.NextRecord(nextOffset);
            serializer.Flush();
            serializer.Dispose();

            serializer = new CheckpointSerializer(path, false);
            serializer.Open();
            var restoredDialogue = serializer.DeserializeRecord<IReachedData>(offset) as ReachedDialogueData;
            var restoredEnd = serializer.DeserializeRecord<IReachedData>(nextOffset, false) as ReachedEndData;

            Assert.NotNull(restoredDialogue);
            Assert.AreEqual(data.nodeName, restoredDialogue.nodeName);
            Assert.AreEqual(data.dialogueIndex, restoredDialogue.dialogueIndex);
            Assert.AreEqual(data.needInterpolate, restoredDialogue.needInterpolate);
            Assert.AreEqual(data.textHash, restoredDialogue.textHash);
            Assert.NotNull(restoredEnd);
            Assert.AreEqual(largeText, restoredEnd.endName);
            Assert.Greater(CheckpointBlock.GetBlockID(endOffset), CheckpointBlock.GetBlockID(nextOffset));
            serializer.Dispose();
        }

        [Test]
        public void TestGameStateCheckpointRoundTripWithVariables()
        {
            var path = Path.Combine(tempDirectory, "checkpoint.nsav");
            var serializer = new CheckpointSerializer(path, false);
            serializer.Open();
            var offset = serializer.BeginRecord();
            var variables = new Variables();
            variables.Set("v_bool", true);
            variables.Set("v_num", 3.5);
            variables.Set("v_text", "hello");
            var checkpoint = new GameStateCheckpoint(9, new Dictionary<string, IRestoreData>(), variables, 4);

            serializer.SerializeRecord(offset, checkpoint);
            serializer.Flush();
            serializer.Dispose();

            serializer = new CheckpointSerializer(path, false);
            serializer.Open();
            var restored = serializer.DeserializeRecord<GameStateCheckpoint>(offset);

            Assert.AreEqual(9, restored.dialogueIndex);
            Assert.AreEqual(4, restored.stepsCheckpointRestrained);
            Assert.AreEqual(true, restored.variables.Get<bool>("v_bool"));
            Assert.AreEqual(3.5, restored.variables.Get<double>("v_num"));
            Assert.AreEqual("hello", restored.variables.Get<string>("v_text"));
            Assert.IsEmpty(restored.restoreDatas);
            serializer.Dispose();
        }
    }
}
