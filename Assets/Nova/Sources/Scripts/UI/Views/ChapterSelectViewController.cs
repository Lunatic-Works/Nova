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
        [SerializeField] private GameObject chapterList;
        [SerializeField] private Button returnButton;
        [SerializeField] private bool unlockAllChapters;
        [SerializeField] private bool unlockDebugChapters;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private LogController logController;
        private NameSorter nameSorter;

        private IReadOnlyList<string> startNodeNames;
        private HashSet<string> unlockedStartNodeNames;
        private HashSet<string> debugNodeNames;

        private Dictionary<string, GameObject> buttons;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            logController = viewManager.GetController<LogController>();
            nameSorter = GetComponent<NameSorter>();

            var startNodes = gameState.GetStartNodeNames();
            if (nameSorter && nameSorter.matchers.Count > 0)
            {
                startNodes = nameSorter.Sort(startNodes);
            }
            startNodeNames = startNodes.ToList();

            unlockedStartNodeNames = new HashSet<string>(gameState.GetUnlockedStartNodeNames());
            debugNodeNames = new HashSet<string>(gameState.GetDebugNodeNames());

            returnButton.onClick.AddListener(Hide);
            I18n.LocaleChanged.AddListener(UpdateButtons);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            I18n.LocaleChanged.RemoveListener(UpdateButtons);
        }

        private GameObject InitButton(string chapter)
        {
            var go = Instantiate(chapterButtonPrefab, chapterList.transform);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => Hide(() => BeginChapter(chapter)));
            return go;
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();

            var allNodes = unlockDebugChapters ? startNodeNames.Concat(debugNodeNames) : startNodeNames;
            buttons = allNodes.ToDictionary(chapter => chapter, InitButton);
        }

        public override void Show(Action onFinish)
        {
            if (UnlockedChapterCount() < 2 && !inputManager.IsPressed(AbstractKey.EditorUnlock))
            {
                BeginChapter();
                return;
            }

            UpdateButtons();

            base.Show(onFinish);
        }

        private bool IsUnlocked(string name)
        {
            return unlockAllChapters || unlockedStartNodeNames.Contains(name) ||
                   debugNodeNames.Contains(name) || checkpointManager.IsReachedAnyHistory(name, 0);
        }

        public int UnlockedChapterCount()
        {
            var cnt = startNodeNames.Count(IsUnlocked);
            if (unlockDebugChapters)
            {
                cnt += debugNodeNames.Count;
            }
            return cnt;
        }

        public IEnumerable<string> GetUnlockedChapters()
        {
            var ret = startNodeNames.Where(IsUnlocked);
            return unlockDebugChapters ? ret.Concat(debugNodeNames) : ret;
        }

        public void BeginChapter(string chapterName = null)
        {
            viewManager.GetController<TitleController>().SwitchView<DialogueBoxController>(() =>
            {
                logController.Clear();

                if (chapterName == null)
                {
                    gameState.GameStart();
                }
                else
                {
                    gameState.GameStart(chapterName);
                }
            });
        }

        private void UpdateButtons()
        {
            if (buttons == null)
            {
                return;
            }

            if (unlockDebugChapters && buttons.Count < startNodeNames.Count + debugNodeNames.Count)
            {
                foreach (var chapter in debugNodeNames)
                {
                    buttons.Add(chapter, InitButton(chapter));
                }
            }

            foreach (var chapter in buttons)
            {
                if (IsUnlocked(chapter.Key))
                {
                    chapter.Value.GetComponent<Button>().enabled = true;
                    chapter.Value.GetComponent<Text>().text = I18n.__(gameState.GetNode(chapter.Key).displayNames);
                }
                else
                {
                    chapter.Value.GetComponent<Button>().enabled = false;
                    chapter.Value.GetComponent<Text>().text = I18n.__("title.selectchapter.locked");
                }
            }
        }

        public void UnlockChapters(bool normal, bool debug)
        {
            unlockAllChapters |= normal;
            unlockDebugChapters |= debug;
            UpdateButtons();
        }

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (inputManager.IsTriggered(AbstractKey.EditorUnlock))
            {
                UnlockChapters(true, true);
            }
        }
    }
}
