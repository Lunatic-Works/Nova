using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LuaInterface;

namespace Nova
{
    [ExportCustomType]
    public static class ScriptDialogueEntryParser
    {
        private const int PreloadDialogueSteps = 5;
        private const string LazyExecutionBlockPattern = @"^<\|((?:.|[\r\n])*?)\|>\r?\n?";
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
                forceCheckpoint = _ => $"{yieldingFuncName}()"
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func(obj, 'resource', ...)` or `...(func, obj, 'resource', ...)`
        // TODO: handle line break in patterns
        public static void AddPattern(string funcName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *(?<obj>[^ ,]+) *, *'(?<res>[^']+)'";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({groups["obj"].Value}, '{groups["res"].Value}')",
                unpreload = groups => $"unpreload({groups["obj"].Value}, '{groups["res"].Value}')"
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func('resource', ...)` or `...(func, 'resource', ...)`
        public static void AddPatternWithObject(string funcName, string objName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *'(?<res>[^']+)'";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, '{groups["res"].Value}')",
                unpreload = groups => $"unpreload({objName}, '{groups["res"].Value}')"
            };
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func(obj, {'resource_1', 'resource_2', ...}, ...)` or `...(func, obj, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternForTable(string funcName)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,) *(?<obj>[^ ,]+) *, *\{{(?<res>[^\}}]+)\}}";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => string.Join("\n",
                    groups["res"].Value.Split(',').Select(res => $"preload({groups["obj"].Value}, {res})")
                ),
                unpreload = groups => string.Join("\n",
                    groups["res"].Value.Split(',').Select(res => $"unpreload({groups["obj"].Value}, {res})")
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
                preload = groups => string.Join("\n",
                    groups["res"].Value.Split(',').Select(res => $"preload({objName}, {res})")
                ),
                unpreload = groups => string.Join("\n",
                    groups["res"].Value.Split(',').Select(res => $"unpreload({objName}, {res})")
                )
            };
        }

        // Generate `preload(obj, 'resource')` when matching `func(...)` or `...(func, ...)`
        public static void AddPatternWithObjectAndResource(string funcName, string objName, string resource)
        {
            string pattern = $@"(^|[ \(:]){funcName}(\(| *,)";
            PatternToActionGenerator[pattern] = new ActionGenerators
            {
                preload = groups => $"preload({objName}, '{resource}')",
                unpreload = groups => $"unpreload({objName}, '{resource}')"
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
                // Debug.LogFormat("Lazy code: <color=blue>{0}</color>", code);
                textStartIndex += lazyExecutionBlockMatch.Length;
            }
            else
            {
                code = null;
            }

            text = dialogueEntryText.Substring(textStartIndex);
            // Debug.LogFormat("Text: <color=green>{0}</color>", text);
        }

        private static void GenerateAdditionalActions(string code, out string preloadActions,
            out string unpreloadActions, out string forceCheckpointActions)
        {
            preloadActions = "";
            unpreloadActions = "";
            forceCheckpointActions = "";
            foreach (var pair in PatternToActionGenerator)
            {
                var matches = Regex.Matches(code, pair.Key, RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    preloadActions += pair.Value.preload?.Invoke(match.Groups) ?? "" + "\n";
                    unpreloadActions += pair.Value.unpreload?.Invoke(match.Groups) ?? "" + "\n";
                    forceCheckpointActions += pair.Value.forceCheckpoint?.Invoke(match.Groups) ?? "" + "\n";
                }
            }

            // if (preloadActions != "") Debug.LogFormat("<color=blue>{0}</color>", preloadActions);
            // if (unpreloadActions != "") Debug.LogFormat("<color=blue>{0}</color>", unpreloadActions);
        }

        private static void CombineActions(IDictionary<int, string> dict, int index, string actions)
        {
            if (string.IsNullOrEmpty(actions)) return;
            dict.TryGetValue(index, out string old);
            dict[index] = (old ?? "") + actions;
        }

        public static List<DialogueEntry> ParseDialogueEntries(IReadOnlyList<string> dialogueEntryTexts)
        {
            var indexToCode = new string[dialogueEntryTexts.Count];
            var indexToText = new string[dialogueEntryTexts.Count];
            var indexToAdditionalActions = new Dictionary<int, string>();
            for (int i = 0; i < dialogueEntryTexts.Count; ++i)
            {
                ParseText(dialogueEntryTexts[i], out string code, out string text);
                indexToCode[i] = code;
                indexToText[i] = text;
                if (!string.IsNullOrEmpty(code))
                {
                    GenerateAdditionalActions(code, out string preloadActions, out string unpreloadActions,
                        out string forceCheckpointActions);
                    CombineActions(indexToAdditionalActions, Math.Max(i - PreloadDialogueSteps, 0), preloadActions);
                    CombineActions(indexToAdditionalActions, i, unpreloadActions);

                    // The first entry of a node must have a real checkpoint, so no need to force here
                    if (i > 0)
                    {
                        CombineActions(indexToAdditionalActions, i - 1, forceCheckpointActions);
                    }
                }
            }

            var results = new List<DialogueEntry>();
            for (int i = 0; i < dialogueEntryTexts.Count; ++i)
            {
                string code = indexToCode[i];
                string text = indexToText[i];
                indexToAdditionalActions.TryGetValue(i, out string additionalActions);

                var m = Regex.Match(text, @"(.*?)(?:：：|::)(.*)");
                string characterName, dialogue;
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

                LuaFunction action = null;
                string combinedCode = string.Format(ActionBeforeLazyBlock, characterName)
                                      + (code ?? "")
                                      + (additionalActions ?? "")
                                      + string.Format(ActionAfterLazyBlock, characterName);
                if (!string.IsNullOrEmpty(combinedCode))
                {
                    action = LuaRuntime.Instance.WrapClosure(combinedCode);
                    if (action == null)
                    {
                        throw new ScriptParseException(
                            $"Syntax error while parsing lazy execution block:\nText: {text}\nCode: {combinedCode}");
                    }
                }

                // TODO: there may be some grammar to set different internal and displayed character names
                results.Add(new DialogueEntry(characterName, characterName, dialogue, action));
            }

            return results;
        }

        public static List<LocalizedDialogueEntry> ParseLocalizedDialogueEntries(IReadOnlyList<string> dialogueEntryTexts)
        {
            var results = new List<LocalizedDialogueEntry>();
            foreach (var text in dialogueEntryTexts)
            {
                var m = Regex.Match(text, @"(.*?)(?:：：|::)(.*)");
                string characterName, dialogue;
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

                results.Add(new LocalizedDialogueEntry {displayName = characterName, dialogue = dialogue});
            }

            return results;
        }
    }
}