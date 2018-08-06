using System;
using System.Collections.Generic;
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
                return info;
            return dict[key] = new TV();
        }
    }

    public class Wrap<T>
    {
        public T value;
    }
}