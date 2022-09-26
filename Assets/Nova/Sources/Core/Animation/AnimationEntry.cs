using UnityEngine;

namespace Nova
{
    public enum AnimationEntryStatus
    {
        Paused,
        Playing,
        Stopped
    }

    /// <summary>
    /// Play animation for one shot. This component will destroy itself when animation finishes.
    /// </summary>
    [ExportCustomType]
    public class AnimationEntry : MonoBehaviour
    {
        #region Fields

        private static PrefabFactory PrefabFactory;

        /// <summary>
        /// The property to animate with. If the Property is null, this animation entry will do nothing, which can
        /// be used as waiting behaviour.
        /// </summary>
        public IAnimationProperty property { get; private set; }

        private float _duration;
        private float _invDuration; // Cached value for optimization.

        public float duration
        {
            get => _duration;
            private set
            {
                // Duration should be greater than 0.
                if (value <= 0.0f)
                {
                    _duration = 0.0f;
                    _invDuration = 0.0f;
                }
                else
                {
                    _duration = value;
                    _invDuration = 1.0f / value;
                }
            }
        }

        public float timeElapsed { get; private set; }
        public float timeRemaining => duration - timeElapsed;

        public EasingFunction easing = LinearEasing();

        /// <summary>
        /// If RepeatNum = 0, no loop, play once.
        /// If RepeatNum > 0, this value indicates how many times this track will be repeated, with current playing
        /// round uncounted. Set RepeatNum = n, the track will be played for actually n + 1 times.
        /// If RepeatNum == -1, infinite loop.
        /// </summary>
        public int repeatNum { get; private set; }

        public int repeatNumRemaining { get; private set; }

        /// <summary>
        /// Total duration of this entry and all its children, accounting for the repeat num.
        /// </summary>
        public float totalDuration
        {
            get
            {
                if (repeatNum == -1) return float.PositiveInfinity;
                float ret = 0.0f;
                foreach (Transform child in transform)
                {
                    if (!child.TryGetComponent<AnimationEntry>(out var anim)) continue;
                    float d = anim.totalDuration;
                    if (d > ret) ret = d;
                }

                return ret + duration * (repeatNum + 1);
            }
        }

        /// <summary>
        /// How much time is needed to finish playing the entry and all its children.
        /// </summary>
        public float totalTimeRemaining => totalDuration - timeElapsed;

        public AnimationEntryStatus status { get; private set; }

        public bool isPlaying => status == AnimationEntryStatus.Playing;
        public bool isStopped => status == AnimationEntryStatus.Stopped;

        // Used for the final action in loop().
        public bool evaluateOnStop;

        #endregion

        #region Create entry and set properties

        public void Init(
            IAnimationProperty property,
            float duration,
            EasingFunction easing,
            int repeatNum)
        {
            this.property = property;
            For(duration);
            With(easing);
            Repeat(repeatNum);
            status = AnimationEntryStatus.Paused;
            evaluateOnStop = true;
        }

        private static GameObject CreateEntryGameObject()
        {
            var go = new GameObject("AnimationEntry");
            go.AddComponent<AnimationEntry>();
            return go;
        }

        public static void InitFactory()
        {
            if (PrefabFactory != null) return;

            var go = new GameObject("AnimationEntryFactory");
            PrefabFactory = go.AddComponent<PrefabFactory>();
            PrefabFactory.creator = CreateEntryGameObject;
            PrefabFactory.maxBufferSize = 100;
        }

        public static AnimationEntry CreateEntry(
            IAnimationProperty property,
            float duration,
            EasingFunction easing,
            int repeatNum,
            Transform parent)
        {
            var entry = PrefabFactory.Get<AnimationEntry>();
            entry.transform.SetParent(parent, false);
            entry.Init(property, duration, easing, repeatNum);
            return entry;
        }

        public static void DestroyEntry(AnimationEntry entry)
        {
            entry.property = null;
            PrefabFactory.Put(entry.gameObject);
        }

        /// <summary>
        /// Add a new animation entry. It will start playing together with the current animation entry.
        /// </summary>
        public AnimationEntry And(IAnimationProperty property, float duration = 0.0f, EasingFunction easing = null,
            int repeatNum = 0)
        {
            var entry = CreateEntry(property, duration, easing, repeatNum, transform.parent);
            entry.status = status;
            return entry;
        }

        /// <summary>
        /// Add a new animation entry. It will start playing after the current animation entry finishes.
        /// </summary>
        public AnimationEntry Then(IAnimationProperty property, float duration = 0.0f, EasingFunction easing = null,
            int repeatNum = 0)
        {
            return CreateEntry(property, duration, easing, repeatNum, transform);
        }

        public AnimationEntry For(float duration)
        {
            this.duration = duration;
            timeElapsed = 0.0f;
            return this;
        }

        public AnimationEntry With(EasingFunction function)
        {
            easing = function ?? LinearEasing();
            return this;
        }

        public AnimationEntry Repeat(int repeatNum)
        {
            this.repeatNum = repeatNum;
            repeatNumRemaining = repeatNum;
            return this;
        }

        #endregion

        #region Playback control

        public void Play()
        {
            status = AnimationEntryStatus.Playing;
        }

        public void Pause()
        {
            status = AnimationEntryStatus.Paused;
        }

        public void Stop()
        {
            // Without this, if property is ActionAnimationProperty(() => Stop()), it will cause a following stack trace:
            // Update -> WakeUpChildren -> Terminate (a) -> set Property.value -> Stop (b)
            // Both a and b will DestroyEntry
            // And there will be duplicate this in factory...
            if (isStopped) return;

            if (evaluateOnStop) Terminate();
            // Even if not EvaluateOnStop, set Status = Stopped to avoid Terminate() multiple times
            status = AnimationEntryStatus.Stopped;

            foreach (Transform child in Utils.GetChildren(transform))
            {
                child.GetComponent<AnimationEntry>().Stop();
            }

            DestroyEntry(this);
        }

        // Remove without evaluating
        public void Remove()
        {
            if (isStopped) return;

            status = AnimationEntryStatus.Stopped;

            foreach (Transform child in Utils.GetChildren(transform))
            {
                child.GetComponent<AnimationEntry>().Remove();
            }

            DestroyEntry(this);
        }

        #endregion

        private void Terminate()
        {
            if (property == null || isStopped) return;
            status = AnimationEntryStatus.Stopped;
            property.value = easing(1.0f);
        }

        private void Evaluate()
        {
            if (property == null || isStopped) return;
            property.value = easing(timeElapsed * _invDuration);
        }

        private void WakeUpChildren()
        {
            Terminate();

            foreach (Transform child in Utils.GetChildren(transform))
            {
                child.SetParent(transform.parent, false);
                child.GetComponent<AnimationEntry>().Play();
            }

            DestroyEntry(this);
        }

        private void Update()
        {
            if (!isPlaying) return;

            timeElapsed += Time.deltaTime;
            if (timeElapsed < duration)
            {
                Evaluate();
                return;
            }

            // check if need loop
            if (repeatNumRemaining == 0) // no more loop
            {
                WakeUpChildren();
                return;
            }

            if (repeatNumRemaining > 0) repeatNumRemaining--;
            timeElapsed -= duration;

            Evaluate();
        }

        #region Easing functions

        /// <summary>
        /// Input is the normalized time in [0, 1].
        /// Output is a float used to interpolate between Property's startValue and targetValue. 0 means startValue, and 1 means targetValue.
        /// Output may be outside [0, 1], and it may not start with 0 or end with 1.
        /// </summary>
        public delegate float EasingFunction(float t);

        public static EasingFunction LinearEasing()
        {
            return t => t;
        }

        public static EasingFunction CubicEasing(float startSlope, float targetSlope)
        {
            float a = startSlope + targetSlope - 2.0f;
            float b = -2.0f * startSlope - targetSlope + 3.0f;
            float c = startSlope;
            return t => ((a * t + b) * t + c) * t;
        }

        public static EasingFunction ShakeEasing(float freq, float pow)
        {
            return t => Mathf.Sin(freq * t) * Mathf.Pow(1.0f - t, pow);
        }

        public static EasingFunction ShakeSquaredEasing(float freq, float pow)
        {
            return t => Mathf.Pow(Mathf.Sin(freq * t), 2.0f) * Mathf.Pow(1.0f - t, pow);
        }

        public static EasingFunction BezierEasing(float x0, float y0)
        {
            if (Mathf.Abs(x0 - 0.5f) < 0.001f)
            {
                return t => ((1.0f - 2.0f * y0) * t + 2.0f * y0) * t;
            }
            else
            {
                float x2 = 2.0f * x0 - 1.0f;
                return t => (-2.0f * (Mathf.Sqrt(x0 * x0 - x2 * t) - x0) * (x0 - y0) + t * x2 * (2.0f * y0 - 1.0f)) /
                            (x2 * x2);
            }
        }

        #endregion

        public void DebugPrint(int level)
        {
            Debug.Log($"{new string('+', level)}{property} {duration} {status}");
            foreach (Transform child in transform)
            {
                if (!child.TryGetComponent<AnimationEntry>(out var entry)) continue;
                entry.DebugPrint(level + 1);
            }
        }
    }
}
