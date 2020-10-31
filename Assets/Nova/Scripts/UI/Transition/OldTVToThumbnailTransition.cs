using System;
using UnityEngine;

namespace Nova
{
    /// <summary>
    /// If BarrelMaterial is set, then an additional barrel transition will be applied
    /// </summary>
    public class OldTVToThumbnailTransition : UIViewTransitionBase
    {
        private const string BarrelShaderCurvatureProperty = "_Sigma";

        private static readonly int BarrelShaderCurvaturePropertyID =
            Shader.PropertyToID(BarrelShaderCurvatureProperty);

        public RectTransform thumbnailTransform;
        public Material barrelMaterial;
        public float barrelDuration = 0.5f;
        public float barrelCurvature = 0.3f;
        public float fadeDuration = 0.5f;
        public float scaleDuration = 0.5f;

        public override float enterDuration => barrelDuration + fadeDuration;

        public override float exitDuration => barrelDuration + fadeDuration;

        private Vector2 tPos0, tSize0;
        private Material barrelMaterialInstance;
        private PostProcessing uiPP;

        public override void Awake()
        {
            base.Awake();

            if (barrelMaterial != null)
                barrelMaterialInstance = new Material(barrelMaterial);

            uiPP = UICameraHelper.Active.GetComponent<PostProcessing>();
        }

        public override void ResetTransitionTarget()
        {
            base.ResetTransitionTarget();

            tPos0 = thumbnailTransform.position;
            tSize0 = thumbnailTransform.rect.size;
        }

        private void CalculateTransform(out Vector2 newPos, out Vector2 newSize)
        {
            float scale = Mathf.Min(
                tSize0.x / size0.x,
                tSize0.y / size0.y
            );
            var _transform = Matrix4x4.TRS(
                tPos0,
                Quaternion.identity,
                new Vector3(scale, scale, 1)
            );
            newPos = _transform.inverse.MultiplyPoint(pos0);
            newSize = size0 / scale;
        }

        protected override void OnEnter(Action onAnimationFinish)
        {
            CalculateTransform(out var newPos, out var newSize);
            AnimationEntry entry = GetBaseAnimationEntry();
            if (barrelMaterial != null)
            {
                uiPP.PushMaterial(barrelMaterialInstance);
                barrelMaterialInstance.SetFloat(BarrelShaderCurvaturePropertyID, 0);
                entry = entry.Then(
                        new MaterialFloatAnimationProperty(barrelMaterialInstance, BarrelShaderCurvatureProperty, 0,
                            barrelCurvature),
                        barrelDuration
                    )
                    .Then(new ActionAnimationProperty(() => uiPP.ClearLayer()))
                    .Then(GetOpacityAnimationProperty(0, 1), fadeDuration)
                    .Then(new RectTransformAnimationProperty(rt, newPos, pos0, newSize, size0), scaleDuration)
                    .With(enterFunction);
            }
            else
                entry = entry.Then(GetOpacityAnimationProperty(0, 1), fadeDuration)
                    .Then(new RectTransformAnimationProperty(rt, newPos, pos0, newSize, size0), scaleDuration)
                    .With(enterFunction);

            if (onAnimationFinish != null)
                entry.Then(new ActionAnimationProperty(onAnimationFinish));
        }

        protected override void OnExit(Action onAnimationFinish)
        {
            CalculateTransform(out var newPos, out var newSize);
            AnimationEntry entry = GetBaseAnimationEntry();
            if (barrelMaterial != null)
            {
                barrelMaterialInstance.SetFloat(BarrelShaderCurvaturePropertyID, barrelCurvature);
                entry = entry.Then(new RectTransformAnimationProperty(rt, pos0, newPos, size0, newSize), scaleDuration)
                    .With(exitFunction)
                    .Then(GetOpacityAnimationProperty(1, 0), fadeDuration)
                    .Then(new ActionAnimationProperty(() => uiPP.PushMaterial(barrelMaterialInstance)))
                    .Then(
                        new MaterialFloatAnimationProperty(barrelMaterialInstance, BarrelShaderCurvatureProperty,
                            barrelCurvature, 0), barrelDuration)
                    .Then(new ActionAnimationProperty(() => uiPP.ClearLayer()));
            }
            else
                entry = entry.Then(new RectTransformAnimationProperty(rt, pos0, newPos, size0, newSize), scaleDuration)
                    .With(exitFunction)
                    .Then(GetOpacityAnimationProperty(1, 0), fadeDuration);

            entry.Then(new ActionAnimationProperty(() =>
            {
                SetToTransitionTarget();
                onAnimationFinish?.Invoke();
            }));
        }
    }
}