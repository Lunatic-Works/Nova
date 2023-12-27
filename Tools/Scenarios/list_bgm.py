#!/usr/bin/env python3

from lua_parser import walk_functions
from nova_script_parser import parse_chapters

in_filename = "scenario.txt"


def do_chapter(entries, bgm_list):
    for code, _, _, _ in entries:
        if not code:
            continue
        for func_name, args, _ in walk_functions(code):
            if func_name in ["play", "fade_in"] and args[0] == "bgm":
                bgm_name = args[1]
                if bgm_name not in bgm_list:
                    bgm_list.append(bgm_name)


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    bgm_list = []
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        do_chapter(entries, bgm_list)
    print()

    for x in bgm_list:
        print(x)


if __name__ == "__main__":
    import subprocess

    subprocess.run("python merge.py", shell=True)
    main()
