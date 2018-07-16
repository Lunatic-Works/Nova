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
        public LuaFunction action;

        /// <value>
        /// The text to display. How to display is a problem of UI design
        /// </value>
        public string text;
    }
}