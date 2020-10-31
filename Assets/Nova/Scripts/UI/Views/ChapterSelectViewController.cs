using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class ChapterSelectViewController : ViewControllerBase
    {
        public GameObject chapterButtonPrefab;
        public GameObject chapterList;
        public Button returnButton;
        public LogController logController;
        public bool unlockAllChaptersForDebug;

        // private const string GameFirstShownKey = "_Game" + ConfigViewController.FirstShownKeySuffix;

        private GameState gameState;

        private CheckpointManager checkpointManager;

        // private ConfigManager configManager;
        private Dictionary<string, GameObject> buttons;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            checkpointManager = controller.CheckpointManager;
            // configManager = controller.ConfigManager;
            returnButton.onClick.AddListener(Hide);
        }

        protected override void Start()
        {
            base.Start();

            buttons = gameState.GetAllStartupNodeNames().Select(chapter =>
            {
                var chapterButton = Instantiate(chapterButtonPrefab, chapterList.transform);
                var button = chapterButton.GetComponent<Button>();
                button.onClick.AddListener(() => Hide(() => BeginChapter(chapter)));
                return new KeyValuePair<string, GameObject>(chapter, chapterButton);
            }).ToDictionary(p => p.Key, p => p.Value);
        }

        public override void Show(Action onFinish)
        {
            var chapterNames = gameState.GetAllStartupNodeNames();
            var reachedChaptersCount =
                chapterNames.Count(name => checkpointManager.IsReachedForAnyVariables(name, 0) != null);
            if (reachedChaptersCount < 2 && !unlockAllChaptersForDebug && !Utils.GetKeyInEditor(KeyCode.LeftShift))
            {
                BeginChapter();
                return;
            }

            UpdateAllButtons();
            base.Show(onFinish);
        }

        private void BeginChapter(string chapterName = null)
        {
            viewManager.GetController<TitleController>().SwitchView<DialogueBoxController>(() =>
            {
                // if (configManager.GetInt(GameFirstShownKey) == 0)
                // {
                //     if (Application.isMobilePlatform)
                //     {
                //         Alert.Show(I18n.__("ingame.first.hint.touch"), () => { Alert.Show(I18n.__("ingame.first.auto")); });
                //     }
                //     else
                //     {
                //         Alert.Show(I18n.__("ingame.first.hint.mouse"), () => { Alert.Show(I18n.__("ingame.first.auto")); });
                //     }
                //     configManager.SetInt(GameFirstShownKey, 1);
                // }

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
                if (unlockAllChaptersForDebug || checkpointManager.IsReachedForAnyVariables(chapter.Key, 0) != null)
                {
                    chapter.Value.GetComponent<Button>().enabled = true;
                    chapter.Value.GetComponent<Text>().text = I18nHelper.NodeNames.Get(chapter.Key);
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
                    chapter.Value.GetComponent<Text>().text = I18nHelper.NodeNames.Get(chapter.Key);
                }
            }
        }
    }
}