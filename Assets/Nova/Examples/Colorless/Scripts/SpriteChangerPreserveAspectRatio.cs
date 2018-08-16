using UnityEngine;
using UnityEngine.UI;

namespace Nova.Examples.Colorless.Scripts
{
    /// <summary>
    /// Ugly implementation only used for demo.
    /// </summary>
    public class SpriteChangerPreserveAspectRatio : MonoBehaviour
    {
        private SpriteChangerWithFade _spriteChanger;

        private AspectRatioFitter _aspectRatioFitter;

        private void Awake()
        {
            _spriteChanger = GetComponent<SpriteChangerWithFade>();
            _aspectRatioFitter = GetComponent<AspectRatioFitter>();
        }

        public Sprite sprite
        {
            get { return _spriteChanger.sprite; }
            set
            {
                if (value == null)
                {
                    _spriteChanger.sprite = null;
                    return;
                }
                var ratio = value.texture.width / (float) value.texture.height;
                _aspectRatioFitter.aspectRatio = ratio;
                _spriteChanger.sprite = value;
            }
        }
    }
}