using LuaInterface;
using System;
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
    public class ChoiceImageInformation
    {
        public readonly string name;
        public readonly float positionX;
        public readonly float positionY;
        public readonly float scale;

        public ChoiceImageInformation(string name, float positionX, float positionY, float scale)
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
    public class BranchInformation : IEquatable<BranchInformation>
    {
        /// <summary>
        /// The internal name of the branch, auto generated from ScriptLoader.RegisterBranch()
        /// The name should be unique in a flow chart node
        /// </summary>
        public readonly string name;

        public readonly Dictionary<SystemLanguage, string> texts;
        public readonly ChoiceImageInformation imageInfo;
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

        public BranchInformation(string name, string text, ChoiceImageInformation imageInfo, BranchMode mode,
            LuaFunction condition)
        {
            this.name = name;
            texts = new Dictionary<SystemLanguage, string> { [I18n.DefaultLocale] = text };
            this.imageInfo = imageInfo;
            this.mode = mode;
            this.condition = condition;
        }

        public void AddLocalizedText(SystemLanguage locale, string text)
        {
            texts[locale] = text;
        }

        public override bool Equals(object obj) => obj is BranchInformation other && Equals(other);

        public bool Equals(BranchInformation other) => !(other is null) && name == other.name;

        public override int GetHashCode() => name.GetHashCode();

        public static bool operator ==(BranchInformation a, BranchInformation b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(BranchInformation a, BranchInformation b) => !(a == b);
    }
}
