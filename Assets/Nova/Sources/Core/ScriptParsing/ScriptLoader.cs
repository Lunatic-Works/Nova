using LuaInterface;
using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// The class that loads scripts and constructs the flow chart tree.
    /// </summary>
    [ExportCustomType]
    public class ScriptLoader
    {
        // Variable indicating whether the script loader is initialized
        private bool inited;

        /// <summary>
        /// Initialize the script loader. This method will load all text asset files in the given folder, parse all the
        /// scripts, and construct the flow chart tree.
        /// </summary>
        /// <remarks>
        /// All scripts will be parsed, so this method might take some time to finish.
        /// </remarks>
        /// <param name="path">
        /// The folder containing all the scripts. All text assets in that folder will be loaded and parsed, so
        /// there should be no text assets other than script files in that folder.
        /// </param>
        public void Init(string path)
        {
            if (inited)
            {
                return;
            }

            ForceInit(path);

            inited = true;
        }

        private FlowChartTree flowChartTree;

        private FlowChartNode currentNode = null;

        // Current locale of the state machine
        public SystemLanguage stateLocale;

        private class LazyBindingEntry
        {
            public FlowChartNode from;
            public string destination;
            public BranchInformation branchInfo;
        }

        private List<LazyBindingEntry> lazyBindingLinks;

        private HashSet<string> onlyIncludedNames;

        private void InitOnlyIncludedNames()
        {
            onlyIncludedNames = new HashSet<string>(LuaRuntime.Instance
                .DoString<LuaTable>("return only_included_scenario_names").ToArray().Cast<string>());
        }

        public void ForceInit(string path)
        {
            flowChartTree = new FlowChartTree();
            currentNode = null;
            stateLocale = I18n.DefaultLocale;
            lazyBindingLinks = new List<LazyBindingEntry>();

            // requires.lua is executed and ScriptDialogueEntryParser.PatternToActionGenerator is filled before calling ParseScript()
            LuaRuntime.Instance.BindObject("scriptLoader", this);
            InitOnlyIncludedNames();

            foreach (var locale in I18n.SupportedLocales)
            {
                stateLocale = locale;

                string localizedPath = path;
                if (locale != I18n.DefaultLocale)
                {
                    localizedPath = I18n.LocalePath + locale + "/" + path;
                }

                var scripts = Resources.LoadAll(localizedPath, typeof(TextAsset)).Cast<TextAsset>();
                foreach (var script in scripts)
                {
                    if (onlyIncludedNames.Count > 0 && !onlyIncludedNames.Contains(script.name))
                    {
                        continue;
                    }

#if UNITY_EDITOR
                    var scriptPath = AssetDatabase.GetAssetPath(script);
                    Debug.Log($"Nova: Parse script {scriptPath}");
#endif

                    try
                    {
                        ParseScript(script.text);
                    }
                    catch (ScriptParseException exception)
                    {
                        throw new ScriptParseException($"Failed to parse {script.name}", exception);
                    }
                }
            }

            // Bind all lazy binding entries
            BindAllLazyBindingEntries();

            // Perform sanity check
            flowChartTree.SanityCheck();

            // Construction finished, freeze the tree status
            flowChartTree.Freeze();
        }

        private void CheckInit()
        {
            Assert.IsTrue(inited, "Nova: ScriptLoader methods should be called after Init().");
        }

        /// <summary>
        /// Get the flow chart tree
        /// </summary>
        /// <remarks>This method should be called after init</remarks>
        /// <returns>The flow chart tree</returns>
        public FlowChartTree GetFlowChartTree()
        {
            CheckInit();
            return flowChartTree;
        }

        private const string EagerExecutionStartSymbol = "@<|";
        private const string EagerExecutionBlockPattern = @"@<\|((?:.|[\r\n])*?)\|>";
        private const string EmptyLinePattern = @"(?:\r?\n\s*){2,}";

        /// <summary>
        /// Parse the given script text
        /// </summary>
        /// <param name="text">Text of a script</param>
        private void ParseScript(string text)
        {
            LuaRuntime.Instance.DoString("action_new_file()");

            text = text.Trim();

            // Detect eager execution block
            int eagerExecutionStartIndex = text.IndexOf(EagerExecutionStartSymbol, StringComparison.Ordinal);
            if (eagerExecutionStartIndex != 0)
            {
                // The script file does not start with a eager execution block
                Debug.LogWarning("Nova: The script file does not start with a eager execution block. " +
                                 "All text before the first execution block will be removed.");
            }

            // No eager execution block is found, simply ignore this file
            if (eagerExecutionStartIndex < 0)
            {
                return;
            }

            text = text.Substring(eagerExecutionStartIndex);
            int lastMatchEndIndex = 0;
            foreach (Match m in Regex.Matches(text, EagerExecutionBlockPattern))
            {
                string flowChartNodeText = text.Substring(lastMatchEndIndex, m.Index - lastMatchEndIndex);
                // This method will not be executed when the execution enter this loop for the first time,
                // since the first eager execution block is definitely at the beginning of the text.
                ParseFlowChartNodeText(flowChartNodeText);
                lastMatchEndIndex = m.Index + m.Length;

                string eagerExecutionBlockCode = m.Groups[1].Value;
                // Debug.LogFormat("Eager code: <color=blue><b>{0}</b></color>", eagerExecutionBlockCode);
                DoEagerExecutionBlock(eagerExecutionBlockCode);
            }

            // A script file should ends with an eager execution block
            // Everything after the last eager execution block will be ignored
            if (lastMatchEndIndex < text.Length)
            {
                Debug.LogWarning("Nova: A script file should ends with a eager execution block, " +
                                 "which needs to refer to the next flow chart node.");
            }
        }

        /// <summary>
        /// Parse the flow chart node
        /// </summary>
        /// <remarks>
        /// The name of this method might be a little misleading, since this method actually parses the text
        /// split by eager execution blocks, while the node structure are defined by scripts in the eager execution
        /// block. A new node is created when the 'label' instruction is invoked in the eager execution block, and its
        /// content ends when either 'branch' or 'jump' instruction is called. Current implementation (2018/07/16)
        /// constructs flow chart tree with a state machine, i.e. parsed flow chart node text are pushed to
        /// the current node.
        /// </remarks>
        /// <param name="flowChartNodeText"></param>
        private void ParseFlowChartNodeText(string flowChartNodeText)
        {
            if (flowChartNodeText == null)
            {
                return;
            }

            flowChartNodeText = flowChartNodeText.Trim();
            if (string.IsNullOrEmpty(flowChartNodeText))
            {
                return;
            }

            if (currentNode == null)
            {
                throw new ArgumentException("Nova: Dangling node text " + flowChartNodeText);
            }

            var dialogueEntryTexts = Regex.Split(flowChartNodeText, EmptyLinePattern);

            if (stateLocale == I18n.DefaultLocale)
            {
                var entries = ScriptDialogueEntryParser.ParseDialogueEntries(dialogueEntryTexts);
                currentNode.SetDialogueEntries(entries);
            }
            else
            {
                var entries = ScriptDialogueEntryParser.ParseLocalizedDialogueEntries(dialogueEntryTexts);
                currentNode.AddLocaleForDialogueEntries(stateLocale, entries);
            }
        }

        /// <summary>
        /// Bind all lazy binding entries
        /// </summary>
        private void BindAllLazyBindingEntries()
        {
            foreach (var entry in lazyBindingLinks)
            {
                var node = entry.from;
                node.AddBranch(entry.branchInfo, flowChartTree.GetNode(entry.destination));
            }

            // Remove unnecessary reference
            lazyBindingLinks = null;
        }

        /// <summary>
        /// Execute code in the eager execution block
        /// </summary>
        /// <param name="eagerExecutionBlockCode"></param>
        private static void DoEagerExecutionBlock(string eagerExecutionBlockCode)
        {
            LuaRuntime.Instance.DoString(eagerExecutionBlockCode);
        }

        #region Methods called by external scripts

        /// <summary>
        /// Create a new flow chart node register it to the current constructing FlowChartTree.
        /// If the current node is a normal node, the newly created one is intended to be its
        /// succeeding node. The link between the new node and the current one will be added immediately, which
        /// will not be registered as a lazy binding link.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="name">Name of the new node</param>
        public void RegisterNewNode(string name)
        {
            var nextNode = new FlowChartNode(name);
            if (currentNode != null && currentNode.type == FlowChartNodeType.Normal)
            {
                currentNode.AddBranch(BranchInformation.Default, nextNode);
            }

            currentNode = nextNode;

            // The try block here is to make debug info easier to read
            try
            {
                flowChartTree.AddNode(currentNode);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentException("Nova: A label must have a name");
            }
            catch (ArgumentException)
            {
                throw new DuplicatedDefinitionException(
                    $"Nova: Multiple definition of the same label {currentNode.name}");
            }
        }

        public void BeginAddLocaleForNode(string name)
        {
            currentNode = flowChartTree.GetNode(name);
        }

        /// <summary>
        /// Register a lazy binding link and null the current node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="destination">Destination of the jump</param>
        public void RegisterJump(string destination)
        {
            if (destination == null)
            {
                string msg = "Nova: jump_to instruction must have a destination.";
                msg += " Exception occurs at node: " + currentNode.name;
                throw new ArgumentException(msg);
            }

            if (currentNode.type == FlowChartNodeType.Branching)
            {
                throw new ArgumentException("Nova: Cannot apply jump_to() to a branching node.");
            }

            lazyBindingLinks.Add(new LazyBindingEntry
            {
                from = currentNode,
                destination = destination,
                branchInfo = BranchInformation.Default
            });

            currentNode = null;
        }

        /// <summary>
        /// Add a branch to the current node.
        /// The type of the current node will be switched to Branching.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="name">Internal name of the branch, unique in a node</param>
        /// <param name="destination">Name of the destination node</param>
        /// <param name="text">Text on the button to select this branch</param>
        /// <param name="imageInfo"></param>
        /// <param name="mode"></param>
        /// <param name="condition"></param>
        public void RegisterBranch(string name, string destination, string text, BranchImageInformation imageInfo,
            BranchMode mode, LuaFunction condition)
        {
            if (string.IsNullOrEmpty(destination))
            {
                throw new ArgumentException(
                    $"Nova: A branch must have a destination. Exception occurs at node: {currentNode.name}, text: {text}");
            }

            if (mode == BranchMode.Normal && condition != null)
            {
                throw new ArgumentException(
                    $"Nova: Branch mode is Normal but condition is not null. Exception occurs at node: {currentNode.name}, destination: {destination}");
            }

            if (mode == BranchMode.Jump && (text != null || imageInfo != null))
            {
                throw new ArgumentException(
                    $"Nova: Branch mode is Jump but text or imageInfo is not null. Exception occurs at node: {currentNode.name}, destination: {destination}");
            }

            if ((mode == BranchMode.Show || mode == BranchMode.Enable) && condition == null)
            {
                throw new ArgumentException(
                    $"Nova: Branch mode is Show or Enable but condition is null. Exception occurs at node: {currentNode.name}, destination: {destination}");
            }

            currentNode.type = FlowChartNodeType.Branching;
            lazyBindingLinks.Add(new LazyBindingEntry
            {
                from = currentNode,
                destination = destination,
                branchInfo = new BranchInformation(name, text, imageInfo, mode, condition)
            });
        }

        /// <summary>
        /// Stop registering branches to the current node, and null the current node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        public void EndRegisterBranch()
        {
            currentNode = null;
        }

        /// <summary>
        /// Set the current node as a start node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// A flow chart tree can have multiple start points.
        /// A name can be assigned to a start point, which can differ from the node name.
        /// The name should be unique among all start point names.
        /// </remarks>
        /// <param name="name">
        /// Name of the start point.
        /// If no name is given, the name of the current node will be used.
        /// </param>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if called without registering the current node.
        /// </exception>
        public void SetCurrentAsStart(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException(
                    $"Nova: SetCurrentAsStart({name}) should be called after registering the current node.");
            }

            if (name == null)
            {
                name = currentNode.name;
            }

            flowChartTree.AddStart(name, currentNode);
        }

        public void SetCurrentAsUnlockedStart(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException(
                    $"Nova: SetCurrentAsUnlockedStart({name}) should be called after registering the current node.");
            }

            if (name == null)
            {
                name = currentNode.name;
            }

            SetCurrentAsStart(name);
            flowChartTree.AddUnlockedStart(name, currentNode);
        }

        /// <summary>
        /// Set the current node as the default start node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// This method will first add the current node as a start node, then set it as default.
        /// </remarks>
        /// <param name="name"></param>
        public void SetCurrentAsDefaultStart(string name)
        {
            SetCurrentAsUnlockedStart(name);
            flowChartTree.defaultStartNode = currentNode;
        }

        /// <summary>
        /// Set the current node as an end node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// A flow chart tree can have multiple end points.
        /// A name can be assigned to an end point, which can differ from the node name.
        /// The name should be unique among all end point names.
        /// </remarks>
        /// <param name="name">
        /// Name of the end point.
        /// If no name is given, the name of the current node will be used.
        /// </param>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if called without registering the current node.
        /// </exception>
        public void SetCurrentAsEnd(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException(
                    $"Nova: SetCurrentAsEnd({name}) should be called after registering the current node.");
            }

            // Set the current node type as End
            currentNode.type = FlowChartNodeType.End;

            // Add the node as an end
            if (name == null)
            {
                name = currentNode.name;
            }

            flowChartTree.AddEnd(name, currentNode);

            // Null the current node, because SetCurrentAsEnd() indicates the end of a node
            currentNode = null;
        }

        #endregion
    }
}