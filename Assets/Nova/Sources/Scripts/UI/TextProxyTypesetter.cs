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

        private static readonly HashSet<char> EnglishChars = new HashSet<char>(
            "0123456789" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            " ([{<)]}>"
        );

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
        private static bool ScanAvoidOrphan(string text, TMP_TextInfo textInfo, int lineIdx)
        {
            if (lineIdx >= textInfo.lineCount - 1)
            {
                return false;
            }

            var nextLineInfo = textInfo.lineInfo[lineIdx + 1];
            if (nextLineInfo.characterCount < 1)
            {
                return false;
            }

            var characterInfos = textInfo.characterInfo;
            // characterInfo.index is the index in the original text with XML tags
            var nextLineFirstIdx = characterInfos[nextLineInfo.firstCharacterIndex].index;
            if (!IsChineseCharacter(text[nextLineFirstIdx]))
            {
                return false;
            }

            if (lineIdx < textInfo.lineCount - 2)
            {
                var nextLineLastIdx = characterInfos[nextLineInfo.lastCharacterIndex].index;
                if (text[nextLineLastIdx] != '\n')
                {
                    return false;
                }
            }

            var nextLineLastCharIdx = nextLineInfo.lastCharacterIndex;
            for (var charIdx = nextLineInfo.firstCharacterIndex + 1; charIdx <= nextLineLastCharIdx; ++charIdx)
            {
                var idx = characterInfos[charIdx].index;
                if (!ChineseFollowingPunctuations.Contains(text[idx]))
                {
                    return false;
                }
            }

            var lineInfo = textInfo.lineInfo[lineIdx];
            if (lineInfo.characterCount < 3)
            {
                return false;
            }

            var lastIdx = characterInfos[lineInfo.lastCharacterIndex].index;
            if (text[lastIdx] == '\v')
            {
                lastIdx = characterInfos[lineInfo.lastCharacterIndex - 1].index;
            }

            // If the line ends with hard line break, text[lastIdx] will be \n
            if (!IsChineseCharacter(text[lastIdx]))
            {
                return false;
            }

            return true;
        }

        private static void ScanKern(string text, TMP_CharacterInfo[] characterInfos, TMP_LineInfo lineInfo,
            List<int> idxs, List<float> kerns, out int flexibleWidthCount, out int englishCharCount)
        {
            const float chinesePunctuationKerning = -0.5f;
            const float chinesePunctuationSubKerning = -0.3333f;

            idxs.Clear();
            kerns.Clear();
            flexibleWidthCount = 0;
            englishCharCount = 0;

            if (lineInfo.characterCount < 2)
            {
                return;
            }

            var firstCharIdx = lineInfo.firstCharacterIndex;
            var lastCharIdx = lineInfo.lastCharacterIndex;

            // From right to left
            var charIdx = lastCharIdx;
            var leftIdx = characterInfos[charIdx].index;
            var leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
            var leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
            var leftChinese = IsChineseCharacter(text[leftIdx]);
            var leftEnglish = EnglishChars.Contains(text[leftIdx]);
            englishCharCount += leftEnglish ? 1 : 0;
            while (charIdx > firstCharIdx)
            {
                var rightIdx = leftIdx;
                var rightOpen = leftOpen;
                var rightClose = leftClose;
                var rightChinese = leftChinese;
                var rightEnglish = leftEnglish;
                --charIdx;
                leftIdx = characterInfos[charIdx].index;
                leftOpen = ChineseOpeningPunctuations.Contains(text[leftIdx]);
                leftClose = ChineseClosingPunctuations.Contains(text[leftIdx]);
                leftChinese = IsChineseCharacter(text[leftIdx]);
                leftEnglish = EnglishChars.Contains(text[leftIdx]);
                englishCharCount += leftEnglish ? 1 : 0;
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
                else if ((leftChinese && rightOpen) || (leftClose && rightChinese) ||
                         (leftEnglish && rightOpen) || (leftClose && rightEnglish) ||
                         (leftEnglish && rightClose) || (leftOpen && rightEnglish) ||
                         (leftChinese && rightEnglish) || (leftEnglish && rightChinese))
                {
                    // Add flexible width
                    // TODO: Only positive width between Chinese and English
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
            TMP_CharacterInfo[] characterInfos, TMP_LineInfo lineInfo, int flexibleWidthCount, float extraWidth,
            out float endMargin, out bool canAvoidOrphan)
        {
            endMargin = 0f;
            canAvoidOrphan = true;

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
                    // Allow any width when avoiding orphan
                }
                else
                {
                    // Justified line may be already compressed by 1em
                    if (flexibleWidth > extraWidth + 1f)
                    {
                        canAvoidOrphan = false;
                        return 0f;
                    }
                }
            }
            else
            {
                if (flexibleWidth > extraWidth)
                {
                    canAvoidOrphan = false;
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
        private static void ApplyKerningLine(TMP_Text textBox, RectTransform rectTransform, ref string text,
            ref TMP_TextInfo textInfo, int lineIdx, List<int> idxs, List<float> kerns, out bool canAvoidOrphan)
        {
            var characterInfos = textInfo.characterInfo;
            var lineInfo = textInfo.lineInfo[lineIdx];
            ScanKern(text, characterInfos, lineInfo, idxs, kerns, out var flexibleWidthCount, out var englishCharCount);

            var flexibleWidth = 0f;
            var endMargin = 0f;
            canAvoidOrphan = false;
            if (lineIdx < textInfo.lineCount - 1)
            {
                flexibleWidth = GetFlexibleWidth(textBox, rectTransform, text, characterInfos, lineInfo,
                    flexibleWidthCount, (float)englishCharCount, out endMargin, out canAvoidOrphan);
            }

            var dirty = false;

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
                    lastIdx += 1;
                    text = text.Insert(lastIdx, "</align>\v");
                }

                if (endMargin != 0f)
                {
                    idxs.Insert(0, lastIdx);
                    kerns.Insert(0, endMargin);
                    // Prevent ignoring kern at line end
                    text = text.Insert(lastIdx, "\u00A0");
                }

                dirty = true;
            }

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

        // Visible character count is unchanged when typesetting
        private static int GetVisibleCharCountUntil(TMP_TextInfo textInfo, int lineIdx)
        {
            var count = 0;
            for (var i = 0; i <= lineIdx; ++i)
            {
                count += textInfo.lineInfo[i].visibleCharacterCount;
            }

            return count;
        }

        private static int GetIdxForVisibleChar(TMP_TextInfo textInfo, int count)
        {
            var characterInfos = textInfo.characterInfo;
            for (var i = 0; i < textInfo.lineCount; ++i)
            {
                var lineInfo = textInfo.lineInfo[i];
                if (count > lineInfo.visibleCharacterCount)
                {
                    count -= lineInfo.visibleCharacterCount;
                }
                else if (count == lineInfo.visibleCharacterCount)
                {
                    return characterInfos[lineInfo.lastVisibleCharacterIndex].index;
                }
                else
                {
                    var lastCharIdx = lineInfo.lastVisibleCharacterIndex;
                    for (var charIdx = lineInfo.firstVisibleCharacterIndex; charIdx < lastCharIdx; ++charIdx)
                    {
                        var charInfo = characterInfos[charIdx];
                        if (!charInfo.isVisible)
                        {
                            continue;
                        }

                        --count;
                        if (count == 0)
                        {
                            return charInfo.index;
                        }
                    }
                }
            }

            // Should not happen
            return -1;
        }

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
                var oldText = text;
                ApplyKerningLine(textBox, rectTransform, ref text, ref textInfo, lineIdx, idxs, kerns,
                    out var canAvoidOrphan);
                if (!canAvoidOrphan)
                {
                    continue;
                }

                canAvoidOrphan = ScanAvoidOrphan(text, textInfo, lineIdx);
                if (!canAvoidOrphan)
                {
                    continue;
                }

                var count = GetVisibleCharCountUntil(textInfo, lineIdx);

                // Rollback
                // There may be bugs if we save oldTextInfo
                text = oldText;
                textInfo = textBox.GetTextInfo(text);

                var idx = GetIdxForVisibleChar(textInfo, count);
                text = text.Insert(idx, "\v");
                textInfo = textBox.GetTextInfo(text);
                ApplyKerningLine(textBox, rectTransform, ref text, ref textInfo, lineIdx, idxs, kerns,
                    out canAvoidOrphan);
            }
        }
    }
}
