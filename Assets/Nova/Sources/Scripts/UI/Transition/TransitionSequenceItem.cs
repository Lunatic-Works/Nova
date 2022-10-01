using System;
using UnityEngine;

namespace Nova
{
    public enum TransitionSequenceOffsetBase
    {
        GlobalBeginning,
        GlobalEndingTillNow,
        LastItemBeginning,
        LastItemEnding
    }

    [Serializable]
    public class TransitionSequenceItem
    {
        [Tooltip("Choose which point the offset is based on")]
        public TransitionSequenceOffsetBase offsetBasedOn;

        [Tooltip("Offset time in seconds")] public float offset;
        public UIViewTransitionBase transition;

        [HideInInspector] public float absoluteOffsetInParent;
    }
}
