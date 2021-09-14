using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LuaInterface;

namespace Nova
{
    [ExportCustomType]
    public static class ScriptDialogueEntryParser
    {
        private const int PreloadDialogueSteps = 5;
        private const string LazyExecutionBlockPattern = @"^<\|((?:.|[\r\n])*?)\|>\r?\n";
        private const string LuaCommentPattern = @"--.*";
        private const string NameDialoguePattern = @"(.*?)(?:：：|::)(.*)";
        private const string ActionBeforeLazyBlock = "action_before_lazy_block('{0}')\n";
        private const string ActionAfterLazyBlock = "action_after_lazy_block('{0}')\n";

        private class ActionGenerators
        {
            public Func<GroupCollection, string> preload;
            public Func<GroupCollection, string> unpreload;
            public Func<GroupCollection, string> forceCheckpoint;
        }

        private static readonly Dictionary<string, ActionGenerators> PatternToActionGenerator =
            new Dictionary<string, ActionGenerators>();

        public static void AddCheckpointPattern(string triggeringFuncName, string yieldingFuncName)
        {
            PatternToActionGenerator[triggeringFuncName] = new ActionGenerators
            {
                forceCheckpoint = _ => $"{yieldingFuncName}()\n"
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func(obj, 'resource', ...)` or `...(func, obj, 'resource', ...)`
        // TODO: handle line break in patterns
        public static void AddPattern(string funcName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *(?<obj>[^ ,]+) *, *'(?<res>[^']+)'";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({groups["obj"].Value}, '{groups["res"].Value}')\n",
                unpreload = groups => $"unpreload({groups["obj"].Value}, '{groups["res"].Value}')\n"
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func('resource', ...)` or `...(func, 'resource', ...)`
        public static void AddPatternWithObject(string funcName, string objName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *'(?<res>[^']+)'";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, '{groups["res"].Value}')\n",
                unpreload = groups => $"unpreload({objName}, '{groups["res"].Value}')\n"
            };
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func(obj, {'resource_1', 'resource_2', ...}, ...)` or `...(func, obj, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternForTable(string funcName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *(?<obj>[^ ,]+) *, *\{{(?<res>[^\}}]+)\}}";
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
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *\{{(?<res>[^\}}]+)\}}";
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
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,)";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, '{resource}')\n",
                unpreload = groups => $"unpreload({objName}, '{resource}')\n"
            };
        }

        /// <summary>
        /// Parse a dialogue entry text
        /// </summary>
        /// <remarks>
        /// A dialogue entry can have one or none lazy execution block. The lazy execution block (if exists) should be
        /// placed above the dialogue entry text.
        /// </remarks>
        /// <param name="dialogueEntryText"></param>
        /// <param name="code"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        private static void ParseText(string dialogueEntryText, out string code, out string text)
        {
            int textStartIndex = 0;
            var lazyExecutionBlockMatch = Regex.Match(dialogueEntryText, LazyExecutionBlockPattern);
            if (lazyExecutionBlockMatch.Success)
            {
                code = lazyExecutionBlockMatch.Groups[1].Value;
                code = code.Trim();
                if (string.IsNullOrEmpty(code))
                {
                    code = null;
                }
                else
                {
                    code += '\n';
                    // Debug.Log($"code: <color=blue>{code}</color>");
                }

                textStartIndex += lazyExecutionBlockMatch.Length;
            }
            else
            {
                code = null;
            }

            text = dialogueEntryText.Substring(textStartIndex);
            // Debug.Log($"text: <color=green>{text}</color>");
        }

        private static void GenerateActions(string code, out StringBuilder preloadActions,
            out StringBuilder unpreloadActions, out StringBuilder forceCheckpointActions)
        {
            preloadActions = null;
            unpreloadActions = null;
            forceCheckpointActions = null;

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

                    if (generators.forceCheckpoint != null)
                    {
                        if (forceCheckpointActions == null)
                        {
                            forceCheckpointActions = new StringBuilder();
                        }

                        forceCheckpointActions.Append(generators.forceCheckpoint.Invoke(match.Groups));
                    }
                }
            }

            // if (preloadActions.Length > 0) Debug.Log($"preloadActions: <color=magenta>{preloadActions.ToString()}</color>");
            // if (unpreloadActions.Length > 0) Debug.Log($"unpreloadActions: <color=magenta>{unpreloadActions.ToString()}</color>");
            // if (forceCheckpointActions.Length > 0) Debug.Log($"forceCheckpointActions: <color=magenta>{forceCheckpointActions.ToString()}</color>");
        }

        private static void AppendActions(IList<StringBuilder> indexToCode, int index, StringBuilder actions)
        {
            if (actions == null || actions.Length == 0)
            {
                return;
            }

            var old = indexToCode[index];
            if (old == null || old.Length == 0)
            {
                indexToCode[index] = actions;
            }
            else
            {
                indexToCode[index] = old.Append(actions);
            }
        }

        private static void ParseNameDialogue(string text, out string characterName, out string dialogue)
        {
            var m = Regex.Match(text, NameDialoguePattern);
            if (m.Success)
            {
                characterName = m.Groups[1].Value;
                dialogue = m.Groups[2].Value;
            }
            else
            {
                characterName = "";
                dialogue = text;
            }
        }

        public static List<DialogueEntry> ParseDialogueEntries(IReadOnlyList<string> dialogueEntryTexts)
        {
            var indexToCode = new StringBuilder[dialogueEntryTexts.Count];
            var indexToText = new string[dialogueEntryTexts.Count];
            for (int i = 0; i < dialogueEntryTexts.Count; ++i)
            {
                ParseText(dialogueEntryTexts[i], out string code, out string text);
                indexToText[i] = text;

                if (!string.IsNullOrEmpty(code))
                {
                    indexToCode[i] = new StringBuilder(code);

                    GenerateActions(code, out StringBuilder preloadActions,
                        out StringBuilder unpreloadActions, out StringBuilder forceCheckpointActions);
                    AppendActions(indexToCode, Math.Max(i - PreloadDialogueSteps, 0), preloadActions);
                    AppendActions(indexToCode, i, unpreloadActions);

                    // The first entry of a node must have a real checkpoint, so no need to force there
                    if (i > 0)
                    {
                        AppendActions(indexToCode, i - 1, forceCheckpointActions);
                    }
                }
            }

            var combinedCode = new StringBuilder();
            var results = new List<DialogueEntry>();
            for (int i = 0; i < dialogueEntryTexts.Count; ++i)
            {
                string text = indexToText[i];
                ParseNameDialogue(text, out string characterName, out string dialogue);

                combinedCode.Clear();
                combinedCode.AppendFormat(ActionBeforeLazyBlock, characterName);
                var code = indexToCode[i];
                if (code != null)
                {
                    combinedCode.Append(code);
                }

                combinedCode.AppendFormat(ActionAfterLazyBlock, characterName);
                string combinedCodeStr = combinedCode.ToString();

                LuaFunction action = null;
                if (!string.IsNullOrEmpty(combinedCodeStr))
                {
                    action = LuaRuntime.Instance.WrapClosure(combinedCodeStr);
                    if (action == null)
                    {
                        throw new ScriptParseException(
                            $"Syntax error while parsing lazy execution block:\nText: {text}\nCode: {combinedCodeStr}");
                    }

                    // Debug.Log($"combinedCodeStr: <color=magenta>{combinedCodeStr}</color>");
                }

                // TODO: there may be some grammar to set different internal and displayed character names
                results.Add(new DialogueEntry(characterName, characterName, dialogue, action));
            }

            return results;
        }

        public static List<LocalizedDialogueEntry> ParseLocalizedDialogueEntries(
            IReadOnlyList<string> dialogueEntryTexts)
        {
            var results = new List<LocalizedDialogueEntry>();
            foreach (var text in dialogueEntryTexts)
            {
                ParseNameDialogue(text, out string characterName, out string dialogue);
                results.Add(new LocalizedDialogueEntry {displayName = characterName, dialogue = dialogue});
            }

            return results;
        }
    }
}