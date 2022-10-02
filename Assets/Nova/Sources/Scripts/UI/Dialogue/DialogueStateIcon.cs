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
            img.color = Utils.SetAlpha(img.color, 0);
            t = 0;
        }

        private void Update()
        {
            t += Time.deltaTime;
            if (t > duration * 2)
            {
                t -= duration * 2;
            }

            if (t < duration)
            {
                img.color = Utils.SetAlpha(img.color, t / duration);
            }
            else
            {
                img.color = Utils.SetAlpha(img.color, 2 - t / duration);
            }
        }
    }
}
