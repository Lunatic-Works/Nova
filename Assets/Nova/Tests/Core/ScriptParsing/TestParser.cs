using Nova.Parser;
using NUnit.Framework;

namespace Tests
{
    public class TestParser
    {
        [Test]
        public void TestParseTextBlock()
        {
            var blocks = Parser.ParseBlocks(@"
Text1
Text2

Text3

");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.Text, blocks[0].type);
            Assert.AreEqual("Text1", blocks[0].content);
            Assert.AreEqual(BlockType.Text, blocks[1].type);
            Assert.AreEqual("Text2", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.Text, blocks[3].type);
            Assert.AreEqual("Text3", blocks[3].content);
        }

        [Test]
        public void TestParseExecutionBlock()
        {
            var blocks = Parser.ParseBlocks(@"
<| code1() |>
<| code2() |>

@<| code3() |>


");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual(" code1() ", blocks[0].content);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.AreEqual(" code2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.AreEqual(" code3() ", blocks[3].content);
        }

        [Test]
        public void TestParseComment()
        {
            var blocks = Parser.ParseBlocks(@"
<|-- Comment |>
code1() |>
<| code2() |>

@<| --[[ Comment |> ]] code3() |>
");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual("-- Comment |>\ncode1() ", blocks[0].content);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.AreEqual(" code2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.AreEqual(" --[[ Comment |> ]] code3() ", blocks[3].content);
        }

        [Test]
        public void TestParserSimple()
        {
            var blocks = Parser.ParseBlocks(@"
@<| hello_world |>

<|
--[[ <| |> ]] |>
Text

");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.EagerExecution, blocks[0].type);
            Assert.AreEqual(" hello_world ", blocks[0].content);
            Assert.AreEqual(BlockType.Separator, blocks[1].type);
            Assert.AreEqual(BlockType.LazyExecution, blocks[2].type);
            Assert.AreEqual("\n--[[ <| |> ]] ", blocks[2].content);
            Assert.AreEqual(BlockType.Text, blocks[3].type);
            Assert.AreEqual("Text", blocks[3].content);
        }

        [Test]
        public void TestBlockWithEmptyLine()
        {
            var blocks = Parser.ParseBlocks(@"
<| code1() |>
<| code2()

code2_2() |>

@<| code3() |>


");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual(" code1() ", blocks[0].content);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.AreEqual(" code2()\n\ncode2_2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.AreEqual(" code3() ", blocks[3].content);
        }

        [Test]
        public void TestUnpaired()
        {
            try
            {
                Parser.ParseBlocks("<| code_unpaired");
            }
            catch (ParserException)
            {
                Assert.IsTrue(true);
                return;
            }

            Assert.IsTrue(false, "Exception not thrown");
        }

        [Test]
        public void TestString()
        {
            var blocks = Parser.ParseBlocks(@"
<| print 'hello\' |>' |>
<| code2()

[[
multiline[[nested |>]]
]]

code2_2() |>

@<| code3() |>


");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual(" print 'hello\\' |>' ", blocks[0].content);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.AreEqual(" code2()\n\n[[\nmultiline[[nested |>]]\n]]\n\ncode2_2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.AreEqual(" code3() ", blocks[3].content);
        }

        [Test]
        public void TestAttribute()
        {
            var blocks = Parser.ParseBlocks(@"
[label = entry, '$name' = 'hello\' world']<|
print 'hello\' |>' |>
<| code2()

[[
multiline[[nested |>]]
]]

code2_2() |>

@[flag]<| code3() |>


");

            Assert.NotNull(blocks);
            Assert.AreEqual(4, blocks.Count);

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual("\nprint 'hello\\' |>' ", blocks[0].content);
            Assert.AreEqual("entry", blocks[0].attributes["label"]);
            Assert.AreEqual("hello\' world", blocks[0].attributes["$name"]);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.AreEqual(" code2()\n\n[[\nmultiline[[nested |>]]\n]]\n\ncode2_2() ", blocks[1].content);
            Assert.IsNull(blocks[1].attributes);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.IsNull(blocks[2].attributes);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.AreEqual(" code3() ", blocks[3].content);
            Assert.IsTrue(blocks[3].attributes.ContainsKey("flag"));
            Assert.IsNull(blocks[3].attributes["flag"]);
        }
    }
}
