using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    // Add extra features to TMP_Text
    [RequireComponent(typeof(TMP_Text))]
    public class TextProxy : UIBehaviour
    {
        private TMP_Text textBox;
        private RectTransform rectTransform;

        private bool inited;
        private bool needRefreshLineBreak;
        private bool needRefreshFade;

        // TODO: Is this deprecated?
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

            textBox.OnPreRenderText += ApplyFade;

            inited = true;
        }

        protected override void OnDestroy()
        {
            textBox.OnPreRenderText -= ApplyFade;
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

        #region Font material

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
                    if (textBox.fontSharedMaterial.name != pair.fontAsset.material.name &&
                        !pair.materials.Values.Select(x => x.name).Contains(textBox.fontSharedMaterial.name))
                    {
                        Debug.LogWarning(
                            $"Nova：Font material {textBox.font}:{textBox.fontSharedMaterial} not in I18nFontConfig, " +
                            $"found in {Utils.GetPath(this)}");
                    }

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
                    if (!string.IsNullOrEmpty(materialName) &&
                        pair.materials.TryGetValue(materialName, out var material))
                    {
                        textBox.fontSharedMaterial = material;
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

        #endregion

        public float fontSize
        {
            get => textBox.fontSize;
            set
            {
                if (Mathf.Abs(value - textBox.fontSize) < 1e-6f) return;
                textBox.fontSize = value;
                needRefreshLineBreak = true;
            }
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

        #region Line break and kerning

        private const string ChinesePunctuationKerning = "<space=-0.5em>";
        private const string ChinesePunctuationSubKerning = "<space=-0.33em>";

        private static readonly HashSet<char> ChineseOpeningPunctuations = new HashSet<char>("‘“（【《");
        private static readonly HashSet<char> ChineseClosingPunctuations = new HashSet<char>("，。、；：？！’”）】》");
        private static readonly HashSet<char> ChineseMiddlePunctuations = new HashSet<char>("…—·");

        private static readonly HashSet<char> ChineseFollowingPunctuations = new HashSet<char>(
            ChineseOpeningPunctuations.Concat(ChineseClosingPunctuations).Concat(ChineseMiddlePunctuations));

        private static bool IsChineseCharacter(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        // If the last line is one Chinese character and some (or zero) Chinese punctuations,
        // and the second last line's last character is Chinese character,
        // then add a line break before the second last line's last character
        // No need to update textInfo
        private void ApplyLineBreak(ref string text, TMP_TextInfo textInfo)
        {
            if (textInfo.lineCount < 2)
            {
                return;
            }

            var secondLineInfo = textInfo.lineInfo[textInfo.lineCount - 2];
            if (secondLineInfo.characterCount < 3)
            {
                return;
            }

            var lineInfo = textInfo.lineInfo[textInfo.lineCount - 1];
            int firstCharIdx = lineInfo.firstCharacterIndex;
            var characterInfos = textInfo.characterInfo;
            // characterInfo.index is the index in the original text with XML tags
            int firstIdx = characterInfos[firstCharIdx].index;
            if (firstIdx < 1 || !IsChineseCharacter(text[firstIdx]))
            {
                return;
            }

            int lastCharIdx = lineInfo.lastCharacterIndex;
            for (int charIdx = firstCharIdx + 1; charIdx <= lastCharIdx; ++charIdx)
            {
                var idx = characterInfos[charIdx].index;
                if (!ChineseFollowingPunctuations.Contains(text[idx]))
                {
                    return;
                }
            }

            int secondLineLastIdx = characterInfos[secondLineInfo.lastCharacterIndex].index;
            if (!IsChineseCharacter(text[secondLineLastIdx]))
            {
                return;
            }

            if (textInfo.lineCount > 2 && ((int)textBox.alignment & (int)HorizontalAlignmentOptions.Justified) > 0)
            {
                // Justify the second last line
                int secondLineFirstIdx = characterInfos[secondLineInfo.firstCharacterIndex].index;
                text = text.Insert(secondLineLastIdx, "</align>\v");
                text = text.Insert(secondLineFirstIdx, "<align=\"flush\">");
            }
            else
            {
                text = text.Insert(secondLineLastIdx, "\v");
            }
        }

        // Add a negative space between each punctuation pair,
        // and before each opening punctuation at line beginning if left aligned
        private void ApplyKerning(ref string text, ref TMP_TextInfo textInfo)
        {
            var characterInfos = textInfo.characterInfo;
            bool dirty = false;

            // Each line is updated only once. Even if some character is pulled to the previous line and forms a
            // punctuation pair, the previous line will not be updated again
            // That's also why we don't apply kerning at line ending
            for (int lineIdx = 0; lineIdx < textInfo.lineCount; ++lineIdx)
            {
                var lineInfo = textInfo.lineInfo[lineIdx];
                int firstCharIdx = lineInfo.firstCharacterIndex;

                int charIdx = lineInfo.lastCharacterIndex;
                int leftIdx = characterInfos[charIdx].index;
                bool leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
                bool leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
                for (; charIdx > firstCharIdx; --charIdx)
                {
                    int rightIdx = leftIdx;
                    bool rightOpen = leftOpen;
                    bool rightClose = leftClose;
                    leftIdx = characterInfos[charIdx - 1].index;
                    leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
                    leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
                    if (leftClose && rightOpen)
                    {
                        text = text.Insert(rightIdx, ChinesePunctuationKerning);
                        dirty = true;
                    }
                    else if ((leftOpen && rightOpen) || (leftClose && rightClose))
                    {
                        text = text.Insert(rightIdx, ChinesePunctuationSubKerning);
                        dirty = true;
                    }
                }

                bool isLeftAligned = ((int)lineInfo.alignment &
                                      ((int)HorizontalAlignmentOptions.Left |
                                       (int)HorizontalAlignmentOptions.Justified)) > 0;
                if (isLeftAligned)
                {
                    int firstIdx = characterInfos[firstCharIdx].index;
                    if (ChineseOpeningPunctuations.Contains(text[firstIdx]))
                    {
                        text = text.Insert(firstIdx, ChinesePunctuationKerning);
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    textInfo = textBox.GetTextInfo(text);
                    characterInfos = textInfo.characterInfo;
                    dirty = false;
                }
            }
        }

        // TODO: advanced English hyphenation can be implemented here
        // Now we use Tools/Scenarios/add_soft_hyphens.py to pre-calculate hyphenation,
        // and Unity supports \u00ad as soft hyphen
        private string Typeset(string text)
        {
            if (I18n.CurrentLocale == SystemLanguage.ChineseSimplified)
            {
                var textInfo = textBox.GetTextInfo(text);
                ApplyKerning(ref text, ref textInfo);
                ApplyLineBreak(ref text, textInfo);
            }

            return text;
        }

        public float GetPreferredHeight(string text, float width)
        {
            text = Typeset(text);
            var height = textBox.GetPreferredValues(text, width, 0f).y;
            return height;
        }

        #endregion

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

        public float GetFirstCharacterCenterY()
        {
            var textInfo = textBox.textInfo;
            if (textInfo.characterCount <= 0)
            {
                return 0f;
            }

            var lineInfo = textInfo.lineInfo[0];
            var characterInfo = textInfo.characterInfo[0];
            var y = -lineInfo.ascender + (characterInfo.ascender + characterInfo.descender) / 2;
            return y;
        }

        #region Fade

        private bool canFade;
        private float fadeValue = 1.0f;

        public void SetFade(float fadeValue)
        {
            canFade = true;
            this.fadeValue = Mathf.Clamp01(fadeValue);
            needRefreshFade = true;
        }

        private static void ApplyAlphaToCharAtIndex(TMP_TextInfo textInfo, int index, byte alpha)
        {
            var characterInfos = textInfo.characterInfo;
            // Boundary check in case characterInfo.Length is wrong
            if (characterInfos.Length <= index) return;

            var characterInfo = characterInfos[index];
            if (!characterInfo.isVisible) return;

            // Characters at different indices may have different materials
            var meshInfos = textInfo.meshInfo;
            var materialIndex = characterInfo.materialReferenceIndex;
            var vertexColors = meshInfos[materialIndex].colors32;
            var vertexIndex = characterInfo.vertexIndex;
            for (var i = 0; i < 4; ++i)
            {
                vertexColors[vertexIndex + i].a = alpha;
            }
        }

        private void ApplyFade(TMP_TextInfo textInfo)
        {
            if (!canFade || textInfo.characterCount == 0)
            {
                return;
            }

            int beginIdx = 0;
            int endIdx = 0;
            if (textInfo.pageCount > 1)
            {
                var pageInfo = textInfo.pageInfo[textBox.pageToDisplay - 1];
                if (pageInfo.lastCharacterIndex > 0)
                {
                    beginIdx = pageInfo.firstCharacterIndex;
                    endIdx = pageInfo.lastCharacterIndex + 1;
                }
            }

            if (endIdx == 0)
            {
                endIdx = textInfo.characterCount;
            }

            var floatFadingIdx = beginIdx + fadeValue * (endIdx - beginIdx);
            var fadingIdx = Mathf.FloorToInt(floatFadingIdx);
            var alpha = textBox.color.a;

            // handle fully revealed characters
            for (var i = beginIdx; i < fadingIdx; ++i)
            {
                ApplyAlphaToCharAtIndex(textInfo, i, (byte)(255 * alpha));
            }

            if (fadingIdx == endIdx)
            {
                return;
            }

            // handle fading character
            var tint = Mathf.Clamp01(floatFadingIdx - fadingIdx);
            ApplyAlphaToCharAtIndex(textInfo, fadingIdx, (byte)(255 * tint * alpha));

            // handle hidden characters
            for (var i = fadingIdx + 1; i < endIdx; ++i)
            {
                ApplyAlphaToCharAtIndex(textInfo, i, 0);
            }
        }

        #endregion

        private void Refresh()
        {
            if (!needRefreshLineBreak && !needRefreshFade)
            {
                return;
            }

            if (needRefreshLineBreak)
            {
                textBox.text = Typeset(text);
            }

            if (needRefreshFade)
            {
                ApplyFade(textBox.textInfo);
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
