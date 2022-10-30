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
        [SerializeField] private bool unlockAllChaptersForDebug;

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
            I18n.LocaleChanged.AddListener(UpdateButtons);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            I18n.LocaleChanged.RemoveListener(UpdateButtons);
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
            if (ReachedChapterCount() < 2 && !inputManager.IsPressed(AbstractKey.EditorUnlock))
            {
                BeginChapter();
                return;
            }

            UpdateButtons();

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

            if (inputManager.IsTriggered(AbstractKey.EditorUnlock))
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
