using System;
using System.Linq;
using UnityEngine.UI;

namespace Nova
{
    public class TitleController : ViewControllerBase
    {
        public Button exitButton;
        public AudioController bgmController;
        public string bgmName;
        public float bgmVolume = 0.5f;

        private const string SelectChapterFirstShownKey = ConfigManager.FirstShownKeyPrefix + "SelectChapter";

        private GameState gameState;
        private ConfigManager configManager;
        private CheckpointManager checkpointManager;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaGameController();
            gameState = controller.GameState;
            configManager = controller.ConfigManager;
            checkpointManager = controller.CheckpointManager;

            exitButton.onClick.AddListener(() =>
                Hide(Utils.Quit)
            );
        }

        protected override void Start()
        {
            base.Start();

            checkpointManager.Init();
            gameState.SaveInitialCheckpoint();
            Show(null);
        }

        public override void Show(Action onFinish)
        {
            base.Show(() =>
            {
                viewManager.dialoguePanel.SetActive(false);
                viewManager.StopAllAnimations();
                gameState.ResetGameState();

                if (bgmController != null && !string.IsNullOrEmpty(bgmName))
                {
                    bgmController.scriptVolume = bgmVolume;
                    bgmController.Play(bgmName);
                }

                if (configManager.GetInt(SelectChapterFirstShownKey) == 0)
                {
                    var unlockedChapterCount = gameState.GetAllUnlockedStartNodeNames().Count;
                    var reachedChapterCount = gameState.GetAllStartNodeNames()
                        .Count(name => checkpointManager.IsReachedAnyHistory(name, 0));
                    if (unlockedChapterCount == 1 && reachedChapterCount > 1)
                    {
                        Alert.Show(I18n.__("title.first.selectchapter"));
                        configManager.SetInt(SelectChapterFirstShownKey, 1);
                    }
                }

                onFinish?.Invoke();
            });
        }

        protected override void Update() { }
    }
}