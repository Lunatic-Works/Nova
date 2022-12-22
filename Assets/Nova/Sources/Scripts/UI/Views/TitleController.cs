using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class TitleController : ViewControllerBase
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private AudioController bgmController;
        [SerializeField] private string bgmName;
        [SerializeField] private float bgmVolume = 0.5f;

        private const string SelectChapterFirstShownKey = ConfigManager.FirstShownKeyPrefix + "SelectChapter";

        private GameState gameState;
        private ConfigManager configManager;
        private CheckpointManager checkpointManager;
        private int unlockedStartCount;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            configManager = controller.ConfigManager;
            checkpointManager = controller.CheckpointManager;

            quitButton.onClick.AddListener(() => Hide(Utils.Quit));
        }

        protected override void Start()
        {
            base.Start();

            unlockedStartCount = gameState.GetStartNodeNames(StartNodeType.Unlocked).Count();
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
                    var reachedChapterCount = gameState.GetStartNodeNames()
                        .Count(name => checkpointManager.IsReachedAnyHistory(name, 0));
                    if (unlockedStartCount == 1 && reachedChapterCount > 1)
                    {
                        Alert.Show("title.first.selectchapter");
                        configManager.SetInt(SelectChapterFirstShownKey, 1);
                    }
                }

                onFinish?.Invoke();
            });
        }

        // Disable BackHide
        protected override void Update() { }
    }
}
