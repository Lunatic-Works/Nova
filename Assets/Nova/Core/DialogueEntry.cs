using LuaInterface;

namespace Nova
{
    /// <summary>
    /// A dialogue entry contains the text to display and some actions to execute.
    /// </summary>
    /// <remarks>
    /// DialogueEntry is immutable
    /// </remarks>
    public class DialogueEntry
    {
        /// <value>
        /// The action to execute when the game processes to this point. The action can contain every thing
        /// you want, like showing amazing VFX, changing BGM, make the character smile or cry, as long as
        /// you can imagine.
        /// </value>
        private readonly LuaFunction _action;

        /// <value>
        /// The text to display. How to display is a problem of UI design
        /// </value>
        public string text { get; private set; }

        public DialogueEntry(string text, LuaFunction action = null)
        {
            _action = action;
            this.text = text;
        }

        /// <summary>
        /// Execute the action stored in this dialogue entry
        /// </summary>
        public void ExecuteAction()
        {
            if (_action != null)
            {
                _action.Call();
            }
        }
    }
}