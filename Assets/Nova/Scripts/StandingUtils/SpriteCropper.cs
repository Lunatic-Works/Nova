using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteCropper : MonoBehaviour
    {
        public RectInt cropRect = new RectInt();
        public int autoCropPadding = 2;
        [Range(0, 1)] public float autoCropAlpha = 0.01f;

        public Sprite sprite => GetComponent<SpriteRenderer>().sprite;
    }
}