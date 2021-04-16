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

    [ExportCustomType]
    public class BranchImageInformation
    {
        public readonly string name;
        public readonly float positionX;
        public readonly float positionY;
        public readonly float scale;

        public BranchImageInformation(string name, float positionX, float positionY, float scale)
        {
            this.name = name;
            this.positionX = positionX;
            this.positionY = positionY;
            this.scale = scale;
        }
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

        public readonly BranchImageInformation imageInfo;

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

        public BranchInformation(string name, string text, BranchImageInformation imageInfo, BranchMode mode,
            LuaFunction condition)
        {
            this.name = name;
            this.text = text;
            this.imageInfo = imageInfo;
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