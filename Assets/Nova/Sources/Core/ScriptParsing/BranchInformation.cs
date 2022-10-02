using LuaInterface;
using System.Collections.Generic;
using UnityEngine;

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

        private readonly Dictionary<SystemLanguage, string> _texts;
        public Dictionary<SystemLanguage, string> texts => _texts;

        public readonly BranchImageInformation imageInfo;
        public readonly BranchMode mode;
        public readonly LuaFunction condition;

        /// <summary>
        /// The default branch, used in normal flow chart nodes
        /// </summary>
        /// <remarks>
        /// Since the default branch owns the default name, all other branches should not have the name '__default'
        /// </remarks>
        public static readonly BranchInformation Default = new BranchInformation("__default");

        public BranchInformation(string name)
        {
            this.name = name;
        }

        public BranchInformation(string name, string text, BranchImageInformation imageInfo, BranchMode mode,
            LuaFunction condition)
        {
            this.name = name;
            _texts = new Dictionary<SystemLanguage, string> { [I18n.DefaultLocale] = text };
            this.imageInfo = imageInfo;
            this.mode = mode;
            this.condition = condition;
        }

        public void AddLocalizedText(SystemLanguage locale, string text)
        {
            _texts[locale] = text;
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

        // BranchInformation are considered equal if they have the same name
        public override bool Equals(object obj)
        {
            return obj is BranchInformation other && name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public static bool operator ==(BranchInformation a, BranchInformation b) => a.Equals(b);

        public static bool operator !=(BranchInformation a, BranchInformation b) => !(a == b);
    }
}
