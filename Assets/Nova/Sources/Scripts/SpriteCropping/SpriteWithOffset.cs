using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class SpriteWithOffset : ScriptableObject
    {
        public Sprite sprite;
        public Vector3 offset;
        public Vector3 scale = Vector3.one;
    }
}
