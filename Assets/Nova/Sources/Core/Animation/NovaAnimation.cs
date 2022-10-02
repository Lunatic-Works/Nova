using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nova
{
    [ExportCustomType]
    [Flags]
    public enum AnimationType
    {
        None = 0,
        PerDialogue = 1,
        Holding = 2,
        UI = 4,
        Text = 8,
        All = 15
    }

    [ExportCustomType]
    public class NovaAnimation : MonoBehaviour
    {
        public AnimationType type = AnimationType.PerDialogue;

        #region Static members

        private static readonly List<NovaAnimation> Animations = new List<NovaAnimation>();

        // _animations may be mutated when some animation stops
        public static void StopAll(AnimationType type = AnimationType.All)
        {
            while (true)
            {
                var animation =
                    Animations.FirstOrDefault(_animation => type.HasFlag(_animation.type) && !_animation.isStopped);
                if (animation == null)
                {
                    break;
                }

                animation.Stop();
            }
        }

        public static bool IsPlayingAny(AnimationType type = AnimationType.All)
        {
            return Animations.Any(animation => type.HasFlag(animation.type) && animation.isPlaying);
        }

        public static float GetTotalDuration(AnimationType type = AnimationType.All)
        {
            return (from animation in Animations where type.HasFlag(animation.type) select animation.totalDuration)
                .Max();
        }

        public static float GetTotalTimeRemaining(AnimationType type = AnimationType.All)
        {
            return (from animation in Animations where type.HasFlag(animation.type) select animation.totalTimeRemaining)
                .Max();
        }

        private static void AddAnimation(NovaAnimation animation)
        {
            Animations.Add(animation);
        }

        private static void RemoveAnimation(NovaAnimation animation)
        {
            Animations.Remove(animation);
        }

        #endregion

        private void Start()
        {
            AnimationEntry.InitFactory();
        }

        private void OnEnable()
        {
            AddAnimation(this);
        }

        private void OnDisable()
        {
            Stop();
            RemoveAnimation(this);
        }

        public AnimationEntry Do(IAnimationProperty property, float duration = 0,
            AnimationEntry.EasingFunction easing = null, int repeatNum = 0)
        {
            var entry = AnimationEntry.CreateEntry(property, duration, easing, repeatNum, transform);
            entry.Play();
            return entry;
        }

        public bool isPlaying
        {
            get
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<AnimationEntry>().isPlaying) return true;
                }

                return false;
            }
        }

        public bool isStopped
        {
            get
            {
                foreach (Transform child in transform)
                {
                    if (!child.GetComponent<AnimationEntry>().isStopped) return false;
                }

                return true;
            }
        }

        public float totalDuration
        {
            get
            {
                float ret = 0.0f;
                foreach (Transform child in transform)
                {
                    float t = child.GetComponent<AnimationEntry>().totalDuration;
                    if (t > ret) ret = t;
                }

                return ret;
            }
        }

        public float totalTimeRemaining
        {
            get
            {
                float ret = 0.0f;
                foreach (Transform child in transform)
                {
                    float t = child.GetComponent<AnimationEntry>().totalTimeRemaining;
                    if (t > ret) ret = t;
                }

                return ret;
            }
        }

        #region Playback control

        public void Play()
        {
            foreach (Transform child in transform)
            {
                child.GetComponent<AnimationEntry>().Play();
            }
        }

        public void Pause()
        {
            foreach (Transform child in transform)
            {
                child.GetComponent<AnimationEntry>().Pause();
            }
        }

        public void Stop()
        {
            foreach (Transform child in Utils.GetChildren(transform))
            {
                child.GetComponent<AnimationEntry>().Stop();
            }
        }

        #endregion

        public static void DebugPrintEntriesAll(AnimationType type = AnimationType.All)
        {
            foreach (var animation in Animations)
            {
                if (type.HasFlag(animation.type))
                {
                    animation.DebugPrintEntries();
                }
            }
        }

        public void DebugPrintEntries()
        {
            Debug.Log($"DebugPrintEntries begin {this}");
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent<AnimationEntry>(out var entry)) continue;
                entry.DebugPrint(0);
            }

            Debug.Log("DebugPrintEntries end");
        }
    }
}
