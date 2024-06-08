using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nova
{
    public static class TextProxyTypesetter
    {
        private static readonly HashSet<char> ChineseOpeningPunctuations = new HashSet<char>("‘“（【《");
        private static readonly HashSet<char> ChineseClosingPunctuations = new HashSet<char>("，。、；：？！’”）】》");
        private static readonly HashSet<char> ChineseMiddlePunctuations = new HashSet<char>("….·—⸺");

        private static readonly HashSet<char> ChineseFollowingPunctuations = new HashSet<char>(
            ChineseOpeningPunctuations.Concat(ChineseClosingPunctuations).Concat(ChineseMiddlePunctuations));

        private static bool IsChineseCharacter(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        private static bool IsJustified(int alignment)
        {
            return (alignment & (int)HorizontalAlignmentOptions.Justified) > 0;
        }

        private static bool IsLeftAligned(int alignment)
        {
            return (alignment & ((int)HorizontalAlignmentOptions.Left | (int)HorizontalAlignmentOptions.Justified)) > 0;
        }

        // If the last line is one Chinese character and some (or zero) Chinese punctuations,
        // and the second last line's last character is Chinese character,
        // then add a line break before the second last line's last character
        // No need to update textInfo
        private static void AvoidOrphan(TMP_Text textBox, ref string text, ref TMP_TextInfo textInfo, int lineIdx)
        {
            if (lineIdx >= textInfo.lineCount - 1)
            {
                return;
            }

            var lineInfo = textInfo.lineInfo[lineIdx];
            if (lineInfo.characterCount < 3)
            {
                return;
            }

            var characterInfos = textInfo.characterInfo;
            // characterInfo.index is the index in the original text with XML tags
            var lastIdx = characterInfos[lineInfo.lastCharacterIndex].index;
            // If the line ends with hard line break, text[lastIdx] will be \n
            if (!IsChineseCharacter(text[lastIdx]))
            {
                return;
            }

            var nextLineInfo = textInfo.lineInfo[lineIdx + 1];
            var nextLineFirstIdx = characterInfos[nextLineInfo.firstCharacterIndex].index;
            if (!IsChineseCharacter(text[nextLineFirstIdx]))
            {
                return;
            }

            if (lineIdx < textInfo.lineCount - 2)
            {
                var nextLineLastIdx = characterInfos[nextLineInfo.lastCharacterIndex].index;
                if (text[nextLineLastIdx] != '\n')
                {
                    return;
                }
            }

            var nextLineLastCharIdx = nextLineInfo.lastCharacterIndex;
            for (var charIdx = nextLineInfo.firstCharacterIndex + 1; charIdx <= nextLineLastCharIdx; ++charIdx)
            {
                var idx = characterInfos[charIdx].index;
                if (!ChineseFollowingPunctuations.Contains(text[idx]))
                {
                    return;
                }
            }

            text = text.Insert(lastIdx, "\v");
            textInfo = textBox.GetTextInfo(text);
        }

        private static void ScanKern(string text, TMP_CharacterInfo[] characterInfos, TMP_LineInfo lineInfo,
            List<int> idxs, List<float> kerns, out int flexibleWidthCount)
        {
            const float chinesePunctuationKerning = -0.5f;
            const float chinesePunctuationSubKerning = -0.3333f;

            var firstCharIdx = lineInfo.firstCharacterIndex;
            var lastCharIdx = lineInfo.lastCharacterIndex;
            idxs.Clear();
            kerns.Clear();
            flexibleWidthCount = 0;

            // From right to left
            var charIdx = lastCharIdx;
            var leftIdx = characterInfos[charIdx].index;
            var leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
            var leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
            var leftChinese = IsChineseCharacter(text[leftIdx]);
            while (charIdx > firstCharIdx)
            {
                var rightIdx = leftIdx;
                var rightOpen = leftOpen;
                var rightClose = leftClose;
                var rightChinese = leftChinese;
                --charIdx;
                leftIdx = characterInfos[charIdx].index;
                leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
                leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
                leftChinese = IsChineseCharacter(text[leftIdx]);
                if (leftClose && rightOpen)
                {
                    idxs.Add(rightIdx);
                    kerns.Add(chinesePunctuationKerning);
                }
                else if ((leftOpen && rightOpen) || (leftClose && rightClose))
                {
                    idxs.Add(rightIdx);
                    kerns.Add(chinesePunctuationSubKerning);
                }
                else if ((leftChinese && rightOpen) || (leftClose && rightChinese))
                {
                    // Add flexible width
                    idxs.Add(rightIdx);
                    kerns.Add(0f);
                    ++flexibleWidthCount;
                }
            }

            if (IsLeftAligned((int)lineInfo.alignment) && leftOpen)
            {
                idxs.Add(leftIdx);
                kerns.Add(chinesePunctuationKerning);
            }
        }

        private static float GetFlexibleWidth(TMP_Text textBox, RectTransform rectTransform, string text,
            TMP_CharacterInfo[] characterInfos, TMP_LineInfo lineInfo, int flexibleWidthCount, out float endMargin)
        {
            endMargin = 0f;
            if (flexibleWidthCount <= 0)
            {
                return 0f;
            }

            var lastIdx = characterInfos[lineInfo.lastCharacterIndex].index;
            if (text[lastIdx] == '\n')
            {
                // No need to add flexible width when end with hard line break
                return 0f;
            }

            var flexibleWidth = (rectTransform.rect.width - lineInfo.length) / textBox.fontSize;

            if (flexibleWidth < -0.5f)
            {
                return 0f;
            }

            if (IsJustified((int)lineInfo.alignment))
            {
                if (text[lastIdx] == '\v')
                {
                    // Allow more width if avoiding orphan
                    if (flexibleWidth > 2f)
                    {
                        return 0f;
                    }
                }
                else
                {
                    // Justified line may be already compressed by 1em
                    if (flexibleWidth > 1f)
                    {
                        return 0f;
                    }
                }
            }
            else
            {
                if (flexibleWidth > 0f)
                {
                    return 0f;
                }
            }

            var totalFlexibleWidth = flexibleWidth;
            // Evenly distribute flexible width on punctuations, and add 1 for flexible character spacing
            flexibleWidth /= flexibleWidthCount + 1;
            if (flexibleWidth > 0.5f)
            {
                flexibleWidth = 0.5f;
                endMargin = totalFlexibleWidth - (flexibleWidthCount + 1) * flexibleWidth;
            }

            // Round toward zero
            flexibleWidth = Mathf.Sign(flexibleWidth) * Mathf.Floor(Mathf.Abs(flexibleWidth) * 1e4f) / 1e4f;
            return flexibleWidth;
        }

        private static readonly Dictionary<float, string> KernStrCache = new Dictionary<float, string>();

        private static string GetKernStr(float kern)
        {
            if (KernStrCache.TryGetValue(kern, out var str))
            {
                return str;
            }

            str = $"<space={kern:F4}em>";
            KernStrCache[kern] = str;
            return str;
        }

        // Add a negative space between each punctuation pair,
        // and before each opening punctuation at line beginning if left aligned
        public static void ApplyKerning(TMP_Text textBox, RectTransform rectTransform, ref string text,
            ref TMP_TextInfo textInfo)
        {
            var idxs = new List<int>();
            var kerns = new List<float>();

            // Each line is updated only once. Even if some character is pulled to the previous line and forms a
            // punctuation pair, the previous line will not be updated again
            // That's also why we don't apply kerning at line ending
            for (var lineIdx = 0; lineIdx < textInfo.lineCount; ++lineIdx)
            {
                AvoidOrphan(textBox, ref text, ref textInfo, lineIdx);

                var characterInfos = textInfo.characterInfo;
                var lineInfo = textInfo.lineInfo[lineIdx];
                ScanKern(text, characterInfos, lineInfo, idxs, kerns, out var flexibleWidthCount);

                var flexibleWidth = GetFlexibleWidth(textBox, rectTransform, text, characterInfos, lineInfo,
                    flexibleWidthCount, out var endMargin);

                // Exact float zero
                var needFlush = flexibleWidth != 0f && IsLeftAligned((int)lineInfo.alignment);
                if (needFlush)
                {
                    var lastIdx = characterInfos[lineInfo.lastCharacterIndex].index;
                    if (text[lastIdx] == '\v')
                    {
                        text = text.Insert(lastIdx, "</align>");
                    }
                    else
                    {
                        text = text.Insert(lastIdx + 1, "</align>\v");
                    }

                    if (endMargin != 0f)
                    {
                        idxs.Insert(0, lastIdx);
                        kerns.Insert(0, endMargin);
                        // Prevent ignoring kern at line end
                        text = text.Insert(lastIdx, " ");
                    }
                }

                var dirty = false;
                for (var i = 0; i < idxs.Count; ++i)
                {
                    var kern = kerns[i];
                    // Exact float zero
                    if (kern == 0f)
                    {
                        kern = flexibleWidth;
                    }

                    if (kern != 0f)
                    {
                        text = text.Insert(idxs[i], GetKernStr(kern));
                        dirty = true;
                    }
                }

                if (needFlush)
                {
                    var firstIdx = characterInfos[lineInfo.firstCharacterIndex].index;
                    text = text.Insert(firstIdx, "<align=\"flush\">");
                }

                if (dirty)
                {
                    textInfo = textBox.GetTextInfo(text);
                }
            }
        }
    }
}
