using System;
using UnityEngine;

namespace Nova
{
    public enum UIViewMovementDirection
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4
    }

    public enum UIViewScalingDirection
    {
        None = 0,
        Expand = 1,
        Shrink = 2
    }

    public enum UIViewRotationDirection
    {
        None = 0,
        Clockwise = -1,
        CounterClockwise = 1
    }

    public class BasicUIViewTransition : UIViewTransitionBase
    {
        private static readonly Vector2[] Dir2Vector =
            {Vector2.zero, Vector2.up, Vector2.right, Vector2.down, Vector2.left};

        private static readonly float[] Dir2Scale = {0, -1, 1};

        public UIViewMovementDirection movementDirection = UIViewMovementDirection.None;
        public UIViewScalingDirection scalingDirection = UIViewScalingDirection.None;
        public UIViewRotationDirection rotationDirection = UIViewRotationDirection.None;
        public float movementPercentage = 0.5f;
        public float scalingPercentage = 0.5f;
        public float rotationPercentage = 0.5f;
        public bool fade = true;
        public float duration = 0.5f;

        public override float enterDuration => duration;

        public override float exitDuration => duration;

        protected internal override void OnBeforeEnter()
        {
            base.OnBeforeEnter();
            if (!useGhost && fade)
            {
                cg.alpha = 0;
            }
        }

        protected override void OnEnter(Action onAnimationFinish)
        {
            AnimationEntry current = GetBaseAnimationEntry();
            bool hasAnimation = false;
            if (fade)
            {
                current = current.Then(GetOpacityAnimationProperty(0, 1), duration);
                hasAnimation = true;
            }

            if (movementDirection != UIViewMovementDirection.None || scalingDirection != UIViewScalingDirection.None)
            {
                var delta = size0.InverseScale(RealScreen.uiSize) * (UICameraHelper.Active.orthographicSize * 2);
                delta.Scale(Dir2Vector[(int)movementDirection] * movementPercentage);
                var size = size0 * (1 + Dir2Scale[(int)scalingDirection] * scalingPercentage);
                var prop = new RectTransformAnimationProperty(rt, pos0 - delta, pos0, size.CloneScale(scale0),
                    size0.CloneScale(scale0));
                current = hasAnimation ? current.And(prop, duration) : current.Then(prop, duration);
                current.With(enterFunction);
                hasAnimation = true;
            }

            if (rotationDirection != UIViewRotationDirection.None)
            {
                var prop = new RotationAnimationProperty(rt, Vector3.zero);
                float angle = 360 * rotationPercentage * (int)rotationDirection;
                rt.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
                current = hasAnimation ? current.And(prop, duration) : current.Then(prop, duration);
                current.With(enterFunction);
            }

            if (onAnimationFinish != null)
                current.Then(new ActionAnimationProperty(onAnimationFinish));
        }

        protected override void OnExit(Action onAnimationFinish)
        {
            AnimationEntry current = GetBaseAnimationEntry();
            bool hasAnimation = false;
            if (fade)
            {
                current = current.Then(GetOpacityAnimationProperty(1, 0), duration);
                hasAnimation = true;
            }

            if (movementDirection != UIViewMovementDirection.None || scalingDirection != UIViewScalingDirection.None)
            {
                var delta = size0.InverseScale(RealScreen.uiSize) * (UICameraHelper.Active.orthographicSize * 2);
                delta.Scale(Dir2Vector[(int)movementDirection] * movementPercentage);
                var size = size0 * (1 + Dir2Scale[(int)scalingDirection] * scalingPercentage);
                var prop = new RectTransformAnimationProperty(rt, pos0, pos0 - delta, size0.CloneScale(scale0),
                    size.CloneScale(scale0));
                current = hasAnimation ? current.And(prop, duration) : current.Then(prop, duration);
                current.With(exitFunction);
                hasAnimation = true;
            }

            if (rotationDirection != UIViewRotationDirection.None)
            {
                float angle = 360 * rotationPercentage * (int)rotationDirection;
                var prop = new RotationAnimationProperty(rt, new Vector3(0, 0, angle));
                rt.rotation = Quaternion.identity;
                current = hasAnimation ? current.And(prop, duration) : current.Then(prop, duration);
                current.With(exitFunction);
            }

            current.Then(new ActionAnimationProperty(() =>
            {
                onAnimationFinish?.Invoke();
                SetToTransitionTarget();
            }));
        }
    }
}