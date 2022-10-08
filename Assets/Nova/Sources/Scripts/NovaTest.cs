using System;
using System.Collections;
using UnityEngine;

namespace Nova
{
    public class NovaTest : MonoBehaviour
    {
        public bool running;
        public bool fastForward;
        public float delaySeconds = 0.8f;
        public int seed;

        private GameState gameState;
        private ViewManager viewManager;
        private DialogueBoxController dialogueBox;
        private SaveViewController saveView;
        private System.Random random;

        private void Awake()
        {
            var controller = Utils.FindNovaGameController();
            viewManager = Utils.FindViewManager();
            gameState = controller.GameState;
            dialogueBox = viewManager.GetController<DialogueBoxController>();
            saveView = viewManager.GetController<SaveViewController>();

            if (seed == 0)
            {
                seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            }
            random = new System.Random(seed);
        }

        private void Start()
        {
            if (running)
            {
                StartCoroutine(Mock());
            }
        }

        private WaitForSeconds delay => new WaitForSeconds(delaySeconds);

        private WaitWhile WaitForView(CurrentViewType viewType)
        {
            return new WaitWhile(() => viewManager.currentView != viewType);
        }

        private IEnumerator Mock()
        {
            yield return StartCoroutine(MockTitle());
            yield return StartCoroutine(MockGame());
        }

        private IEnumerator MockTitle()
        {
            // simply wait for game to start
            yield return WaitForView(CurrentViewType.Game);
        }

        private IEnumerator MockSave()
        {
            if (random.NextInt(2) == 0)
            {
                saveView.QuickSaveBookmark();
            }
            else
            {
                int index = (int)BookmarkType.NormalSave + random.NextInt(saveView.maxSaveEntry);
                saveView.ShowSave();
                yield return WaitForView(CurrentViewType.UI);
                yield return delay;
                saveView.SaveBookmark(index);
                yield return delay;
                saveView.Hide();
                yield return WaitForView(CurrentViewType.Game);
            }
        }

        private IEnumerator MockGame()
        {
            while (true)
            {
                yield return delay;

                if (gameState.isEnded)
                {
                    yield break;
                }

                bool isAnimating = NovaAnimation.IsPlayingAny(AnimationType.PerDialogue);
                bool textIsAnimating = NovaAnimation.IsPlayingAny(AnimationType.Text);

                if (!gameState.canStepForward)
                {
                    // in branch
                }
                else if (!isAnimating && !textIsAnimating)
                {
                    if (random.NextDouble() < 0.1)
                    {
                        Debug.Log("try save");
                        yield return StartCoroutine(MockSave());
                    }
                    else
                    {
                        Debug.Log("step dialogue");
                        dialogueBox.NextPageOrStep();
                    }
                }
                else if (fastForward)
                {
                    NovaAnimation.StopAll(AnimationType.PerDialogue | AnimationType.Text);
                }
            }
        }
    }
}
