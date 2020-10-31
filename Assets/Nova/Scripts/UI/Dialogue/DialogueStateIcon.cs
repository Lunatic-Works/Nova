using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    [RequireComponent(typeof(Image))]
    public class DialogueStateIcon : MonoBehaviour
    {
        public float duration = 1.0f;

        private Image img;
        private float t;

        private void Awake()
        {
            img = GetComponent<Image>();
            var c = img.color;
            c.a = 0;
            img.color = c;
            t = 0;
        }

        private void Update()
        {
            t += Time.deltaTime;
            Color newColor = img.color;
            if (t > duration * 2)
            {
                t -= duration * 2;
            }

            if (t < duration)
            {
                newColor.a = t / duration;
            }
            else
            {
                newColor.a = 2 - t / duration;
            }

            img.color = newColor;
        }
    }
}