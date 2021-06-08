using Nova.Exceptions;
using System;
using System.Collections.Generic;
using LuaInterface;
using UnityEngine;

namespace Nova
{
    public class LocalizedDialogueEntry
    {
        public string displayName;
        public string dialogue;
    }

    /// <summary>
    /// Dialogue entry without action. Used for serialization.
    /// </summary>
    [Serializable]
    public class DialogueDisplayData
    {
        public readonly string characterName;
        public readonly Dictionary<SystemLanguage, string> displayNames;
        public readonly Dictionary<SystemLanguage, string> dialogues;

        public DialogueDisplayData(string characterName, Dictionary<SystemLanguage, string> displayNames,
            Dictionary<SystemLanguage, string> dialogues)
        {
            this.characterName = characterName;
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
    /// A dialogue entry contains the character name and the dialogue in each locale, and the action to execute.
    /// </summary>
    public class DialogueEntry
    {
        /// <value>Internally used character name.</value>
        public readonly string characterName;

        /// <value>Displayed character name for each locale.</value>
        public readonly Dictionary<SystemLanguage, string> displayNames = new Dictionary<SystemLanguage, string>();

        /// <value>Displayed dialogue for each locale.</value>
        public readonly Dictionary<SystemLanguage, string> dialogues = new Dictionary<SystemLanguage, string>();

        /// <value>
        /// The action to execute when the game processes to this point.
        /// </value>
        private readonly LuaFunction action;

        public DialogueEntry(string characterName, string displayName, string dialogue, LuaFunction action)
        {
            this.characterName = characterName;
            displayNames[I18n.DefaultLocale] = displayName;
            dialogues[I18n.DefaultLocale] = dialogue;
            this.action = action;
        }

        public void AddLocale(SystemLanguage locale, LocalizedDialogueEntry entry)
        {
            displayNames[locale] = entry.displayName;
            dialogues[locale] = entry.dialogue;
        }

        /// <summary>
        /// Execute the action stored in this dialogue entry
        /// </summary>
        public void ExecuteAction()
        {
            if (action != null)
            {
                try
                {
                    action.Call();
                }
                catch (LuaException ex)
                {
                    throw new ScriptActionException(
                        $"Nova: Exception occurred when executing action: {I18n.__(dialogues)}", ex);
                }
            }
        }

        public DialogueDisplayData displayData => new DialogueDisplayData(characterName, displayNames, dialogues);
    }
}