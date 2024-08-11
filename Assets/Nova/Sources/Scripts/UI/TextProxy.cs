using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nova
{
    // Add extra features to TMP_Text
    [RequireComponent(typeof(TMP_Text))]
    public class TextProxy : UIBehaviour
    {
        [SerializeField] private bool disableTypeset;
        private bool lastDisableTypeset;

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
            fontSize = textBox.fontSize;

            // Use character spacing almost only if no word spacing
            textBox.wordWrappingRatios = 0.999f;

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
                needRefreshLineBreak = true;
                Refresh();
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

        private float _fontSize;

        public float fontSize
        {
            get => _fontSize;
            set
            {
                if (Mathf.Abs(value - _fontSize) < 1e-6f) return;
                _fontSize = value;
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
            if (lastDisableTypeset != disableTypeset)
            {
                lastDisableTypeset = disableTypeset;
                needRefreshLineBreak = true;
            }

            Refresh();
        }

        // TODO: advanced English hyphenation can be implemented here
        // Now we use Tools/Scenarios/add_soft_hyphens.py to pre-calculate hyphenation,
        // and Unity supports \u00AD as soft hyphen
        private string Typeset(string text)
        {
            if (I18n.CurrentLocale == SystemLanguage.ChineseSimplified)
            {
                // Justified alignment for Chinese and left alignment for English
                if (textBox.alignment == TextAlignmentOptions.TopLeft)
                {
                    textBox.alignment = TextAlignmentOptions.TopJustified;
                }

                if (!disableTypeset)
                {
                    var textInfo = textBox.GetTextInfo(text);
                    TextProxyTypesetter.ApplyKerning(textBox, rectTransform, ref text, ref textInfo);
                }
            }
            else
            {
                if (textBox.alignment == TextAlignmentOptions.TopJustified)
                {
                    textBox.alignment = TextAlignmentOptions.TopLeft;
                }
            }

            return text;
        }

        public float GetPreferredHeight(string text, float width)
        {
            text = Typeset(text);
            var height = textBox.GetPreferredValues(text, width, 0f).y;
            return height;
        }

        private static readonly Regex XmlPattern = new Regex(@"<[^\n>]+>", RegexOptions.Compiled);

        private static string RemoveXml(string s)
        {
            return XmlPattern.Replace(s, "");
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

            return RemoveXml(textBox.text).Length;
        }

        public float GetFirstCharacterCenterY()
        {
            var textInfo = textBox.textInfo;
            if (textInfo.lineCount <= 0)
            {
                return 0f;
            }

            var lineInfo = textInfo.lineInfo[0];
            if (lineInfo.characterCount <= 0)
            {
                return 0f;
            }

            var characterInfo = textInfo.characterInfo[lineInfo.firstCharacterIndex];
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
                // Update fontSize before Typeset
                textBox.fontSize = fontSize;
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

        public void ForceRefresh()
        {
            Init();
            Refresh();
        }
    }
}
