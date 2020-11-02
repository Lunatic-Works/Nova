using LuaInterface;

namespace Nova
{
    /// <summary>
    /// The information of branch
    /// </summary>
    /// <remarks>
    /// BranchInformation is immutable
    /// </remarks>
    public class BranchInformation
    {
        /// <value>
        /// The name of this branch.
        /// The name should be unique among all branches that derived from the same flow chart node
        /// </value>
        public string name { get; private set; }

        /// <summary>
        /// A branch information can have some other data, like descriptions or lua functions,
        /// which can be customized by scripts
        /// </summary>
        public readonly LuaTable metadata;

        /// <summary>
        /// The default branch value, used for Normal flow chart node
        /// </summary>
        /// <remarks>
        /// Since default value owns the default name, all other branches should not have the name __@default
        /// </remarks>
        public static readonly BranchInformation Default = new BranchInformation {name = "__@default"};

        public BranchInformation(string name = null, LuaTable metadata = null)
        {
            this.name = name;
            this.metadata = metadata;
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