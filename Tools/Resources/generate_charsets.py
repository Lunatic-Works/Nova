#!/usr/bin/env python3

import re
import unicodedata
from glob import glob

in_patterns = [
    "../../Assets/Nova/Fonts/CharsetChinese.txt",
    "../../Assets/Resources/Scenarios/*.txt",
    "../../Assets/Resources/LocalizedResources/*/Scenarios/*.txt",
    "../../Assets/Resources/LocalizedStrings/*.json",
]
out_filename = "../../Assets/Nova/Fonts/Charset.txt"
out_bold_filename = "../../Assets/Nova/Fonts/CharsetBold.txt"


def dedup(s, ignored_chars="\n"):
    return "".join(sorted(c for c in set(s) if c not in ignored_chars))


def check_control_char(s):
    for c in s:
        if unicodedata.category(c)[0] == "C":
            print(f"Special character: U+{ord(c):04X}")


def check_nfc(s):
    s_nfc = unicodedata.normalize("NFC", s)
    if s_nfc != s:
        s_set = set(s)
        s_nfc_set = set(s_nfc)
        diff = sorted(c for c in s_set if c not in s_nfc_set)
        for c in diff:
            print(f"Non-NFC character: U+{ord(c):04X}")


def main():
    with open(out_filename, "r", encoding="utf-8") as f:
        old_text = f.read().strip("\n")
    with open(out_bold_filename, "r", encoding="utf-8") as f:
        old_text_bold = f.read().strip("\n")

    text = ""
    for pattern in in_patterns:
        for filename in glob(pattern):
            print(filename)
            with open(filename, "r", encoding="utf-8") as f:
                s = f.read()
            s = dedup(s)
            check_control_char(s)
            check_nfc(s)
            text += s

    print("-" * 80)

    text_bold = "".join(re.compile("<b>(.*?)</b>").findall(text))

    text = dedup(text)
    text_bold = dedup(text_bold)

    if all(x in old_text for x in text):
        print("Need to rebuild font asset: NO")
    else:
        print("Need to rebuild font asset: YES")
        with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)

    if all(x in old_text_bold for x in text_bold):
        print("Need to rebuild bold font asset: NO")
    else:
        print("Need to rebuild bold font asset: YES")
        with open(out_bold_filename, "w", encoding="utf-8", newline="\n") as f:
            f.write(text_bold)


if __name__ == "__main__":
    main()
