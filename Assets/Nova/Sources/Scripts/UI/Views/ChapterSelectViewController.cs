using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ChapterSelectViewController : ViewControllerBase
    {
        public GameObject chapterButtonPrefab;
        public GameObject chapterList;
        public Button returnButton;
        public bool unlockAllChaptersForDebug;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private LogController logController;
        private NameSorter nameSorter;

        private IReadOnlyList<string> startNodeNames;
        private IReadOnlyList<string> unlockedStartNodeNames;

        private Dictionary<string, GameObject> buttons;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            logController = viewManager.GetController<LogController>();
            nameSorter = GetComponent<NameSorter>();

            startNodeNames = gameState.GetAllStartNodeNames();
            if (nameSorter && nameSorter.matchers.Count > 0)
            {
                startNodeNames = nameSorter.Sort(startNodeNames).ToList();
            }

            unlockedStartNodeNames = gameState.GetAllUnlockedStartNodeNames();

            returnButton.onClick.AddListener(Hide);
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();

            buttons = startNodeNames.Select(chapter =>
            {
                var go = Instantiate(chapterButtonPrefab, chapterList.transform);
                var button = go.GetComponent<Button>();
                button.onClick.AddListener(() => Hide(() => BeginChapter(chapter)));
                return new KeyValuePair<string, GameObject>(chapter, go);
            }).ToDictionary(p => p.Key, p => p.Value);
        }

        public override void Show(Action onFinish)
        {
            if (ReachedChapterCount() < 2 && !Utils.GetKeyInEditor(KeyCode.LeftShift))
            {
                BeginChapter();
                return;
            }

            UpdateAllButtons();

            base.Show(onFinish);
        }

        private bool IsUnlocked(string name)
        {
            return unlockAllChaptersForDebug || unlockedStartNodeNames.Contains(name) ||
                   checkpointManager.IsReachedAnyHistory(name, 0);
        }

        private int ReachedChapterCount()
        {
            return startNodeNames.Count(IsUnlocked);
        }

        private void BeginChapter(string chapterName = null)
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

        private void UpdateAllButtons()
        {
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

        protected override void OnActivatedUpdate()
        {
            base.OnActivatedUpdate();

            if (Utils.GetKeyDownInEditor(KeyCode.LeftShift))
            {
                foreach (var chapter in buttons)
                {
                    chapter.Value.GetComponent<Button>().enabled = true;
                    chapter.Value.GetComponent<Text>().text = I18n.__(gameState.GetNode(chapter.Key).displayNames);
                }
            }
        }
    }
}