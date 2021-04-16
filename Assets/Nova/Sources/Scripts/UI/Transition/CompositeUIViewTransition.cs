using System;

namespace Nova
{
    public class CompositeUIViewTransition : UIViewTransitionBase
    {
        public TransitionSequenceItem[] forwardChildTransitions;
        public TransitionSequenceItem[] backwardChildTransitions;

        private float forwardEnding, backwardEnding;
        private int forwardEndingIdx, backwardEndingIdx;

        public override float enterDuration => forwardEnding;

        public override float exitDuration => backwardEnding;

        public override void Awake()
        {
            base.Awake();
            BuildSequence(true);
            BuildSequence(false);
        }

        private void BuildSequence(bool isEnter)
        {
            var seq = isEnter ? forwardChildTransitions : backwardChildTransitions;
            float globalEnding = 0, lastBeginning = 0, lastEnding = 0;
            int maxIdx = 0;
            for (int i = 0; i < seq.Length; i++)
            {
                var item = seq[i];
                float beginning, ending;
                switch (item.offsetBasedOn)
                {
                    case TransitionSequenceOffsetBase.GlobalBeginning:
                        beginning = item.offset;
                        break;
                    case TransitionSequenceOffsetBase.GlobalEndingTillNow:
                        beginning = globalEnding;
                        break;
                    case TransitionSequenceOffsetBase.LastItemBeginning:
                        beginning = lastBeginning;
                        break;
                    case TransitionSequenceOffsetBase.LastItemEnding:
                    default:
                        beginning = lastEnding;
                        break;
                }

                beginning += item.offset;
                ending = beginning + (isEnter ? item.transition.enterDuration : item.transition.exitDuration);
                if (ending >= globalEnding)
                {
                    maxIdx = i;
                    globalEnding = ending;
                }

                lastBeginning = beginning;
                lastEnding = ending;
                item.absoluteOffsetInParent = beginning;
            }

            if (isEnter)
            {
                forwardEnding = globalEnding;
                forwardEndingIdx = maxIdx;
            }
            else
            {
                backwardEnding = globalEnding;
                backwardEndingIdx = maxIdx;
            }
        }

        private void RunSequence(bool isEnter, Action onAnimationFinish)
        {
            var seq = isEnter ? forwardChildTransitions : backwardChildTransitions;
            for (int i = 0; i < seq.Length; i++)
            {
                var item = seq[i];
                if (isEnter)
                {
                    item.transition.Enter(i == forwardEndingIdx ? onAnimationFinish : null,
                        item.absoluteOffsetInParent + delayOffset);
                }
                else
                {
                    item.transition.Exit(i == backwardEndingIdx ? onAnimationFinish : null,
                        item.absoluteOffsetInParent + delayOffset);
                }
            }
        }

        protected internal override void OnBeforeEnter()
        {
            base.OnBeforeEnter();
            foreach (var item in forwardChildTransitions)
            {
                item.transition.OnBeforeEnter();
            }
        }

        protected override void OnEnter(Action onAnimationFinish)
        {
            RunSequence(true, onAnimationFinish);
        }

        protected override void OnExit(Action onAnimationFinish)
        {
            RunSequence(false, onAnimationFinish);
        }
    }
}