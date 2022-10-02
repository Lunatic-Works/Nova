using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    public static class EditorUtils
    {
        public static string GetSelectedDirectory()
        {
            var path = "";
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                path = Path.GetDirectoryName(path);
                break;
            }

            return path;
        }

        public static IEnumerable<string> GetSelectedSpritePaths()
        {
            foreach (var tex in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(tex));
                if (sprite != null)
                {
                    yield return AssetDatabase.GetAssetPath(sprite);
                }
            }
        }

        public static void DrawFrame(Rect rect, Color color, float lineWidth = 1.0f)
        {
            var offset = rect.min;
            var size = rect.size;
            var halfLineWidth = lineWidth / 2.0f;
            EditorGUI.DrawRect(new Rect(
                offset.x - halfLineWidth, offset.y - halfLineWidth,
                lineWidth, size.y + lineWidth
            ), color);
            EditorGUI.DrawRect(new Rect(
                offset.x + size.x - halfLineWidth, offset.y - halfLineWidth,
                lineWidth, size.y + lineWidth
            ), color);
            EditorGUI.DrawRect(new Rect(
                offset.x - halfLineWidth, offset.y - halfLineWidth,
                size.x + lineWidth, lineWidth
            ), color);
            EditorGUI.DrawRect(new Rect(
                offset.x - halfLineWidth, offset.y + size.y - halfLineWidth,
                size.x + lineWidth, lineWidth
            ), color);
        }

        public static void DrawPreviewCropFrame(Rect preview, Rect crop, Color color, float lineWidth = 1.0f)
        {
            var offset = preview.min + preview.size * new Vector2(crop.x, 1.0f - crop.y);
            var size = preview.size * crop.size;
            size.y = -size.y;
            DrawFrame(new Rect(offset, size), color, lineWidth);
        }

        public static void DrawPreviewCaptureFrame(Rect preview, Rect capture, float scale, bool inverseY, Color color,
            float lineWidth = 1.0f)
        {
            var offset = preview.min + scale * capture.min;
            if (inverseY)
            {
                offset.y = preview.yMax - scale * capture.yMax;
            }

            var size = scale * capture.size;
            DrawFrame(new Rect(offset, size), color, lineWidth);
        }
    }
}
