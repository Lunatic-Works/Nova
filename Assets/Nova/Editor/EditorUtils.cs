using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nova.Editor
{
    public static class EditorUtils
    {
        // https://gist.github.com/allanolivei/9260107
        public static string GetSelectedDirectory(string fallback = "Assets")
        {
            var path = fallback;

            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                path = Path.GetDirectoryName(path);
                break;
            }

            return path;
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

        public static void DrawPreviewCropFrame(Rect previewRect, Rect crop, Color color, float lineWidth = 1.0f)
        {
            var offset = previewRect.min + previewRect.size *
                new Vector2(crop.x, 1.0f - crop.y);
            var size = previewRect.size * crop.size;
            size.y = -size.y;
            DrawFrame(new Rect(offset, size), color, lineWidth);
        }

        public static IEnumerable<string> PathOfSelectedSprites()
        {
            // strange work around with unity
            foreach (var tex in Selection.GetFiltered<Texture2D>(SelectionMode.Assets))
            {
                Sprite sprite = null;
                try
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(tex));
                }
                catch
                {
                    // ignored
                }

                if (sprite != null)
                {
                    yield return AssetDatabase.GetAssetPath(sprite);
                }
            }
        }
    }
}