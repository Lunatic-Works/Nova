﻿using Nova.Script;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class TestParser
    {
        [Test]
        public void TestParseTextBlock()
        {
            var parsed = Parser.Parse(@"
Text1
Text2

Text3

");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
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
            var parsed = Parser.Parse(@"
<| code1() |>
<| code2() |>

@<| code3() |>


");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
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
            var parsed = Parser.Parse(@"
<|-- Comment |>
code1() |>
<| code2() |>

@<| --[[ Comment |> ]] code3() |>
");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
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
            var parsed = Parser.Parse(@"
@<| hello_world |>

<|
--[[ <| |> ]] |>
Text

");
            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(blocks.Count, 4);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
            }

            Assert.AreEqual(blocks[0].type, BlockType.EagerExecution);
            Assert.AreEqual(blocks[0].content, " hello_world ");
            Assert.AreEqual(blocks[1].type, BlockType.Separator);
            Assert.AreEqual(blocks[2].type, BlockType.LazyExecution);
            Assert.AreEqual(blocks[2].content, "\n--[[ <| |> ]] ");
            Assert.AreEqual(blocks[3].type, BlockType.Text);
            Assert.AreEqual(blocks[3].content, "Text");
        }

        [Test]
        public void TestBlockWithEmptyLine()
        {
            var parsed = Parser.Parse(@"
<| code1() |>
<| code2()

code2_2() |>

@<| code3() |>


");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
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
                var parsed =  Parser.Parse("<| code_unpaired");
            }
            catch (ParseException)
            {
                Assert.IsTrue(true);
                return;
            }

            Assert.IsTrue(false, "Exception not thrown");
        }

        [Test]
        public void TestString()
        {
            var parsed =  Parser.Parse(@"
<| print 'hello\' |>' |>
<| code2()

[[
multiline[[nested |>]]
]]

code2_2() |>

@<| code3() |>


");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.IsTrue(blocks[0].attributes.Count == 0);
            Assert.AreEqual(" print 'hello\\' |>' ", blocks[0].content);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.IsTrue(blocks[1].attributes.Count == 0);
            Assert.AreEqual(" code2()\n\n[[\nmultiline[[nested |>]]\n]]\n\ncode2_2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.IsTrue(blocks[3].attributes.Count == 0);
            Assert.AreEqual(" code3() ", blocks[3].content);
        }

        [Test]
        public void TestAttribute()
        {
            var parsed =  Parser.Parse(@"
[label = entry, '$name' = 'hello\' world']<|
print 'hello\' |>' |>
<| code2()

[[
multiline[[nested |>]]
]]

code2_2() |>

@[flag]<| code3() |>


");

            Assert.NotNull(parsed);
            var blocks = parsed.blocks;
            Assert.AreEqual(4, blocks.Count);
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.attributes);
            }

            Assert.AreEqual(BlockType.LazyExecution, blocks[0].type);
            Assert.AreEqual("\nprint 'hello\\' |>' ", blocks[0].content);
            Assert.AreEqual("entry", blocks[0].attributes["label"]);
            Assert.AreEqual("hello\' world", blocks[0].attributes["$name"]);
            Assert.AreEqual(BlockType.LazyExecution, blocks[1].type);
            Assert.IsTrue(blocks[1].attributes.Count == 0);
            Assert.AreEqual(" code2()\n\n[[\nmultiline[[nested |>]]\n]]\n\ncode2_2() ", blocks[1].content);
            Assert.AreEqual(BlockType.Separator, blocks[2].type);
            Assert.AreEqual(BlockType.EagerExecution, blocks[3].type);
            Assert.IsTrue(blocks[3].attributes.ContainsKey("flag"));
            Assert.IsNull(blocks[3].attributes["flag"]);
            Assert.AreEqual(" code3() ", blocks[3].content);
        }
    }
}
