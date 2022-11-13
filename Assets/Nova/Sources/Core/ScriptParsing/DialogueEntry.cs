using LuaInterface;
using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    public class LocalizedDialogueEntry
    {
        public string displayName;
        public string dialogue;
    }

    /// <summary>
    /// Dialogue entry without actions. Used for serialization.
    /// </summary>
    [Serializable]
    public class DialogueDisplayData
    {
        public readonly Dictionary<SystemLanguage, string> displayNames;
        public readonly Dictionary<SystemLanguage, string> dialogues;

        public DialogueDisplayData(Dictionary<SystemLanguage, string> displayNames,
            Dictionary<SystemLanguage, string> dialogues)
        {
            this.displayNames = displayNames;
            this.dialogues = dialogues;
        }

        public string FormatNameDialogue()
        {
            var name = I18n.__(displayNames);
            var dialogue = I18n.__(dialogues);
            if (string.IsNullOrEmpty(name))
            {
                return dialogue;
            }
            else
            {
                return string.Format(I18n.__("format.namedialogue"), name, dialogue);
            }
        }
    }

    /// <summary>
    /// A dialogue entry contains the character name and the dialogue text in each locale, and the actions to execute.
    /// </summary>
    public class DialogueEntry
    {
        /// <summary>
        /// Internally used character name.
        /// </summary>
        public readonly string characterName;

        /// <summary>
        /// Displayed character name in each locale, before string interpolation.
        /// </summary>
        private readonly Dictionary<SystemLanguage, string> displayNames;

        /// <summary>
        /// Displayed dialogue text in each locale, before string interpolation.
        /// </summary>
        private readonly Dictionary<SystemLanguage, string> dialogues;

        /// <summary>
        /// The actions to execute when the game processes to this point.
        /// </summary>
        private readonly Dictionary<DialogueActionStage, LuaFunction> actions;

        public readonly ulong textHash;

        public DialogueEntry(string characterName, string displayName, string dialogue,
            Dictionary<DialogueActionStage, LuaFunction> actions, ulong textHash)
        {
            this.characterName = characterName;
            displayNames = new Dictionary<SystemLanguage, string> { [I18n.DefaultLocale] = displayName };
            dialogues = new Dictionary<SystemLanguage, string> { [I18n.DefaultLocale] = dialogue };
            this.actions = actions;
            this.textHash = textHash;
        }

        public void AddLocalized(SystemLanguage locale, LocalizedDialogueEntry entry)
        {
            displayNames[locale] = entry.displayName;
            dialogues[locale] = entry.dialogue;
        }

        // DialogueDisplayData is cached only if there is no need to interpolate
        private DialogueDisplayData cachedDisplayData;
        private bool needInterpolate;

        public bool NeedInterpolate()
        {
            if (cachedDisplayData == null && !needInterpolate)
            {
                var func = LuaRuntime.Instance.GetFunction("text_need_interpolate");
                needInterpolate = displayNames.Any(x => func.Invoke<string, bool>(x.Value))
                                  || dialogues.Any(x => func.Invoke<string, bool>(x.Value));
                if (!needInterpolate)
                {
                    cachedDisplayData = new DialogueDisplayData(displayNames, dialogues);
                }
            }

            return needInterpolate;
        }

        public DialogueDisplayData GetDisplayData()
        {
            if (NeedInterpolate())
            {
                var interpolatedDisplayNames = displayNames.ToDictionary(x => x.Key, x => InterpolateText(x.Value));
                var interpolatedDialogues = dialogues.ToDictionary(x => x.Key, x => InterpolateText(x.Value));
                return new DialogueDisplayData(interpolatedDisplayNames, interpolatedDialogues);
            }

            return cachedDisplayData;
        }

        /// <summary>
        /// Execute the action stored in this dialogue entry.
        /// </summary>
        public void ExecuteAction(DialogueActionStage stage, bool isRestoring)
        {
            if (actions.TryGetValue(stage, out var action))
            {
                LuaRuntime.Instance.UpdateExecutionContext(new ExecutionContext(ExecutionMode.Lazy, stage,
                    isRestoring));
                try
                {
                    action.Call();
                }
                catch (LuaException e)
                {
                    throw new ScriptActionException(
                        $"Nova: Exception occurred when executing action: {I18n.__(dialogues)}", e);
                }
            }
        }

        private const string ActionCoroutineName = "__Nova.action_coroutine";

        public static string WrapCoroutine(string code)
        {
            return $@"
{ActionCoroutineName} = coroutine.start(function()
    __Nova.coroutineHelper:AcquireActionPause()
    {code}
    __Nova.coroutineHelper:ReleaseActionPause()
end)";
        }

        public static void StopActionCoroutine()
        {
            // Do nothing if the coroutine is nil
            LuaRuntime.Instance.GetFunction("coroutine.stop").Call(ActionCoroutineName);
        }

        private static string InterpolateText(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            return LuaRuntime.Instance.GetFunction("interpolate_text").Invoke<string, string>(s);
        }
    }
}
