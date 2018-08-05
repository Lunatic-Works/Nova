using System;
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
    }
}