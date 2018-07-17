using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LuaInterface;
using Nova.Exceptions;
using UnityEngine;
using UnityEngine.Assertions;

namespace Nova
{
    /// <summary>
    /// The class that load scripts and construct the flowchart tree.
    /// </summary>
    public class ScriptLoader
    {
        // variable indicates whether the script loader is inited
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

            LuaRuntime.Instance.BindObject("scriptLoader", this);
            var scripts = Resources.LoadAll(path, typeof(TextAsset)).Cast<TextAsset>().ToArray();
            foreach (var script in scripts)
            {
                ParseScript(script.text);
            }

            // Bind all lazy binding entries
            BindAllLazyBindingEntries();

            isInited = true;
        }

        private void CheckInit()
        {
            Assert.IsTrue(isInited, "Nova: ScriptLoader methods should be called after Init");
        }

        /// <summary>
        /// Get the flow chart tree
        /// </summary>
        /// <remarks>This method should be called after Init</remarks>
        /// <returns>The constructed flow chart tree</returns>
        public FlowChartTree GetFlowChartTree()
        {
            CheckInit();
            return flowChartTree;
        }

        private const string FastExecutionStartSymbol = "@<|";
        private const string FastExecutionEndSymbol = "|>";
        private const string LazyExcutionStartSymbol = "<|";
        private const string LazyExcutionEndSymbol = "|>";

        private const string FastExecutionBlockPattern = @"@<\|((?:.|[\r\n])*?)\|>";
        private const string LazyExecutionBlockPattern = @"^<\|((?:.|[\r\n])*?)\|>";

        /// <summary>
        /// Parse the given script text
        /// </summary>
        /// <param name="text">text of a script</param>
        private void ParseScript(string text)
        {
            text = text.Trim();
            // detect the fast execution chunk
            var fastExecutionStartIndex = text.IndexOf(FastExecutionStartSymbol, StringComparison.Ordinal);
            if (fastExecutionStartIndex != 0)
            {
                // The script file does not start with a fast execution chunck
                Debug.Log("<color=red><b>WARN</b>: The script file does not start with a fast execution block. " +
                          "All text before the first execution block will be removed</color>");
            }

            // No fast execution block is found, simply ignore this file
            if (fastExecutionStartIndex < 0)
            {
                return;
            }

            text = text.Substring(fastExecutionStartIndex);
            var lastMatchEndIndex = 0;
            foreach (Match m in Regex.Matches(text, FastExecutionBlockPattern))
            {
                var flowChartNodeText = text.Substring(lastMatchEndIndex, m.Index - lastMatchEndIndex);
                // This method will not be excuted when the execution enter this loop for the first time,
                // since the first fast execution block is definitely at the begining of the text.
                ParseFlowChartNodeText(flowChartNodeText);
                lastMatchEndIndex = m.Index + m.Length;

                var fastExecutionBlockCode = m.Groups[1].Value;
                DoFastExecutionBlock(fastExecutionBlockCode);
            }

            // For some reason, a script file should ends with a fast execution chuck, which needs to refer
            // to the next flow chart node
            // Every thing after the last fast execution chunk will be ignored

            if (lastMatchEndIndex < text.Length)
            {
                Debug.Log("<color=red><b>WARN</b>: A script file should ends with a fast execution chuck, " +
                          "which needs to refer to the next flow chart node</color>");
            }
        }

        private class LazyBindingEntry
        {
            public FlowChartNode from;
            public BranchInformation branchInfo;
            public string destination;
        }

        private FlowChartNode currentNode = null;

        private List<LazyBindingEntry> lazyBindingLinks = new List<LazyBindingEntry>();

        private readonly FlowChartTree flowChartTree = new FlowChartTree();


        /// <summary>
        /// Parse the flow chart node
        /// </summary>
        /// <remarks>
        /// The name of this method might be a little misleading, since this method actually parses the text
        /// splitted by fast execution blocks, while the node structure are defined by scripts in the fast execution
        /// block. A new node is created when the 'label' instruction is invoked in the fast execution block, and its
        /// content ends when either 'branch' or 'jump' instruction is called. Current implementaion (2018/07/16)
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

            var emptyLinePattern = @"(?:(?:\r\n|\n)\s*){2,}";
            var dialogueEntryTexts = Regex.Split(flowChartNodeText, emptyLinePattern);
            foreach (var text in dialogueEntryTexts)
            {
                Debug.Log("<color=green>" + text + "</color>");
                var dialogueEntry = ParseDialogueEntry(text);
                currentNode.dialogueEntries.Add(dialogueEntry);
            }
        }

        /// <summary>
        /// Parse a dialogue entry text
        /// </summary>
        /// <remarks>
        /// A dialogue entry can have one or none lazy execution block. The lazy execution block (if have) should be
        /// placed at the begining of the dialogue entry text.
        /// </remarks>
        /// <param name="dialogueEntryText"></param>
        /// <returns></returns>
        private DialogueEntry ParseDialogueEntry(string dialogueEntryText)
        {
            dialogueEntryText = dialogueEntryText.Trim();
            var textStartIndex = 0;
            var dialogueEntry = new DialogueEntry();
            var lazyExecutionBlockMatch = Regex.Match(dialogueEntryText, LazyExecutionBlockPattern);
            if (lazyExecutionBlockMatch.Success)
            {
                var code = lazyExecutionBlockMatch.Groups[1].Value;
                dialogueEntry.action = LuaRuntime.Instance.WrapClosure(code);
                textStartIndex += lazyExecutionBlockMatch.Length;
            }

            dialogueEntry.text = dialogueEntryText.Substring(textStartIndex);
            return dialogueEntry;
        }

        /// <summary>
        /// Bind all lazy binding entries
        /// </summary>
        private void BindAllLazyBindingEntries()
        {
            foreach (var entry in lazyBindingLinks)
            {
                var node = entry.from;
                node.branches.Add(entry.branchInfo, flowChartTree.FindNode(entry.destination));
            }

            // remove unnecessary reference
            lazyBindingLinks = null;
        }

        /// <summary>
        /// Execute code in the fast execution block
        /// </summary>
        /// <param name="fastExecutionBlockCode"></param>
        private void DoFastExecutionBlock(string fastExecutionBlockCode)
        {
            LuaRuntime.Instance.DoString(fastExecutionBlockCode);
        }

        // ----------------------- Below are methods called by external scripts ---------------------------- //

        /// <summary>
        /// This method is designed to be called externally by scripts.
        /// A new flow chart node will be created and registered to the current constructing FlowChartTree.
        /// If current editing node is a normal node, the newly created one is intended to be its
        /// succeed node. The link between the new node and the current one will be added immediately, which
        /// won't be registered as a lazy binding link.
        /// </summary>
        /// <param name="name">the name of the new node</param>
        /// <param name="description">the description of the new node</param>
        public void RegisterNewNode(string name, string description)
        {
            var nextNode = new FlowChartNode {name = name, description = description};
            if (currentNode != null && currentNode.type == FlowChartNodeType.Normal)
            {
                currentNode.branches.Add(BranchInformation.Defualt, nextNode);
            }

            currentNode = nextNode;
            // The try block here is to make debug info easier to read
            try
            {
                flowChartTree.AddNode(currentNode);
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentException("Nova: A label must have a name");
            }
            catch (ArgumentException ex)
            {
                throw new DuplicatedDefinitionException(
                    string.Format("Nova: Multiple definition of the same label {0}", currentNode.name));
            }
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
                var msg = "Nova: jump_to instruction must have a destination.";
                msg += " Exception occurs at chunk: " + currentNode.name;
                throw new ArgumentException(msg);
            }

            if (currentNode.type == FlowChartNodeType.Branching)
            {
                throw new ArgumentException("Nova: Can not apply 'jump_to' to a Branching node");
            }

            lazyBindingLinks.Add(new LazyBindingEntry
            {
                from = currentNode,
                branchInfo = BranchInformation.Defualt,
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
                var msg = "Nova: a branch must have a destination.";
                msg += " Exception occurs at chunk: " + currentNode.name;
                throw new ArgumentException(msg);
            }

            currentNode.type = FlowChartNodeType.Branching;
            lazyBindingLinks.Add(new LazyBindingEntry
            {
                from = currentNode,
                destination = destination,
                branchInfo = new BranchInformation
                {
                    name = name,
                    metadata = metadata
                }
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
        public void SetCurrentAsStarUpNode(string name)
        {
            if (currentNode == null)
            {
                throw new ArgumentException("Nova: is_start should be called after the definition of a label");
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
            SetCurrentAsStarUpNode(name);

            // Make current node as the default start point
            flowChartTree.DefaultStartUpNode = currentNode;
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
                    string.Format("Nova: is_end({0}) should be called after the definition of a label", name));
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
    }
}