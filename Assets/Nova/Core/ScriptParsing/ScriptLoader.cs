using LuaInterface;
using Nova.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// The class that load scripts and construct the flowchart tree.
    /// </summary>
    [ExportCustomType]
    public class ScriptLoader
    {
        // variable indicates whether the script loader is initialized
        private bool isInited = false;

        /// <summary>
        /// Initialize the script loader. This method will load all text asset files in the given folder, parse all the
        /// scripts and generate the flowchart tree of the story.
        /// </summary>
        /// <remarks>
        /// All scripts will be parsed so this method might take some time to finish.
        /// </remarks>
        /// <param name="path">
        /// The folder that stores all the scripts. All text assets in that folder will be loaded and processed, so
        /// there should be no text assets other than script files in the given folder.
        /// </param>
        public void Init(string path)
        {
            if (isInited)
            {
                return;
            }

            ForceInit(path);

            isInited = true;
        }

        private FlowChartTree flowChartTree;

        private FlowChartNode currentNode = null;

        // Current locale of the state machine
        public SystemLanguage stateLocale;

        private List<LazyBindingEntry> lazyBindingLinks = new List<LazyBindingEntry>();

        public void ForceInit(string path)
        {
            flowChartTree = new FlowChartTree();
            currentNode = null;
            stateLocale = I18n.DefaultLocale;
            lazyBindingLinks = new List<LazyBindingEntry>();

            // requires.lua is executed and ScriptDialogueEntryParser.PatternToActionGenerator is filled before calling ParseScript()
            LuaRuntime.Instance.BindObject("scriptLoader", this);

            foreach (var locale in I18n.SupportedLocales)
            {
                stateLocale = locale;

                string localizedPath = path;
                if (locale != I18n.DefaultLocale)
                {
                    localizedPath = I18n.LocalePath + locale + "/" + path;
                }

                var scripts = Resources.LoadAll(localizedPath, typeof(TextAsset)).Cast<TextAsset>().ToArray();
                foreach (var script in scripts)
                {
                    ParseScript(script.text);
                }
            }

            // Bind all lazy binding entries
            BindAllLazyBindingEntries();

            // perform sanity check
            flowChartTree.SanityCheck();

            // Construction finish, freeze the tree status
            flowChartTree.Freeze();
        }

        private void CheckInit()
        {
            Assert.IsTrue(isInited, "Nova: ScriptLoader methods should be called after Init().");
        }

        /// <summary>
        /// Get the flow chart tree
        /// </summary>
        /// <remarks>This method should be called after init</remarks>
        /// <returns>The constructed flow chart tree</returns>
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
        /// <param name="text">text of a script</param>
        private void ParseScript(string text)
        {
            LuaRuntime.Instance.DoString("action_new_file()");

            text = text.Trim();

            // Detect the eager execution block
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

            // For some reason, a script file should ends with a eager execution block, which needs to refer
            // to the next flow chart node
            // Every thing after the last eager execution block will be ignored
            if (lastMatchEndIndex < text.Length)
            {
                Debug.LogWarning("Nova: A script file should ends with a eager execution block, " +
                                 "which needs to refer to the next flow chart node.");
            }
        }

        private class LazyBindingEntry
        {
            public FlowChartNode from;
            public BranchInformation branchInfo;
            public string destination;
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

            string[] dialogueEntryTexts = Regex.Split(flowChartNodeText, EmptyLinePattern);

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

            // remove unnecessary reference
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
        /// This method is designed to be called externally by scripts.
        /// A new flow chart node will be created and registered to the current constructing FlowChartTree.
        /// If current editing node is a normal node, the newly created one is intended to be its
        /// succeed node. The link between the new node and the current one will be added immediately, which
        /// won't be registered as a lazy binding link.
        /// </summary>
        /// <param name="name">the name of the new node</param>
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
        /// Register a lazy binding link and null the current node
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="destination">the destination of jump</param>
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
                branchInfo = BranchInformation.Default,
                destination = destination
            });

            currentNode = null;
        }

        /// <summary>
        /// Add a branch to the current node.
        /// The type of the current node will be switched to Branching.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="name">the name of this branch</param>
        /// <param name="destination">the destination of this branch</param>
        /// <param name="metadata">additional metadata</param>
        public void RegisterBranch(string name, string destination, LuaTable metadata)
        {
            if (destination == null)
            {
                string msg =
                    $"Nova: a branch must have a destination. (name = {name}) Exception occurs at node: {currentNode.name}";
                throw new ArgumentException(msg);
            }

            currentNode.type = FlowChartNodeType.Branching;
            lazyBindingLinks.Add(new LazyBindingEntry
            {
                from = currentNode,
                destination = destination,
                branchInfo = new BranchInformation(name, metadata)
            });
        }

        /// <summary>
        /// Stop registering branch to the current node
        /// This method is designed to be called externally by scripts.
        /// Simply null the current node
        /// </summary>
        public void EndRegisterBranch()
        {
            currentNode = null;
        }

        /// <summary>
        /// Set the current node as the start up node
        /// This method is designed to be called externally by scripts
        /// </summary>
        /// <remarks>
        /// A flow chart tree can have multiple entry points. A name can be assigned to a start point,
        /// if no name is given, the name of the current node will be used
        /// </remarks>
        /// <param name="name">
        /// The name of the start point, which can be differ from that of the node.
        /// If no name is given, use the name of the current node as the start point name.
        /// </param>
        public void SetCurrentAsStartUpNode(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException("Nova: is_start() should be called after the definition of a label.");
            }

            if (name == null)
            {
                name = currentNode.name;
            }

            flowChartTree.AddStartUp(name, currentNode);
        }

        /// <summary>
        /// Make the current node as the default start point of the game.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// This method will first add the current node as a start point, then set it as default.
        /// </remarks>
        /// <param name="name"></param>
        public void SetCurrentAsDefaultStart(string name)
        {
            // add a start up point
            SetCurrentAsStartUpNode(name);

            // Make current node as the default start point
            flowChartTree.defaultStartUpNode = currentNode;
        }

        /// <summary>
        /// Set the current node as an end node
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// While a flow chart can have multiple endings, each name of endings should be unique among other endings,
        /// and a node can only have one end name.
        /// </remarks>
        /// <param name="name">The name of the ending</param>
        /// <exception cref="ArgumentException">
        /// ArgumentException will been thrown if is_end is called when the label is not defined.
        /// </exception>
        public void SetCurrentAsEnd(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException(
                    $"Nova: is_end({name}) should be called after the definition of a label.");
            }

            // Set the current node type as end
            currentNode.type = FlowChartNodeType.End;

            // Add the node as an end
            if (name == null)
            {
                name = currentNode.name;
            }

            flowChartTree.AddEnd(name, currentNode);

            // null the current node, is_end will indicates the end of a label
            currentNode = null;
        }

        #endregion
    }
}