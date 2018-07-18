using LuaInterface;

namespace Nova
{
    /// <summary>
    /// A dialogue entry contains the text to display and some actions to execute.
    /// </summary>
    public class DialogueEntry
    {
        /// <value>
        /// The action to execute when the game processes to this point. The action can contain every thing
        /// you want, like showing amazing VFX, changing BGM, make the character smile or cry, as long as
        /// you can imagine.
        /// </value>
        private LuaFunction _action;

        /// <value>
        /// The text to display. How to display is a problem of UI design
        /// </value>
        public string text;

        /// <summary>
        /// Set the action of this dialogue entry
        /// </summary>
        /// <param name="action">A lua function as the action</param>
        public void SetAction(LuaFunction action)
        {
            if (_action != null)
            {
                _action.Dispose();
            }

            _action = action;
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