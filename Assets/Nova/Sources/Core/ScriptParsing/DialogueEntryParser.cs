using LuaInterface;
using Nova.Script;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nova
{
    public static class DialogueEntryParser
    {
        private static readonly Regex LuaCommentPattern =
            new Regex(@"--.*", RegexOptions.Compiled);

        private static readonly Regex LuaMultilineCommentPattern =
            new Regex(@"--\[(=*)\[[^\]]*\]\1\]", RegexOptions.Compiled);

        private static readonly Regex NameDialoguePattern =
            new Regex(@"(?<name>[^/：:]*)(//(?<hidden>[^：:]*))?(：：|::)(?<dialogue>(.|\n)*)",
                RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private static readonly Regex MarkdownCodePattern =
            new Regex(@"`([^`]*)`", RegexOptions.Compiled);

        private static readonly Regex MarkdownLinkPattern =
            new Regex(@"\[([^\]]*)\]\(([^\)]*)\)", RegexOptions.Compiled);

        private const string ActionBeforeLazyBlock = "action_before_lazy_block('{0}')\n";
        private const string ActionAfterLazyBlock = "action_after_lazy_block('{0}')\n";

        private static void ParseNameDialogue(string text, out string displayName, out string hiddenName,
            out string dialogue)
        {
            if (text.IndexOf("：：", StringComparison.Ordinal) < 0 && text.IndexOf("::", StringComparison.Ordinal) < 0)
            {
                displayName = "";
                hiddenName = "";
                dialogue = text;
                return;
            }

            var m = NameDialoguePattern.Match(text);
            if (m.Success)
            {
                displayName = m.Groups["name"].Value;
                hiddenName = m.Groups["hidden"].Value;
                dialogue = m.Groups["dialogue"].Value;
            }
            else
            {
                displayName = "";
                hiddenName = "";
                dialogue = text;
            }
        }

        /// <remarks>
        /// There can be multiple dialogue texts in the same chunk. They are concatenated into one, separated with newlines.
        /// </remarks>
        private static string GetText(ScriptLoader.Chunk chunk)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var block in chunk.blocks)
            {
                if (block.type == BlockType.Text)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append('\n');
                    }

                    sb.Append(block.content);
                }
            }

            var text = sb.ToString();

            // Markdown syntaxes used in tutorials
            // They are not in the NovaScript spec. If they interfere with your scenarios or you have performance concern,
            // you can comment out them.
            text = MarkdownCodePattern.Replace(text, @"<style=Code>$1</style>");
            text = MarkdownLinkPattern.Replace(text, @"<link=""$2""><style=Link>$1</style></link>");

            // Debug.Log($"text: <color=green>{text}</color>");
            return text;
        }

        private static string GetStageName(DialogueActionStage stage)
        {
            switch (stage)
            {
                case DialogueActionStage.BeforeCheckpoint:
                    return "before_checkpoint";
                case DialogueActionStage.Default:
                    return "";
                case DialogueActionStage.AfterDialogue:
                    return "after_dialogue";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetCode(ScriptLoader.Chunk chunk, DialogueActionStage stage)
        {
            var sb = new StringBuilder();
            var stageName = GetStageName(stage);
            const string stageKey = "stage";
            foreach (var block in chunk.blocks)
            {
                if (block.type == BlockType.LazyExecution)
                {
                    if (block.attributes == null || !block.attributes.TryGetValue(stageKey, out var stageValue))
                    {
                        stageValue = "";
                    }

                    if (stageValue != stageName)
                    {
                        continue;
                    }

                    sb.Append(block.content);
                    sb.Append("\n");
                }
            }

            var code = sb.ToString();

            // If you don't use Lua multiline comment, or any Lua comment at all,
            // you can commit out the following for better performance
            code = LuaMultilineCommentPattern.Replace(code, "");
            code = LuaCommentPattern.Replace(code, "");

            code = code.Trim();
            if (code == "")
            {
                code = null;
            }

            // if (code != null) Debug.Log($"code: <color=blue>{code}</color>");
            return code;
        }

        private static void GenerateActions(IReadOnlyDictionary<DialogueActionStage, string[]> codes,
            IReadOnlyList<string> characterNames)
        {
            var dialogueCount = characterNames.Count;
            var codeBuilders = new Dictionary<DialogueActionStage, StringBuilder[]>();
            foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
            {
                codeBuilders[stage] = new StringBuilder[dialogueCount];
            }

            for (var i = 0; i < dialogueCount; ++i)
            {
                foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
                {
                    var code = codes[stage][i];
                    var hasCode = !string.IsNullOrEmpty(code);
                    if (stage != DialogueActionStage.Default && !hasCode)
                    {
                        continue;
                    }

                    var codeBuilder = new StringBuilder();
                    if (stage == DialogueActionStage.Default)
                    {
                        codeBuilder.AppendFormat(ActionBeforeLazyBlock, characterNames[i]);
                    }

                    if (hasCode)
                    {
                        codeBuilder.Append("-- Begin original code block\n");
                        codeBuilder.Append(code);
                        codeBuilder.Append("\n-- End original code block\n");
                    }

                    codeBuilders[stage][i] = codeBuilder;

                    if (hasCode)
                    {
                        DialogueEntryPreprocessor.GenerateActions(codeBuilders, code, stage, i);
                    }
                }
            }

            for (var i = 0; i < dialogueCount; ++i)
            {
                foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
                {
                    var codeBuilder = codeBuilders[stage][i];
                    if (codeBuilder == null)
                    {
                        continue;
                    }

                    if (stage == DialogueActionStage.Default)
                    {
                        codeBuilder.AppendFormat(ActionAfterLazyBlock, characterNames[i]);
                    }

                    var preprocessedCode = codeBuilder.ToString().Trim();
                    // Debug.Log($"preprocessedCode: <color=orange>{preprocessedCode}</color>");
                    codes[stage][i] = preprocessedCode;
                }
            }
        }

        public static IReadOnlyList<DialogueEntry> ParseDialogueEntries(IReadOnlyList<ScriptLoader.Chunk> chunks)
        {
            var codes = new Dictionary<DialogueActionStage, string[]>();
            foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
            {
                codes[stage] = new string[chunks.Count];
            }

            var characterNames = new string[chunks.Count];
            var displayNames = new string[chunks.Count];
            var dialogues = new string[chunks.Count];
            var hiddenNames = new Dictionary<string, string>();
            for (var i = 0; i < chunks.Count; ++i)
            {
                foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
                {
                    codes[stage][i] = GetCode(chunks[i], stage);
                }

                var text = GetText(chunks[i]);
                ParseNameDialogue(text, out var displayName, out var hiddenName, out dialogues[i]);

                if (string.IsNullOrEmpty(hiddenName))
                {
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        if (hiddenNames.ContainsKey(displayName))
                        {
                            hiddenName = hiddenNames[displayName];
                        }
                        else
                        {
                            hiddenName = displayName;
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        hiddenNames[displayName] = hiddenName;
                    }
                }

                characterNames[i] = hiddenName;
                displayNames[i] = displayName;
            }

            GenerateActions(codes, characterNames);

            var results = new List<DialogueEntry>();
            for (var i = 0; i < chunks.Count; ++i)
            {
                var characterName = characterNames[i];
                var displayName = displayNames[i];
                var dialogue = dialogues[i];

                var actions = new Dictionary<DialogueActionStage, LuaFunction>();
                foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
                {
                    var code = codes[stage][i];
                    if (string.IsNullOrEmpty(code))
                    {
                        continue;
                    }

                    code = DialogueEntry.WrapCoroutine(code);
                    var action = LuaRuntime.Instance.WrapClosure(code);
                    if (action == null)
                    {
                        throw new ParserException(
                            "Syntax error while parsing lazy execution block.\n" +
                            $"characterName: {characterName}, displayName: {displayName}, dialogue: {dialogue}\n" +
                            $"stage: {stage}, code: {code}");
                    }

                    actions.Add(stage, action);
                }

                results.Add(new DialogueEntry(characterName, displayName, dialogue, actions, chunks[i].GetHashUlong()));
            }

            return results;
        }

        public static IReadOnlyList<LocalizedDialogueEntry> ParseLocalizedDialogueEntries(
            IEnumerable<ScriptLoader.Chunk> chunks)
        {
            var results = new List<LocalizedDialogueEntry>();
            foreach (var chunk in chunks)
            {
                var text = GetText(chunk);
                ParseNameDialogue(text, out var displayName, out var hiddenName, out var dialogue);
                if (!string.IsNullOrEmpty(hiddenName))
                {
                    throw new ParserException(
                        "Cannot set internal character name in non-default locale.\n" +
                        $"hiddenName: {hiddenName}, displayName: {displayName}, dialogue: {dialogue}");
                }

                results.Add(new LocalizedDialogueEntry(displayName, dialogue));
            }

            return results;
        }
    }
}
