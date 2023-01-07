using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ChapterSelectViewController : ViewControllerBase
    {
        [SerializeField] private GameObject chapterButtonPrefab;
        [SerializeField] private Transform chapterList;
        [SerializeField] private Button returnButton;
        [SerializeField] private bool unlockAllNodes;
        [SerializeField] private bool unlockDebugNodes;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private NameSorter nameSorter;

        private IReadOnlyList<string> nodes;
        private IReadOnlyCollection<string> activeNodes;
        private IReadOnlyCollection<string> unlockedNodes;
        private IReadOnlyList<GameObject> buttons;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            nameSorter = GetComponent<NameSorter>();

            var _nodes = gameState.GetStartNodeNames(StartNodeType.All);
            if (nameSorter)
            {
                _nodes = nameSorter.Sort(_nodes);
            }

            nodes = _nodes.ToList();
            buttons = nodes.Select(InitButton).ToList();

            returnButton.onClick.AddListener(Hide);
            I18n.LocaleChanged.AddListener(UpdateButtons);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            I18n.LocaleChanged.RemoveListener(UpdateButtons);
        }

        public override void Show(Action onFinish)
        {
            UpdateNodes();
            if (unlockedNodes.Count == 1 && !inputManager.IsPressed(AbstractKey.EditorUnlock))
            {
                GameStart(unlockedNodes.First());
                return;
            }

            UpdateButtons();
            base.Show(onFinish);
        }

        public void UpdateNodes()
        {
            activeNodes = new HashSet<string>(
                gameState.GetStartNodeNames(unlockDebugNodes ? StartNodeType.All : StartNodeType.Normal));
            var unlockedAtFirst = new HashSet<string>(
                gameState.GetStartNodeNames(unlockAllNodes ? StartNodeType.All : StartNodeType.Unlocked));
            unlockedNodes = new HashSet<string>(activeNodes.Where(
                node => unlockedAtFirst.Contains(node) || checkpointManager.IsReachedAnyHistory(node, 0)));
        }

        public IEnumerable<string> GetUnlockedNodes()
        {
            return unlockedNodes;
        }

        public void GameStart(string nodeName)
        {
            viewManager.GetController<TitleController>().SwitchView<DialogueBoxController>(() =>
            {
                gameState.GameStart(nodeName);
            });
        }

        private GameObject InitButton(string nodeName)
        {
            var go = Instantiate(chapterButtonPrefab, chapterList);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => Hide(() => GameStart(nodeName)));
            return go;
        }

        private void UpdateButtons()
        {
            if (activeNodes == null)
            {
                return;
            }

            for (var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                var go = buttons[i];
                if (activeNodes.Contains(node))
                {
                    go.SetActive(true);
                    var text = go.GetComponent<Text>();
                    var button = go.GetComponent<Button>();
                    if (unlockedNodes.Contains(node))
                    {
                        text.text = I18n.__(gameState.GetNode(node, false).displayNames);
                        button.enabled = true;
                    }
                    else
                    {
                        text.text = I18n.__("title.selectchapter.locked");
                        button.enabled = false;
                    }
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        public void UnlockNodes(bool normal, bool debug)
        {
            unlockAllNodes |= normal;
            unlockDebugNodes |= debug;
            UpdateNodes();
            UpdateButtons();
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (inputManager.IsTriggered(AbstractKey.EditorUnlock))
            {
                UnlockNodes(true, true);
            }
        }
    }
}
