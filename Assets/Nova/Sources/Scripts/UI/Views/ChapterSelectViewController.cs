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
        private NameSorter nameSorter;

        private IReadOnlyList<string> chapters;
        private IReadOnlyCollection<string> activeChapters;
        private IReadOnlyCollection<string> unlockedChapters;
        private IReadOnlyList<GameObject> buttons;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            nameSorter = GetComponent<NameSorter>();

            var _chapters = gameState.GetStartNodeNames(StartNodeType.All);
            if (nameSorter)
            {
                _chapters = nameSorter.Sort(_chapters);
            }

            chapters = _chapters.ToList();
            buttons = chapters.Select(InitButton).ToList();

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
            UpdateChapters();
            if (unlockedChapters.Count == 1 && !inputManager.IsPressed(AbstractKey.EditorUnlock))
            {
                BeginChapter(unlockedChapters.First());
                return;
            }

            UpdateButtons();
            base.Show(onFinish);
        }

        public void UpdateChapters()
        {
            activeChapters = new HashSet<string>(
                gameState.GetStartNodeNames(unlockDebugChapters ? StartNodeType.All : StartNodeType.Normal));
            var unlockedAtFirst = new HashSet<string>(
                gameState.GetStartNodeNames(unlockAllChapters ? StartNodeType.All : StartNodeType.Unlocked));
            unlockedChapters = new HashSet<string>(activeChapters.Where(
                name => unlockedAtFirst.Contains(name) || checkpointManager.IsReachedAnyHistory(name, 0)));
        }

        public IEnumerable<string> GetUnlockedChapters()
        {
            return unlockedChapters;
        }

        public void BeginChapter(string chapterName)
        {
            viewManager.GetController<TitleController>().SwitchView<DialogueBoxController>(() =>
            {
                gameState.GameStart(chapterName);
            });
        }

        private GameObject InitButton(string chapter)
        {
            var go = Instantiate(chapterButtonPrefab, chapterList);
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => Hide(() => BeginChapter(chapter)));
            return go;
        }

        private void UpdateButtons()
        {
            if (activeChapters == null)
            {
                return;
            }

            for (var i = 0; i < chapters.Count; ++i)
            {
                var chapter = chapters[i];
                var go = buttons[i];
                if (activeChapters.Contains(chapter))
                {
                    go.SetActive(true);
                    var text = go.GetComponent<Text>();
                    var button = go.GetComponent<Button>();
                    if (unlockedChapters.Contains(chapter))
                    {
                        text.text = I18n.__(gameState.GetNode(chapter).displayNames);
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
