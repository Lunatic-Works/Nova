#!/usr/bin/env python3

import os
import re
import unicodedata

from lua_parser import is_nil, walk_functions
from nova_script_parser import normalize_dialogue, parse_chapters

in_dir = "../../Assets/Resources/Scenarios/"
template_filename = "template.txt"


def print_warn(line_num, message):
    print(f"\033[93mLine {line_num}: {message}\033[0m")


def is_special_char(c):
    return c != "\n" and unicodedata.category(c)[0] == "C"


def check_code(code, line_num, anim_hold_tracked):
    for c in code:
        if is_special_char(c):
            print_warn(line_num, f"special character U+{ord(c):04X} in code")

    if "TODO" in code:
        print_warn(line_num, "TODO in code")

    check_anim_hold_override = False
    check_show = False
    check_trans = False
    wait_time = 0
    try:
        for func_name, args, env in walk_functions(code):
            for name in (func_name,) + args:
                if name == "anim_hold_begin":
                    if anim_hold_tracked:
                        print_warn(line_num, "anim_hold_begin() not match")
                    else:
                        anim_hold_tracked = True

                    if env:
                        check_anim_hold_override = True

                    if "anim_hold" in env:
                        print_warn(line_num, "anim_hold_begin() in anim_hold")

                elif name == "anim_hold_end":
                    if anim_hold_tracked:
                        anim_hold_tracked = False
                    else:
                        print_warn(line_num, "anim_hold_end() not match")

                    if env:
                        check_anim_hold_override = True

                    if "anim_hold" in env:
                        print_warn(line_num, "anim_hold_end() in anim_hold")

            if func_name == "anim":
                wait_time = 0

                if env:
                    print_warn(line_num, "anim in anon function")

            elif func_name == "anim_hold":
                wait_time = 0

                if not anim_hold_tracked:
                    print_warn(line_num, "anim_hold not tracked")

                if check_anim_hold_override and not env:
                    print_warn(
                        line_num,
                        "anim_hold overridden by anim_hold_begin() or anim_hold_end()",
                    )

                if "anim_hold" in env:
                    print_warn(line_num, "anim_hold in anim_hold")

            elif func_name == "show":
                if not env and not any(
                    args[0].startswith(x) for x in ["bg", "fg", "ui_img"]
                ):
                    check_show = True

            elif func_name.startswith("trans"):
                if (
                    len(args) >= 2
                    and args[0].startswith("cam")
                    and not is_nil(args[1])
                    and wait_time <= 0.1
                ):
                    check_trans = True

            elif func_name == "wait":
                if isinstance(args[0], (int, float)):
                    wait_time += args[0]

    except Exception as e:
        print_warn(line_num, f"error when parsing code: {e}")

    if check_show and check_trans:
        print_warn(line_num, "show() outside of trans()")

    return anim_hold_tracked


def check_dialogue(chara_name, dialogue, line_num):
    for c in dialogue:
        if is_special_char(c):
            print_warn(line_num, f"special character U+{ord(c):04X} in dialogue")

    match = re.compile("（TODO：(.*?)：.*?）").search(dialogue)
    if match:
        print_warn(line_num, f"TODO: {match.group(1)} in dialogue")
    elif "TODO" in dialogue:
        print_warn(line_num, "TODO in dialogue")

    dialogue = normalize_dialogue(dialogue)

    if chara_name:
        if "\n" in chara_name:
            print_warn(line_num, "new line in character name")

        if not dialogue.startswith("“"):
            print_warn(line_num, "dialogue not start with quote mark")
        if not dialogue.endswith("”"):
            print_warn(line_num, "dialogue not end with quote mark")
        if "“" in dialogue[1:-1]:
            print_warn(line_num, "double left quote mark inside dialogue")
        if "”" in dialogue[1:-1]:
            print_warn(line_num, "double right quote mark inside dialogue")
    else:
        match = re.compile(".*?：“.*?”").fullmatch(dialogue)
        if match:
            print_warn(line_num, "quote with single colon")

    # if len(dialogue) > 54:
    #     print_warn(line_num, "dialogue longer than 54 chars")

    if any(x in dialogue for x in ",.?!;:'\"()"):
        print_warn(line_num, "half width punctuation in dialogue")


def lint_file(in_filename):
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        anim_hold_tracked = False
        for code, chara_name, dialogue, line_num in entries:
            if code and not dialogue:
                print_warn(line_num, "code block with empty dialogue")

            if code:
                anim_hold_tracked = check_code(code, line_num, anim_hold_tracked)

            if dialogue:
                check_dialogue(chara_name, dialogue, line_num)


def main():
    with open(template_filename, "r", encoding="utf-8") as f_template:
        for line in f_template:
            if line.startswith("@include"):
                in_filename = os.path.join(in_dir, line.strip().split()[1])
                print(in_filename)
                lint_file(in_filename)
                print()


if __name__ == "__main__":
    main()
