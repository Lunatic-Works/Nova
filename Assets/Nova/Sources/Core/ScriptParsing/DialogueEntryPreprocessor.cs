using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nova
{
    [ExportCustomType]
    public static class DialogueEntryPreprocessor
    {
        private const int PreloadSteps = 5;

        private enum OutOfBound
        {
            Discard,
            Clamp
        }

        private class ActionGeneratorRule
        {
            public readonly Func<GroupCollection, string> outFunc;
            public readonly DialogueActionStage? source;
            public readonly DialogueActionStage? target;
            public readonly int deltaDialogueIndex;
            public readonly OutOfBound outOfBound;

            public ActionGeneratorRule(Func<GroupCollection, string> outFunc, DialogueActionStage? source,
                DialogueActionStage? target, int deltaDialogueIndex, OutOfBound outOfBound)
            {
                this.outFunc = outFunc;
                this.source = source;
                this.target = target;
                this.deltaDialogueIndex = deltaDialogueIndex;
                this.outOfBound = outOfBound;
            }
        }

        private class ActionGenerator
        {
            public readonly Regex pattern;
            public readonly List<ActionGeneratorRule> rules = new List<ActionGeneratorRule>();

            public ActionGenerator(Regex pattern)
            {
                this.pattern = pattern;
            }
        }

        private static readonly Dictionary<string, ActionGenerator> ActionGenerators =
            new Dictionary<string, ActionGenerator>();

        public static void ClearPatterns()
        {
            ActionGenerators.Clear();
        }

        private static ActionGenerator EnsureActionGenerator(string funcName, string pattern)
        {
            Utils.RuntimeAssert(!ActionGenerators.ContainsKey(funcName), $"Duplicate preload rules for {funcName}");
            var generator = new ActionGenerator(new Regex(pattern,
                RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline));
            ActionGenerators[funcName] = generator;
            return generator;
        }

        // Generate `preload(obj, 'resource')` when matching `func(obj, 'resource', ...)` or `...(func, obj, 'resource', ...)`
        // TODO: handle line break in patterns
        public static void AddPattern(string funcName)
        {
            var generator = EnsureActionGenerator(funcName,
                $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<obj>[^\s,]+)\s*,\s*(?<res>['""][^'""]+['""])");
            generator.rules.Add(new ActionGeneratorRule(
                groups => $"preload({groups["obj"].Value}, {groups["res"].Value})\n",
                null,
                DialogueActionStage.Default,
                PreloadSteps,
                OutOfBound.Clamp
            ));
            generator.rules.Add(new ActionGeneratorRule(
                groups => $"unpreload({groups["obj"].Value}, {groups["res"].Value})\n",
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Clamp
            ));
        }

        // Generate `preload(obj, 'resource')` when matching `func('resource', ...)` or `...(func, 'resource', ...)`
        public static void AddPatternWithObject(string funcName, string objName)
        {
            var generator =
                EnsureActionGenerator(funcName, $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<res>['""][^'""]+['""])");
            generator.rules.Add(new ActionGeneratorRule(
                groups => $"preload({objName}, {groups["res"].Value})\n",
                null,
                DialogueActionStage.Default,
                PreloadSteps,
                OutOfBound.Clamp
            ));
            generator.rules.Add(new ActionGeneratorRule(
                groups => $"unpreload({objName}, {groups["res"].Value})\n",
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Clamp
            ));
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func(obj, {'resource_1', 'resource_2', ...}, ...)` or `...(func, obj, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternForTable(string funcName)
        {
            var generator = EnsureActionGenerator(funcName,
                $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*(?<obj>[^\s,]+)\s*,\s*\{{(?<res>[^\}}]+)\}}");
            generator.rules.Add(new ActionGeneratorRule(
                groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"preload({groups["obj"].Value}, {res})\n")
                ),
                null,
                DialogueActionStage.Default,
                PreloadSteps,
                OutOfBound.Clamp
            ));
            generator.rules.Add(new ActionGeneratorRule(
                groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"unpreload({groups["obj"].Value}, {res})\n")
                ),
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Clamp
            ));
        }

        // Generate `preload(obj, 'resource_1')\npreload(obj, 'resource_2')\n...`
        // when matching `func({'resource_1', 'resource_2', ...}, ...)` or `...(func, {'resource_1', 'resource_2', ...}, ...)`
        public static void AddPatternWithObjectForTable(string funcName, string objName)
        {
            var generator = EnsureActionGenerator(funcName, $@"(^|[\s\(:]){funcName}(\(|\s*,)\s*\{{(?<res>[^\}}]+)\}}");
            generator.rules.Add(new ActionGeneratorRule(
                groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"preload({objName}, {res})\n")
                ),
                null,
                DialogueActionStage.Default,
                PreloadSteps,
                OutOfBound.Clamp
            ));
            generator.rules.Add(new ActionGeneratorRule(
                groups => string.Concat(
                    groups["res"].Value.Split(',').Select(res => $"unpreload({objName}, {res})\n")
                ),
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Clamp
            ));
        }

        // Generate `preload(obj, 'resource')` when matching `func(...)` or `...(func, ...)`
        public static void AddPatternWithObjectAndResource(string funcName, string objName, string resource)
        {
            var generator = EnsureActionGenerator(funcName, $@"(^|[\s\(:]){funcName}(\(|\s*,)");
            generator.rules.Add(new ActionGeneratorRule(
                _ => $"preload({objName}, '{resource}')\n",
                null,
                DialogueActionStage.Default,
                PreloadSteps,
                OutOfBound.Clamp
            ));
            generator.rules.Add(new ActionGeneratorRule(
                _ => $"unpreload({objName}, '{resource}')\n",
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Clamp
            ));
        }

        // The first dialogue of a node must have a checkpoint,
        // so we don't need to add a checkpoint when the index is out of bound
        public static void AddCheckpointPattern(string funcName, string outFuncName)
        {
            var generator = EnsureActionGenerator(funcName, $@"(^|[\s\(:]){funcName}(\(|\s*,)");
            generator.rules.Add(new ActionGeneratorRule(
                _ => $"{outFuncName}()\n",
                null,
                DialogueActionStage.Default,
                1,
                OutOfBound.Discard
            ));
        }

        public static void AddCheckpointNextPattern(string funcName, string outFuncName)
        {
            var generator = EnsureActionGenerator(funcName, $@"(^|[\s\(:]){funcName}(\(|\s*,)");
            generator.rules.Add(new ActionGeneratorRule(
                _ => $"{outFuncName}()\n",
                null,
                DialogueActionStage.Default,
                0,
                OutOfBound.Discard
            ));
        }

        public static void GenerateActions(IReadOnlyDictionary<DialogueActionStage, StringBuilder[]> codeBuilders,
            string code, DialogueActionStage stage, int dialogueIndex)
        {
            foreach (var pair in ActionGenerators)
            {
                // Coarse test of whether the function exists in the code
                if (code.IndexOf(pair.Key, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var generator = pair.Value;
                var matches = generator.pattern.Matches(code);
                foreach (Match match in matches)
                {
                    foreach (var rule in generator.rules)
                    {
                        if (rule.source != null && rule.source != stage)
                        {
                            continue;
                        }

                        var newIndex = dialogueIndex - rule.deltaDialogueIndex;
                        if (newIndex < 0)
                        {
                            if (rule.outOfBound == OutOfBound.Discard)
                            {
                                continue;
                            }
                            else // rule.outOfBound == OutOfBound.Clamp
                            {
                                newIndex = 0;
                            }
                        }

                        var newCode = rule.outFunc.Invoke(match.Groups);

                        foreach (DialogueActionStage target in Enum.GetValues(typeof(DialogueActionStage)))
                        {
                            if (rule.target != null && rule.target != target)
                            {
                                continue;
                            }

                            var codeBuilder = codeBuilders[target][newIndex];
                            if (codeBuilder == null)
                            {
                                codeBuilders[target][newIndex] = codeBuilder = new StringBuilder();
                            }

                            codeBuilder.Append(newCode);

                            // Debug.Log($"codeBuilder: <color=magenta>{codeBuilder}</color>");
                        }
                    }
                }
            }
        }
    }
}
