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
        [SerializeField] private float bgmFadeOutDuration = 1.0f;

        private const string SelectChapterFirstShownKey = ConfigManager.FirstShownKeyPrefix + "SelectChapter";

        private GameState gameState;
        private ConfigManager configManager;
        private CheckpointManager checkpointManager;
        private NovaAnimation novaAnimation;
        private int unlockedStartCount;

        protected override void Awake()
        {
            base.Awake();

            var controller = Utils.FindNovaController();
            gameState = controller.GameState;
            configManager = controller.ConfigManager;
            checkpointManager = controller.CheckpointManager;
            novaAnimation = controller.UIAnimation;

            quitButton.onClick.AddListener(() => this.Hide(Utils.Quit));
        }

        protected override void Start()
        {
            base.Start();

            unlockedStartCount = gameState.GetStartNodeNames(StartNodeType.Unlocked).Count();
            Show();
        }

        public override void Show(bool doTransition, Action onFinish)
        {
            base.Show(doTransition, () =>
            {
                viewManager.GetController<GameViewController>().HideImmediate();
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

        public override void Hide(bool doTransition, Action onFinish)
        {
            if (bgmController != null && !string.IsNullOrEmpty(bgmName))
            {
                novaAnimation.Do(new VolumeAnimationProperty(bgmController, 0.0f), bgmFadeOutDuration)
                    .Then(new ActionAnimationProperty(bgmController.Stop));
            }

            base.Hide(doTransition, onFinish);
        }

        // Disable BackHide
        protected override void Update() { }
    }
}
