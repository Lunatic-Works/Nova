using System;
using UnityEngine;

namespace Nova
{
    [Serializable]
    [CreateAssetMenu(menuName = "Nova/Sprite With Offset")]
    public class SpriteWithOffset : ScriptableObject
    {
        public Sprite sprite;
        public Vector3 offset;
    }
}
