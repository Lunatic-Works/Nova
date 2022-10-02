using System;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(SpriteCropper))]
    public class SpriteCropperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var cropper = target as SpriteCropper;
            var texture = cropper.sprite.texture;

            if (GUILayout.Button("Auto Crop"))
            {
                AutoCrop(cropper);
            }

            var scale = EditorGUIUtility.currentViewWidth / texture.width * 0.5f;
            var previewRect =
                EditorGUILayout.GetControlRect(false, scale * texture.height, GUILayout.Width(scale * texture.width));
            EditorGUI.DrawTextureTransparent(previewRect, texture);
            EditorUtils.DrawPreviewCaptureFrame(previewRect, cropper.boundRect.ToRect(), scale, true, Color.red);
            EditorUtils.DrawPreviewCaptureFrame(previewRect, cropper.cropRect.ToRect(), scale, true, Color.yellow);
        }

        private static int RoundUpToFour(int x)
        {
            return ((x + 3) / 4) * 4;
        }

        // Try to round up x2 - x1 to a multiplier of 4, while ensuring x1 >= left, x2 <= right
        private static void RoundWithBounds(ref int x1, ref int x2, int left, int right)
        {
            x2 = x1 + RoundUpToFour(x2 - x1);

            if (x1 < left)
            {
                if (left + x2 - x1 <= right)
                {
                    x2 = left + x2 - x1;
                }
                else
                {
                    x2 = right;
                }

                x1 = left;
            }

            if (x2 > right)
            {
                if (right - x2 + x1 >= left)
                {
                    x1 = right - x2 + x1;
                }
                else
                {
                    x1 = left;
                }

                x2 = right;
            }
        }

        public static void AutoCrop(SpriteCropper cropper)
        {
            var texture = cropper.sprite.texture;
            var colors = texture.GetPixels();

            var boundRect = cropper.boundRect;
            int left = Math.Max(0, boundRect.xMin);
            int right = Math.Min(texture.width, boundRect.xMax);
            int bottom = Math.Max(0, boundRect.yMin);
            int top = Math.Min(texture.height, boundRect.yMax);

            bool hasPixel = false;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (var i = bottom; i < top; ++i)
            {
                bool hasPixelInRow = false;
                for (var j = left; j < right; ++j)
                {
                    var color = colors[texture.width * i + j];
                    if (color.a > cropper.autoCropAlpha)
                    {
                        hasPixelInRow = true;
                        minX = Math.Min(minX, j);
                        break;
                    }
                }

                if (!hasPixelInRow)
                {
                    continue;
                }

                for (var j = right - 1; j >= left; --j)
                {
                    var color = colors[texture.width * i + j];
                    if (color.a > cropper.autoCropAlpha)
                    {
                        maxX = Math.Max(maxX, j);
                        break;
                    }
                }

                hasPixel = true;
                minY = Math.Min(minY, i);
                maxY = Math.Max(maxY, i);
            }

            if (hasPixel)
            {
                int padding = cropper.autoCropPadding;
                int x1 = Math.Max(left, minX - padding);
                int x2 = Math.Min(right, maxX + padding + 1);
                int y1 = Math.Max(bottom, minY - padding);
                int y2 = Math.Min(top, maxY + padding + 1);

                RoundWithBounds(ref x1, ref x2, left, right);
                RoundWithBounds(ref y1, ref y2, bottom, top);

                cropper.cropRect = new RectInt(x1, y1, x2 - x1, y2 - y1);
            }
            else
            {
                // Empty image
                var center = Vector2Int.RoundToInt(boundRect.center);
                cropper.cropRect = new RectInt(center.x - 2, center.y - 2, 4, 4);
            }

            EditorUtility.SetDirty(cropper);
        }
    }
}
