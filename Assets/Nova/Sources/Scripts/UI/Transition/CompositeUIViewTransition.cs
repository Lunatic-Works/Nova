using System;
using System.Collections.Generic;

namespace Nova
{
    public class CompositeUIViewTransition : UIViewTransitionBase
    {
        public List<TransitionSequenceItem> forwardChildTransitions;
        public List<TransitionSequenceItem> backwardChildTransitions;

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

        private void CheckTransition(bool isEnter, UIViewTransitionBase transition, int id)
        {
            var name = isEnter ? "Forward" : "Backward";
            var path = Utils.GetPath(transform);
            this.RuntimeAssert(transition != null, $"{name} transition {id} is null in {path}.");
        }

        private void BuildSequence(bool isEnter)
        {
            var seq = isEnter ? forwardChildTransitions : backwardChildTransitions;
            float globalEnding = 0f, lastBeginning = 0f, lastEnding = 0f;
            int maxIdx = 0;
            for (int i = 0; i < seq.Count; ++i)
            {
                var item = seq[i];
                CheckTransition(isEnter, item.transition, i);

                float beginning;
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
                        beginning = lastEnding;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                beginning += item.offset;
                float ending = beginning + (isEnter ? item.transition.enterDuration : item.transition.exitDuration);
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

        private void RunSequence(bool isEnter, Action onFinish)
        {
            var seq = isEnter ? forwardChildTransitions : backwardChildTransitions;
            for (int i = 0; i < seq.Count; ++i)
            {
                var item = seq[i];
                if (isEnter)
                {
                    item.transition.Enter(i == forwardEndingIdx ? onFinish : null,
                        item.absoluteOffsetInParent + delayOffset);
                }
                else
                {
                    item.transition.Exit(i == backwardEndingIdx ? onFinish : null,
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

        protected override void OnEnter(Action onFinish)
        {
            RunSequence(true, onFinish);
        }

        protected override void OnExit(Action onFinish)
        {
            RunSequence(false, onFinish);
        }
    }
}
