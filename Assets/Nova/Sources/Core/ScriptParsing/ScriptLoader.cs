using LuaInterface;
using Nova.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// The class that loads scripts and constructs the flow chart graph.
    /// </summary>
    [ExportCustomType]
    public class ScriptLoader
    {
        private bool inited;

        /// <summary>
        /// Initialize the script loader. This method will load all text asset files in the given folder, parse all the
        /// scripts, and construct the flow chart graph.
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

        private readonly FlowChartGraph flowChartGraph = new FlowChartGraph();

        private FlowChartNode currentNode;

        // Current locale of the state machine
        public SystemLanguage stateLocale;

        private readonly Dictionary<string, string> hiddenCharacterNames = new Dictionary<string, string>();

        private class LazyBindingEntry
        {
            public readonly FlowChartNode from;
            public readonly string destination;
            public readonly BranchInformation branchInfo;

            public LazyBindingEntry(FlowChartNode from, string destination, BranchInformation branchInfo)
            {
                this.from = from;
                this.destination = destination;
                this.branchInfo = branchInfo;
            }
        }

        private readonly List<LazyBindingEntry> lazyBindingLinks = new List<LazyBindingEntry>();

        private readonly HashSet<string> onlyIncludedNames = new HashSet<string>();

        private void InitOnlyIncludedNames()
        {
            var table = LuaRuntime.Instance.GetTable("only_included_scenario_names");
            onlyIncludedNames.Clear();
            onlyIncludedNames.UnionWith(table.ToArray().Cast<string>());
            table.Dispose();
        }

        public void ForceInit(string path)
        {
            currentNode = null;
            stateLocale = I18n.DefaultLocale;
            lazyBindingLinks.Clear();

            ScriptDialogueEntryParser.ClearPatterns();
            // requires.lua is executed and ScriptDialogueEntryParser.ActionGenerators is filled before calling ParseScript()
            LuaRuntime.Instance.BindObject("scriptLoader", this);
            LuaRuntime.Instance.UpdateExecutionContext(new ExecutionContext(ExecutionMode.Eager,
                DialogueActionStage.Default, false));
            InitOnlyIncludedNames();

            flowChartGraph.Unfreeze();

            foreach (var locale in I18n.SupportedLocales)
            {
                stateLocale = locale;

                string localizedPath = path;
                if (locale != I18n.DefaultLocale)
                {
                    localizedPath = I18n.LocalizedResourcesPath + locale + "/" + path;
                }

                var scripts = Resources.LoadAll(localizedPath, typeof(TextAsset)).Cast<TextAsset>();
                foreach (var script in scripts)
                {
                    if (onlyIncludedNames.Count > 0 && !onlyIncludedNames.Contains(script.name))
                    {
                        continue;
                    }

#if UNITY_EDITOR
                    // var scriptPath = AssetDatabase.GetAssetPath(script);
                    // Debug.Log($"Nova: Parse script {scriptPath}");
#endif

                    try
                    {
                        // If deferChunks == true, only eager execution blocks are parsed and executed when the game starts
                        // If deferChunks == false, dialogues and lazy execution blocks are also parsed
                        ParseScript(script, true);
                    }
                    catch (ParserException e)
                    {
                        throw new ParserException($"Failed to parse {script.name}", e);
                    }
                }
            }

            // Bind all lazy binding entries
            BindAllLazyBindingEntries();

            // Perform sanity check
            flowChartGraph.SanityCheck();

            // Construction finished, freeze the graph status
            flowChartGraph.Freeze();
        }

        private void CheckInit()
        {
            Utils.RuntimeAssert(inited, "ScriptLoader methods should be called after Init.");
        }

        /// <summary>
        /// Get the flow chart graph.
        /// </summary>
        /// <remarks>This method should be called after init</remarks>
        /// <returns>The flow chart graph</returns>
        public FlowChartGraph GetFlowChartGraph()
        {
            CheckInit();
            return flowChartGraph;
        }

        public class Chunk
        {
            public readonly List<ParsedBlock> blocks = new List<ParsedBlock>();

            public ulong GetHashUlong()
            {
                return Utils.HashList(blocks.SelectMany(x =>
                {
                    IEnumerable<object> ret = new object[] {x.type, x.content};
                    if (x.attributes != null)
                    {
                        ret = ret.Concat(x.attributes.Cast<object>());
                    }

                    return ret;
                }));
            }
        }

        /// <summary>
        /// Split blocks at separator and eager execution blocks. All chunks in the result contain
        /// at least one ParsedBlock.
        /// </summary>
        private static IReadOnlyList<Chunk> DivideBlocksToChunks(IReadOnlyList<ParsedBlock> blocks)
        {
            var res = new List<Chunk>();
            var chunk = new Chunk();

            void FlushChunk()
            {
                if (chunk.blocks.Count > 0)
                {
                    res.Add(chunk);
                    chunk = new Chunk();
                }
            }

            foreach (var block in blocks)
            {
                if (block.type == BlockType.Separator)
                {
                    FlushChunk();
                }
                else if (block.type == BlockType.EagerExecution)
                {
                    FlushChunk();
                    var item = new Chunk();
                    item.blocks.Add(block);
                    res.Add(item);
                }
                else
                {
                    chunk.blocks.Add(block);
                }
            }

            FlushChunk();

            return res;
        }

        private static ulong GetNodeHash(IReadOnlyList<Chunk> nodeChunks)
        {
            return Utils.HashList(nodeChunks.Select(x => x.GetHashUlong()));
        }

        /// <summary>
        /// Parse the given TextAsset to chunks and add them to currentNode.
        /// </summary>
        private void ParseScript(TextAsset script, bool deferChunks = false)
        {
            hiddenCharacterNames.Clear();
            LuaRuntime.Instance.GetFunction("action_new_file").Call(script.name);

            var blocks = Parser.Parse(script.text).blocks;
            if (blocks.Count == 0)
            {
                return;
            }

            var chunks = DivideBlocksToChunks(blocks);
            var nodeChunks = new List<Chunk>();
            foreach (var chunk in chunks)
            {
                var firstBlock = chunk.blocks[0];
                if (firstBlock.type == BlockType.EagerExecution)
                {
                    if (nodeChunks.Count > 0)
                    {
                        if (stateLocale == I18n.DefaultLocale)
                        {
                            currentNode.textHash = GetNodeHash(nodeChunks);
                        }

                        if (deferChunks)
                        {
                            currentNode.deferredChunks[stateLocale] = nodeChunks;
                        }
                        else
                        {
                            AddDialogueChunks(nodeChunks);
                        }

                        nodeChunks = new List<Chunk>();
                    }

                    DoEagerExecutionBlock(firstBlock.content);
                }
                else
                {
                    nodeChunks.Add(chunk);
                }
            }
        }

        private void AddDialogueChunks(IReadOnlyList<Chunk> chunks)
        {
            if (currentNode == null)
            {
                throw new ArgumentException("Nova: Dangling node text.");
            }

            if (stateLocale == I18n.DefaultLocale)
            {
                var entries = ScriptDialogueEntryParser.ParseDialogueEntries(chunks, hiddenCharacterNames);
                currentNode.SetDialogueEntries(entries);
            }
            else
            {
                var entries = ScriptDialogueEntryParser.ParseLocalizedDialogueEntries(chunks);
                currentNode.AddLocalizedDialogueEntries(stateLocale, entries);
            }
        }

        public void AddDeferredDialogueChunks(FlowChartNode node)
        {
            if (node.deferredChunks.Count == 0)
            {
                return;
            }

            node.Unfreeze();

            foreach (var locale in node.deferredChunks.Keys)
            {
                var chunks = node.deferredChunks[locale];
                if (locale == I18n.DefaultLocale)
                {
                    var entries = ScriptDialogueEntryParser.ParseDialogueEntries(chunks, hiddenCharacterNames);
                    node.SetDialogueEntries(entries);
                }
                else
                {
                    var entries = ScriptDialogueEntryParser.ParseLocalizedDialogueEntries(chunks);
                    node.AddLocalizedDialogueEntries(locale, entries);
                }
            }

            node.deferredChunks.Clear();
            node.Freeze();
        }

        /// <summary>
        /// Bind all lazy binding entries
        /// </summary>
        private void BindAllLazyBindingEntries()
        {
            foreach (var entry in lazyBindingLinks)
            {
                var node = entry.from;
                node.AddBranch(entry.branchInfo, flowChartGraph.GetNode(entry.destination));
            }

            // Remove unnecessary reference
            lazyBindingLinks.Clear();
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
        /// Create a new flow chart node register it to the current constructing FlowChartGraph.
        /// If the current node is a normal node, the newly created one is intended to be its
        /// succeeding node. The link between the new node and the current one will be added immediately, which
        /// will not be registered as a lazy binding link.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <param name="name">Internal name of the new node</param>
        /// <param name="displayName">Displayed name of the new node</param>
        public void RegisterNewNode(string name, string displayName)
        {
            var nextNode = new FlowChartNode(name);
            if (currentNode != null && currentNode.type == FlowChartNodeType.Normal)
            {
                currentNode.AddBranch(BranchInformation.Default, nextNode);
            }

            currentNode = nextNode;

            flowChartGraph.AddNode(currentNode);

            currentNode.AddLocalizedName(stateLocale, displayName);
        }

        public void AddLocalizedNode(string name, string displayName)
        {
            currentNode = flowChartGraph.GetNode(name);
            if (currentNode == null)
            {
                throw new ArgumentException(
                    $"Nova: Node {name} found in {stateLocale} but not in {I18n.DefaultLocale}. " +
                    "Maybe you need to delete the default English scenarios.");
            }

            currentNode.AddLocalizedName(stateLocale, displayName);
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
                throw new ArgumentException(
                    $"Nova: jump_to instruction must have a destination. Exception occurs at node: {currentNode.name}");
            }

            if (currentNode.type == FlowChartNodeType.Branching)
            {
                throw new ArgumentException("Nova: Cannot apply jump_to() to a branching node.");
            }

            lazyBindingLinks.Add(new LazyBindingEntry(currentNode, destination, BranchInformation.Default));

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
            lazyBindingLinks.Add(new LazyBindingEntry(currentNode, destination,
                new BranchInformation(name, text, imageInfo, mode, condition)));
        }

        public void AddLocalizedBranch(string name, string destination, string text)
        {
            var branchInfo = lazyBindingLinks.Find(x =>
                    x.from.name == currentNode.name && x.destination == destination && x.branchInfo.name == name)
                ?.branchInfo;
            if (branchInfo == null)
            {
                throw new ArgumentException(
                    $"Nova: branchInfo not found. from: {currentNode.name}, destination: {destination}, branchInfo: {name}");
            }

            branchInfo.AddLocalizedText(stateLocale, text);
        }

        /// <summary>
        /// Stop registering branches to the current node, and null the current node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        public void EndRegisterBranch()
        {
            currentNode = null;
        }

        private void CheckNode()
        {
            if (currentNode == null)
            {
                throw new ArgumentException("Nova: This function should be called after registering the current node.");
            }
        }

        /// <summary>
        /// Set the current node as a start node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// A flow chart graph can have multiple start points.
        /// A name can be assigned to a start point, which can differ from the node name.
        /// The name should be unique among all start point names.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// ArgumentException will be thrown if called without registering the current node.
        /// </exception>
        public void SetCurrentAsStart()
        {
            CheckNode();
            flowChartGraph.AddStart(currentNode, StartNodeType.Locked);
        }

        public void SetCurrentAsUnlockedStart()
        {
            CheckNode();
            flowChartGraph.AddStart(currentNode, StartNodeType.Unlocked);
        }

        public void SetCurrentAsDebug()
        {
            CheckNode();
            flowChartGraph.AddStart(currentNode, StartNodeType.Debug);
        }

        /// <summary>
        /// Set the current node as an end node.
        /// This method is designed to be called externally by scripts.
        /// </summary>
        /// <remarks>
        /// A flow chart graph can have multiple end points.
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

            flowChartGraph.AddEnd(name, currentNode);

            // Null the current node, because SetCurrentAsEnd() indicates the end of a node
            currentNode = null;
        }

        #endregion
    }
}
