using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Nova.Exceptions;
using UnityEngine;

namespace Nova
{
    public static class Utils
    {
        public static Sprite Texture2DToSprite(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        public static void RuntimeAssert(this MonoBehaviour mb, bool conditionToBeTrue, string msg)
        {
            if (!conditionToBeTrue)
            {
                throw new Exception(string.Format("Nova - {0}: {1}", mb.name, msg));
            }
        }

        public static TV Ensure<TK, TV>(this Dictionary<TK, TV> dict, TK key) where TV : new()
        {
            TV info;
            if (dict.TryGetValue(key, out info))
            {
                return info;
            }
            return dict[key] = new TV();
        }

        public static Vector2Int GetContainerSize(this Vector2Int contentSize, float containerAspectRatio)
        {
            var contentAspectRatio = 1.0f * contentSize.x / contentSize.y;
            if (contentAspectRatio > containerAspectRatio)
                return new Vector2Int(contentSize.x, (int) (contentSize.x / containerAspectRatio));
            return new Vector2Int((int) (contentSize.y * containerAspectRatio), contentSize.y);
        }

        public static GameObject FindNovaGameController()
        {
            var gameController = GameObject.FindWithTag("NovaGameController");
            if (gameController == null)
            {
                throw new InvalidAccessException(
                    "Nova: Can not find Nova game controller by tag. May be you should put" +
                    " NovaCreator prefab in your scene");
            }
            return gameController;
        }
    }

    public class Wrap<T>
    {
        public T value;
    }
}