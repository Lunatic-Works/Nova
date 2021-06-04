using System;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    [CustomEditor(typeof(SpriteCropper))]
    public class SpriteCropperEditor : UnityEditor.Editor
    {
        private bool showCaptureBox;
        private RectInt captureBox = new RectInt(100, 100, 400, 400);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var cropper = target as SpriteCropper;
            var texture = cropper.sprite.texture;

            if (GUILayout.Button("Auto Crop"))
            {
                AutoCrop(cropper);
            }

            showCaptureBox = GUILayout.Toggle(showCaptureBox, "Show Capture Box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Capture Box");
            captureBox = EditorGUILayout.RectIntField(captureBox);
            GUILayout.EndHorizontal();

            var scale = EditorGUIUtility.currentViewWidth / texture.width;
            var previewRect = EditorGUILayout.GetControlRect(false, scale * texture.height);
            EditorGUI.DrawTextureTransparent(previewRect, texture);

            if (showCaptureBox)
            {
                EditorUtils.DrawPreviewCaptureFrame(previewRect, captureBox.ToRect(), scale, Color.red);
            }

            var cropRectInt = cropper.cropRect;
            var cropRect = new Rect(
                (float)cropRectInt.x / texture.width,
                (float)cropRectInt.y / texture.height,
                (float)cropRectInt.width / texture.width,
                (float)cropRectInt.height / texture.height
            );
            EditorUtils.DrawPreviewCropFrame(previewRect, cropRect, Color.yellow);
        }

        private static int RoundToFour(int v)
        {
            return ((v + 3) >> 2) << 2;
        }

        public static void AutoCrop(SpriteCropper cropper)
        {
            var texture = cropper.sprite.texture;
            var width = texture.width;
            var height = texture.height;
            var colors = texture.GetPixels();
            // scan from bottom to top
            int minX = width - 1, maxX = 0;
            int minY = -1, maxY = -1;
            for (var i = 0; i < height; i++)
            {
                var hasPixel = false;
                for (var j = 0; j < width; j++)
                {
                    var color = colors[width * i + j];
                    if (color.a > cropper.autoCropAlpha)
                    {
                        hasPixel = true;
                        minX = Math.Min(minX, j);
                        maxX = Math.Max(maxX, j);
                    }
                }

                if (hasPixel && minY == -1)
                {
                    minY = i;
                }
            }

            // scan from top to bottom
            for (var i = height - 1; i >= 0; i--)
            {
                var hasPixel = false;
                for (var j = 0; j < width; j++)
                {
                    var color = colors[width * i + j];
                    if (color.a > cropper.autoCropAlpha)
                    {
                        hasPixel = true;
                    }
                }

                if (hasPixel && maxY == -1)
                {
                    maxY = i;
                }
            }

            if (minY == -1 || maxY == -1)
            {
                // empty image
                cropper.cropRect = new RectInt(0, 0, 1, 1);
            }
            else
            {
                var padding = cropper.autoCropPadding;
                var x1 = Math.Max(0, minX - padding);
                var y1 = Math.Max(0, minY - padding);
                var x2 = Math.Min(maxX + padding, width - 1);
                var y2 = Math.Min(maxY + padding, height - 1);

                // try to round to 4
                var cw = RoundToFour(x2 - x1 + 1);
                var ch = RoundToFour(y2 - y1 + 1);

                if (x1 + cw > width)
                {
                    x1 = width - cw;
                }

                if (y1 + ch > height)
                {
                    y1 = height - ch;
                }

                x2 = x1 + cw - 1;
                y2 = y1 + ch - 1;

                // restrain in range
                x1 = Math.Max(0, x1);
                y1 = Math.Max(0, y1);

                cropper.cropRect = new RectInt(x1, y1, x2 - x1 + 1, y2 - y1 + 1);
            }

            EditorUtility.SetDirty(cropper);
        }
    }
}