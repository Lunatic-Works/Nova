using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextProxy : MonoBehaviour
    {
        private static readonly HashSet<char> ChineseFollowingPunctuations = new HashSet<char>("，。、；：？！…—‘’“”（）【】《》");

        private static bool IsChineseCharacter(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        public TMP_Text textBox { get; private set; }

        private bool inited;
        private bool needRefreshLineBreak;
        private bool needRefreshFade;
        private bool needRefreshAtNextFrame;

        private void Awake()
        {
            Init();
        }

        // Awake() may not be called early enough
        public void Init()
        {
            if (inited) return;
            textBox = GetComponent<TMP_Text>();
            inited = true;
        }

        // When calling ScheduleRefresh(), the transform size may change in
        // LateUpdate() of the current frame
        public void ScheduleRefresh()
        {
            needRefreshAtNextFrame = true;
        }

        private string _text;

        public string text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;
                textBox.text = value;
                needRefreshLineBreak = true;
            }
        }

        private string _materialName;

        public string materialName
        {
            get => _materialName;
            set
            {
                if (_materialName == value) return;
                _materialName = value;
                UpdateFont();
            }
        }

        public void UpdateFont()
        {
            foreach (var pair in I18nFontConfig.Config)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    textBox.font = pair.fontAsset;
                    textBox.fontSharedMaterial = pair.fontAsset.material;

                    foreach (var nameAndMaterial in pair.materials)
                    {
                        if (nameAndMaterial.name == materialName)
                        {
                            textBox.fontSharedMaterial = nameAndMaterial.material;
                            break;
                        }
                    }

                    break;
                }
            }

            needRefreshLineBreak = true;
        }

        public float fontSize
        {
            get => textBox.fontSize;
            set
            {
                if (Mathf.Approximately(textBox.fontSize, value)) return;
                textBox.fontSize = value;
                needRefreshLineBreak = true;
            }
        }

        private byte targetAlpha = 255;
        private float fadeValue = 1.0f;

        public void SetFade(byte targetAlpha, float fadeValue)
        {
            this.targetAlpha = targetAlpha;
            this.fadeValue = fadeValue;
            needRefreshFade = true;
        }

        private void OnEnable()
        {
            needRefreshLineBreak = true;
            needRefreshFade = true;
        }

        private void LateUpdate()
        {
            if (needRefreshLineBreak || needRefreshFade)
            {
                Refresh();
            }

            if (needRefreshAtNextFrame)
            {
                needRefreshAtNextFrame = false;
                needRefreshLineBreak = true;
                needRefreshFade = true;
            }
        }

        // If the last line is one Chinese character and some (or zero) Chinese punctuations,
        // and the second last line's last character is Chinese character,
        // then add a line break before the second last line's last character
        // TODO: advanced English hyphenation can be implemented here
        // Now we use Tools/Scenarios/add_soft_hyphens.py to pre-calculate hyphenation,
        // and Unity supports \u00ad as soft hyphen
        private void ApplyLineBreak(string text)
        {
            var textInfo = textBox.GetTextInfo(text);

            if (textInfo.lineCount >= 2)
            {
                var lineInfo = textInfo.lineInfo[textInfo.lineCount - 1];
                int firstIdx = lineInfo.firstCharacterIndex;

                bool needBreak = firstIdx >= 1 && IsChineseCharacter(text[firstIdx]);

                if (needBreak)
                {
                    int lastIdx = lineInfo.lastCharacterIndex;
                    for (int i = firstIdx + 1; i <= lastIdx; ++i)
                    {
                        if (!ChineseFollowingPunctuations.Contains(text[i]))
                        {
                            needBreak = false;
                            break;
                        }
                    }
                }

                if (needBreak)
                {
                    if (!IsChineseCharacter(text[firstIdx - 1]))
                    {
                        needBreak = false;
                    }
                }

                if (needBreak)
                {
                    text = text.Insert(firstIdx - 1, "\n");
                }
            }

            textBox.text = text;
        }

        public void SetTextAlpha(byte a)
        {
            textBox.color = Utils.SetAlpha32(textBox.color, a);
        }

        private void ApplyAlphaToCharAtIndex(int index, byte alpha)
        {
            var characterInfo = textBox.textInfo.characterInfo;
            // TODO: skip animation for invisible characters?
            if (!characterInfo[index].isVisible) return;

            // Characters at different indexes may have different materials
            var meshInfo = textBox.textInfo.meshInfo;
            var materialIndex = characterInfo[index].materialReferenceIndex;
            var newVertexColors = meshInfo[materialIndex].colors32;
            var vertexIndex = characterInfo[index].vertexIndex;
            newVertexColors[vertexIndex + 0].a = alpha;
            newVertexColors[vertexIndex + 1].a = alpha;
            newVertexColors[vertexIndex + 2].a = alpha;
            newVertexColors[vertexIndex + 3].a = alpha;
        }

        private void ApplyFade()
        {
            // due to some strange behaviour of TMP, manually check special case
            if (fadeValue >= 1.0f - 1e-3f)
            {
                SetTextAlpha(targetAlpha);
                return;
            }

            SetTextAlpha(0);

            var characterCount = textBox.textInfo.characterCount;
            var fadingCharacterIndex = Mathf.FloorToInt(characterCount * fadeValue);
            // handle fully visible characters
            for (var i = 0; i < fadingCharacterIndex; ++i)
            {
                ApplyAlphaToCharAtIndex(i, targetAlpha);
            }

            // handle fading character
            var tint = Mathf.Clamp01(characterCount * fadeValue - fadingCharacterIndex);
            var alpha = (byte)(targetAlpha * tint);
            ApplyAlphaToCharAtIndex(fadingCharacterIndex, alpha);

            // handle hidden characters
            for (var i = fadingCharacterIndex + 1; i < characterCount; i++)
            {
                ApplyAlphaToCharAtIndex(i, 0);
            }
        }

        private void Refresh()
        {
            if (!gameObject.activeInHierarchy)
            {
                textBox.text = text;
                return;
            }

            if (needRefreshLineBreak)
            {
                needRefreshLineBreak = false;
                if (string.IsNullOrEmpty(text))
                {
                    textBox.text = text;
                }
                else
                {
                    ApplyLineBreak(text);
                }
            }

            if (needRefreshFade)
            {
                needRefreshFade = false;
                ApplyFade();
            }

            if (needRefreshLineBreak)
            {
                textBox.ForceMeshUpdate();
            }
            else
            {
                textBox.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }
    }
}