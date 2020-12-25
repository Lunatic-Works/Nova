using LuaInterface;

namespace Nova
{
    [ExportCustomType]
    public enum BranchMode
    {
        Normal,
        Jump,
        Show,
        Enable
    }

    /// <summary>
    /// The information of branch
    /// </summary>
    /// <remarks>
    /// BranchInformation is immutable
    /// </remarks>
    public class BranchInformation
    {
        /// <summary>
        /// The internal name of the branch, auto generated from ScriptLoader.RegisterBranch()
        /// The name should be unique in a flow chart node
        /// </summary>
        public readonly string name;

        /// <summary>
        /// The text on the button to select this branch
        /// </summary>
        public readonly string text;

        public readonly BranchMode mode;
        public readonly LuaFunction condition;

        /// <summary>
        /// The default branch, used in normal flow chart nodes
        /// </summary>
        /// <remarks>
        /// Since the default branch owns the default name, all other branches should not have the name 'default'
        /// </remarks>
        public static readonly BranchInformation Default = new BranchInformation("default");

        public BranchInformation(string name)
        {
            this.name = name;
        }

        public BranchInformation(string name, string text, BranchMode mode, LuaFunction condition)
        {
            this.name = name;
            this.text = text;
            this.mode = mode;
            this.condition = condition;
        }

        /// <summary>
        /// Check if this branch is a default sequential branch
        /// </summary>
        /// <returns>
        /// true if this branch equals the default branch
        /// </returns>
        public bool IsDefaultValue()
        {
            return Equals(Default);
        }

        // BranchInformations are considered equal if they have the same name
        public override bool Equals(object obj)
        {
            return obj is BranchInformation anotherBranch && name.Equals(anotherBranch.name);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }
}