using LuaInterface;
using Nova.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nova
{
    [ExportCustomType]
    public static class ScriptDialogueEntryParser
    {
        private const int PreloadDialogueSteps = 5;
        private const string LuaCommentPattern = @"--.*";
        private const string LuaMultilineCommentPattern = @"--\[(=*)\[(.|\n)*?\]\1\]";
        private const string NameDialoguePattern = @"(?<name>.*?)(//(?<hidden>.*?))?(：：|::)(?<dialogue>(.|\n)*)";
        private const string ActionBeforeLazyBlock = "action_before_lazy_block('{0}')\n";
        private const string ActionAfterLazyBlock = "action_after_lazy_block('{0}')\n";

        private class ActionGenerators
        {
            public Func<GroupCollection, string> preload;
            public Func<GroupCollection, string> unpreload;
            public Func<GroupCollection, string> checkpoint;
        }

        private static readonly Dictionary<string, ActionGenerators> PatternToActionGenerator =
            new Dictionary<string, ActionGenerators>();

        // Generate `preload(obj, 'resource')` when matching `func(obj, 'resource', ...)` or `...(func, obj, 'resource', ...)`
        // TODO: handle line break in patterns
        public static void AddPattern(string funcName)
        {
            string pattern = $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<obj>[^\s,]+)\s*,\s*(?<res>['""][^'""]+['""])";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({groups["obj"].Value}, {groups["res"].Value})\n",
                unpreload = groups => $"unpreload({groups["obj"].Value}, {groups["res"].Value})\n"
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func('resource', ...)` or `...(func, 'resource', ...)`
        public static void AddPatternWithObject(string funcName, string objName)
        {
            string pattern = $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<res>['""][^'""]+['""])";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, {groups["res"].Value})\n",
                unpreload = groups => $"unpreload({objName}, {groups["res"].Value})\n"
            };
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func(obj, {'resource_1', 'resource_2', ...}, ...)` or `...(func, obj, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternForTable(string funcName)
        {
            string pattern = $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<obj>[^\s,]+)\s*,\s*\{{(?<res>[^\}}]+)\}}";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"preload({groups["obj"].Value}, {res})\n")
                ),
                unpreload = groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"unpreload({groups["obj"].Value}, {res})\n")
                )
            };
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func({'resource_1', 'resource_2', ...}, ...)` or `...(func, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternWithObjectForTable(string funcName, string objName)
        {
            string pattern = $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*\{{(?<res>[^\}}]+)\}}";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"preload({objName}, {res})\n")
                ),
                unpreload = groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"unpreload({objName}, {res})\n")
                )
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func(...)` or `...(func, ...)`
        public static void AddPatternWithObjectAndResource(string funcName, string objName, string resource)
        {
            string pattern = $@"(^|[\s\(:]){funcName}(\(|\s*,)";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, '{resource}')\n",
                unpreload = groups => $"unpreload({objName}, '{resource}')\n"
            };
        }

        public static void AddCheckpointPattern(string triggeringFuncName, string yieldingFuncName)
        {
            string pattern = $@"(^|[\s\(:]){triggeringFuncName}(\(|\s*,)";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                checkpoint = _ => $"{yieldingFuncName}()\n"
            };
        }

        public static void AddCheckpointNextPattern(string triggeringFuncName, string yieldingFuncName)
        {
            string pattern = $@"(^|[\s\(:]){triggeringFuncName}(\(|\s*,)";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                unpreload = _ => $"{yieldingFuncName}()\n"
            };
        }

        private static void GenerateActions(string code, out StringBuilder preloadActions,
            out StringBuilder unpreloadActions, out StringBuilder checkpointActions)
        {
            preloadActions = null;
            unpreloadActions = null;
            checkpointActions = null;

            code = Regex.Replace(code, LuaMultilineCommentPattern, "");
            code = Regex.Replace(code, LuaCommentPattern, "");

            foreach (var pair in PatternToActionGenerator)
            {
                var matches = Regex.Matches(code, pair.Key, RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    var generators = pair.Value;

                    if (generators.preload != null)
                    {
                        if (preloadActions == null)
                        {
                            preloadActions = new StringBuilder();
                        }

                        preloadActions.Append(generators.preload.Invoke(match.Groups));
                    }

                    if (generators.unpreload != null)
                    {
                        if (unpreloadActions == null)
                        {
                            unpreloadActions = new StringBuilder();
                        }

                        unpreloadActions.Append(generators.unpreload.Invoke(match.Groups));
                    }

                    if (generators.checkpoint != null)
                    {
                        if (checkpointActions == null)
                        {
                            checkpointActions = new StringBuilder();
                        }

                        checkpointActions.Append(generators.checkpoint.Invoke(match.Groups));
                    }
                }
            }

            // if (preloadActions.Length > 0) Debug.Log($"preloadActions: <color=magenta>{preloadActions.ToString()}</color>");
            // if (unpreloadActions.Length > 0) Debug.Log($"unpreloadActions: <color=magenta>{unpreloadActions.ToString()}</color>");
            // if (checkpointActions.Length > 0) Debug.Log($"checkpointActions: <color=magenta>{checkpointActions.ToString()}</color>");
        }

        private static void AppendActions(IList<StringBuilder> codeBuilders, int index, StringBuilder actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return;
            }

            var old = codeBuilders[index];
            if (old == null || old.Length == 0)
            {
                codeBuilders[index] = actions;
            }
            else
            {
                codeBuilders[index] = old.Append(actions);
            }
        }

        private static void ParseNameDialogue(string text, out string displayName, out string hiddenName,
            out string dialogue)
        {
            var m = Regex.Match(text, NameDialoguePattern, RegexOptions.ExplicitCapture);
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

            // Normalize line endings
            sb.Replace("\r", "");

            var text = sb.ToString();

            // Markdown syntaxes used in tutorials
            // They are not in the NovaScript spec. If they interfere with your scenarios or you have performance concern,
            // you can comment out them.
            text = Regex.Replace(text, @"`([^`]*)`", @"<style=Code>$1</style>");
            text = Regex.Replace(text, @"\[([^\]]*)\]\(([^\)]*)\)", @"<link=""$2""><style=Link>$1</style></link>");

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
                    if (!block.attributes.TryGetValue(stageKey, out var stageValue))
                    {
                        stageValue = "";
                    }

                    if (!stageValue.Equals(stageName))
                    {
                        continue;
                    }

                    sb.Append(block.content);
                    sb.Append("\n");
                }
            }

            var code = sb.ToString().Trim();
            if (code == "")
            {
                code = null;
            }

            // if (code != null) Debug.Log($"code: <color=blue>{code}</color>");
            return code;
        }

        private static void PatchDefaultActionCode(IReadOnlyDictionary<DialogueActionStage, string[]> codes,
            IReadOnlyList<string> characterNames)
        {
            var codeBuilders = new StringBuilder[characterNames.Count];
            for (var i = 0; i < characterNames.Count; ++i)
            {
                foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
                {
                    var code = codes[stage][i];
                    if (string.IsNullOrEmpty(code))
                    {
                        continue;
                    }

                    var codeBuilder = new StringBuilder();
                    codeBuilder.Append("-- Begin original code block\n");
                    codeBuilder.Append(code);
                    codeBuilder.Append("\n-- End original code block\n");
                    codeBuilders[i] = codeBuilder;

                    GenerateActions(code, out StringBuilder preloadActions, out StringBuilder unpreloadActions,
                        out StringBuilder checkpointActions);
                    AppendActions(codeBuilders, Math.Max(i - PreloadDialogueSteps, 0), preloadActions);
                    AppendActions(codeBuilders, i, unpreloadActions);

                    // The first entry of a node must have a checkpoint, so no need to force here
                    if (i > 0)
                    {
                        AppendActions(codeBuilders, i - 1, checkpointActions);
                    }
                }
            }

            var patchBuilder = new StringBuilder();
            for (var i = 0; i < characterNames.Count; ++i)
            {
                var characterName = characterNames[i];

                patchBuilder.Clear();
                patchBuilder.AppendFormat(ActionBeforeLazyBlock, characterName);
                var codeBuilder = codeBuilders[i];
                if (codeBuilder != null)
                {
                    patchBuilder.Append(codeBuilder);
                }

                patchBuilder.AppendFormat(ActionAfterLazyBlock, characterName);

                var patchedCode = patchBuilder.ToString();
                // Debug.Log($"patchBuilder: <color=orange>{patchedCode}</color>");
                codes[DialogueActionStage.Default][i] = patchedCode;
            }
        }

        public static IReadOnlyList<DialogueEntry> ParseDialogueEntries(IReadOnlyList<ScriptLoader.Chunk> chunks,
            IDictionary<string, string> hiddenCharacterNames)
        {
            var codes = new Dictionary<DialogueActionStage, string[]>();
            foreach (DialogueActionStage stage in Enum.GetValues(typeof(DialogueActionStage)))
            {
                codes[stage] = new string[chunks.Count];
            }

            var characterNames = new string[chunks.Count];
            var displayNames = new string[chunks.Count];
            var dialogues = new string[chunks.Count];
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
                        if (hiddenCharacterNames.ContainsKey(displayName))
                        {
                            hiddenName = hiddenCharacterNames[displayName];
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
                        hiddenCharacterNames[displayName] = hiddenName;
                    }
                }

                characterNames[i] = hiddenName;
                displayNames[i] = displayName;
            }

            PatchDefaultActionCode(codes, characterNames);

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
                        throw new ParseException(
                            "Syntax error while parsing lazy execution block\n" +
                            $"characterName: {characterName}, displayName: {displayName}, dialogue: {dialogue}\n" +
                            $"stage: {stage}, code: {code}");
                    }

                    actions.Add(stage, action);
                }

                // TODO: there may be some grammar to set different internal and displayed character names
                results.Add(new DialogueEntry(characterName, displayName, dialogue, actions));
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
                ParseNameDialogue(text, out var displayName, out var _, out var dialogue);
                results.Add(new LocalizedDialogueEntry { displayName = displayName, dialogue = dialogue });
            }

            return results;
        }
    }
}