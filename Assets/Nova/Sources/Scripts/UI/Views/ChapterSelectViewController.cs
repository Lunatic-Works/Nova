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
        [SerializeField] private bool unlockAllChapters;
        [SerializeField] private bool unlockDebugChapters;

        private GameState gameState;
        private CheckpointManager checkpointManager;
        private LogController logController;
        private NameSorter nameSorter;

        private IReadOnlyList<string> chapters;
        private HashSet<string> unlockedChapters;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            logController = viewManager.GetController<LogController>();
            nameSorter = GetComponent<NameSorter>();

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
        }

        public override void Show(Action onFinish)
        {
            UpdateChapters();
            if (GetUnlockedChapters().Count() < 2 && !inputManager.IsPressed(AbstractKey.EditorUnlock))
            {
                BeginChapter();
                return;
            }

            UpdateButtons();
            base.Show(onFinish);
        }

        public void UpdateChapters()
        {
            chapters = gameState.GetStartNodeNames(unlockDebugChapters ? StartNodeType.All : StartNodeType.Normal)
                .ToList();
            if (nameSorter)
            {
                chapters = nameSorter.Sort(chapters).ToList();
            }

            var unlockedAtFirst = new HashSet<string>(
                gameState.GetStartNodeNames(unlockAllChapters ? StartNodeType.UnlockedAll : StartNodeType.Unlocked)
            );
            unlockedChapters = new HashSet<string>(
                chapters.Where(name => unlockedAtFirst.Contains(name) || checkpointManager.IsReachedAnyHistory(name, 0))
            );
        }

        public IEnumerable<string> GetUnlockedChapters()
        {
            return unlockedChapters;
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

        private void InitButton(string chapter, bool unlocked)
        {
            var go = Instantiate(chapterButtonPrefab, chapterList);
            var text = go.GetComponent<Text>();
            var button = go.GetComponent<Button>();
            if (unlocked)
            {
                text.text = I18n.__(gameState.GetNode(chapter).displayNames);
                button.enabled = true;
                button.onClick.AddListener(() => Hide(() => BeginChapter(chapter)));
            }
            else
            {
                text.text = I18n.__("title.selectchapter.locked");
                button.enabled = false;
            }
        }

        private void ClearButtons()
        {
            foreach (var child in Utils.GetChildren(chapterList))
            {
                Destroy(child.gameObject);
            }
        }

        private void UpdateButtons()
        {
            ClearButtons();
            foreach (var chapter in chapters)
            {
                InitButton(chapter, unlockedChapters.Contains(name));
            }
        }

        public void UnlockChapters(bool normal, bool debug)
        {
            unlockAllChapters |= normal;
            unlockDebugChapters |= debug;
            UpdateChapters();
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
