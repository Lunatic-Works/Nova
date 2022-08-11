using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextProxy : UIBehaviour
    {
        private static readonly HashSet<char> ChineseFollowingPunctuations = new HashSet<char>("，。、；：？！…—‘’“”（）【】《》");

        private static bool IsChineseCharacter(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        private TMP_Text textBox;
        private RectTransform rectTransform;

        private bool inited;
        private bool needRefreshLineBreak;
        private bool needRefreshFade;

        [HideInInspector] public bool canRefreshLineBreak = true;

        protected override void Awake()
        {
            Init();
        }

        // Awake() may not be called early enough
        public void Init()
        {
            if (inited) return;
            textBox = GetComponent<TMP_Text>();
            rectTransform = GetComponent<RectTransform>();
            inited = true;
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

        public void CheckFontInConfig()
        {
            foreach (var pair in I18nFontConfig.Config)
            {
                if (textBox.font == pair.fontAsset)
                {
                    return;
                }
            }

            Debug.LogWarning($"Nova：Font asset {textBox.font} not in I18nFontConfig, found in {Utils.GetPath(this)}");
        }

        public void UpdateFont()
        {
            foreach (var pair in I18nFontConfig.Config)
            {
                if (pair.locale == I18n.CurrentLocale)
                {
                    textBox.font = pair.fontAsset;
                    if (!string.IsNullOrEmpty(materialName) && pair.materials.ContainsKey(materialName))
                    {
                        textBox.fontSharedMaterial = pair.materials[materialName];
                    }
                    else
                    {
                        textBox.fontSharedMaterial = pair.fontAsset.material;
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

        protected override void OnEnable()
        {
            needRefreshLineBreak = true;
            needRefreshFade = true;
        }

        private float lastWidth;
        private float lastHeight;

        protected override void OnRectTransformDimensionsChange()
        {
            Init();
            var rect = rectTransform.rect;
            if (Mathf.Abs(rect.width - lastWidth) < 0.01f && Mathf.Abs(rect.height - lastHeight) < 0.01f)
            {
                return;
            }

            lastWidth = rect.width;
            lastHeight = rect.height;
            needRefreshLineBreak = true;
        }

        private void LateUpdate()
        {
            Refresh();
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
                var characterInfos = textInfo.characterInfo;
                // characterInfo.index is the index in the original text with XML tags
                int firstIdx = characterInfos[lineInfo.firstCharacterIndex].index;

                bool needBreak = firstIdx >= 1 && IsChineseCharacter(text[firstIdx]);

                if (needBreak)
                {
                    int lastIdx = characterInfos[lineInfo.lastCharacterIndex].index;
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

        // Character count of the parsed text without XML tags
        // TODO: sometimes textInfo.characterCount or pageInfo.lastCharacterIndex returns 0, which may be a bug of TMP
        // In this case, we need to remove XML tags when counting the text length
        public int GetPageCharacterCount()
        {
            var textInfo = textBox.textInfo;
            if (textInfo.pageCount > 1)
            {
                var pageInfo = textInfo.pageInfo[textBox.pageToDisplay - 1];
                if (pageInfo.lastCharacterIndex > 0)
                {
                    return pageInfo.lastCharacterIndex - pageInfo.firstCharacterIndex + 1;
                }
            }

            if (textInfo.characterCount > 0)
            {
                return textInfo.characterCount;
            }

            return textBox.text.Length;
        }

        public void SetTextAlpha(byte a)
        {
            textBox.color = Utils.SetAlpha32(textBox.color, a);
        }

        private void ApplyAlphaToCharAtIndex(int index, byte alpha)
        {
            var characterInfo = textBox.textInfo.characterInfo;
            // Boundary check in case characterInfo.Length is wrong
            if (characterInfo.Length <= index) return;
            // TODO: skip animation for invisible characters? <- Boundary Check Applied
            if (!characterInfo[index].isVisible) return;

            // Characters at different indices may have different materials
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

            var characterCount = GetPageCharacterCount();
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
            if (!needRefreshLineBreak && !needRefreshFade)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                textBox.text = text;
                return;
            }

            if (needRefreshLineBreak)
            {
                if (string.IsNullOrEmpty(text))
                {
                    // TODO: the text may not update if we set an empty string. It may be a bug of TMP.
                    textBox.text = " ";
                }
                else
                {
                    ApplyLineBreak(text);
                }
            }

            if (needRefreshFade)
            {
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

            needRefreshLineBreak = false;
            needRefreshFade = false;
        }
    }
}
