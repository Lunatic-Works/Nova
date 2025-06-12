#!/usr/bin/env python3

import re

from lua_parser import walk_functions
from nova_script_parser import normalize_dialogue, parse_chapters

in_filename = "scenario.txt"
out_filename = "scenario_no_code.tex"
need_parse_code = True
highlight_chara = None

translate_data = [
    ("room", "房间"),
]
translate_data = sorted(translate_data, key=lambda x: len(x[0]), reverse=True)


def camel_to_snake(s):
    s = re.compile("(.)([A-Z][a-z]+)").sub(r"\1_\2", s)
    s = re.compile("([a-z0-9])([A-Z])").sub(r"\1_\2", s)
    s = s.lower()
    return s


def translate(s):
    s = camel_to_snake(s)
    for x, y in translate_data:
        s = s.replace(x, y)
    s = s.replace("_", "")
    # assert not any("A" <= c <= "Z" or "a" <= c <= "z" for c in s), s
    return s


def parse_code(code, f):
    bg_name = ""
    bgm_name = ""
    for func_name, args, _ in walk_functions(code):
        if func_name == "action":
            func_name = args[0]
            args = args[1:]

        if (
            func_name
            in [
                "show",
                "trans",
                "trans2",
                "trans_fade",
                "trans_left",
                "trans_right",
                "trans_up",
                "trans_down",
            ]
            and args[0] in ["bg", "cg"]
            and isinstance(args[1], str)
        ):
            bg_name = args[1]
        elif func_name == "show_loop" and args[0] == "bg":
            bg_name = args[1][0]
        elif func_name == "timeline":
            bg_name = args[0]

        elif func_name in ["play", "fade_in"] and args[0] == "bgm":
            bgm_name = args[1]

    return bg_name, bgm_name


def normalize_tex(s):
    s = s.replace("\\", "{\\textbackslash}")
    for x in " &%$#_{}":
        s = s.replace(x, f"\\{x}")
    s = s.replace("~", "{\\textasciitilde}")
    s = s.replace("^", "{\\textasciicircum}")
    s = s.replace("\n", " \\\\\n")
    s = s.replace(" \\\\\n \\\\\n", "\n\n")
    for x in "？！":
        s = s.replace(f"“{x}", f"“\\hspace{{0pt}}{x}")
    for x in "—…":
        for y in "（【《“‘「『":
            s = s.replace(f"{y}{x}", f"{y}\\nobreak{x}")
        for y in "，。？！：；）】》”’」』":
            s = s.replace(f"{x}{y}", f"{x}\\nobreak{y}")
    return s


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        f.write(
            r"""\documentclass{article}
\usepackage[a4paper, left=1in, right=1in, top=1in, bottom=1in]{geometry}
\usepackage[hidelinks]{hyperref}
\usepackage[pagewise]{lineno}
\usepackage{titlesec}
\usepackage{xcolor}
\usepackage[CheckSingle=true, CJKecglue=\hspace{0pt}]{xeCJK}

\setlength{\parindent}{0pt}
\setlength{\parskip}{1ex}

\linenumbers

\titlespacing*{\section}{0pt}{2ex}{2ex}

"""
        )

        f.write("\\begin{document}\n\n")

        for chapter_name, entries, _, _ in chapters:
            print(chapter_name)

            chapter_name = normalize_tex(chapter_name)
            f.write(f"\\section*{{{chapter_name}}}\n\n")

            bg_name = ""
            bgm_name = ""
            last_bg_name = ""
            last_bgm_name = ""
            for code, chara_name, dialogue, _ in entries:
                if need_parse_code:
                    if bg_name:
                        last_bg_name = bg_name
                    if bgm_name:
                        last_bgm_name = bgm_name
                    bg_name, bgm_name = parse_code(code, f)
                    if bg_name == last_bg_name:
                        bg_name = ""
                    if bgm_name == last_bgm_name:
                        bgm_name = ""

                    if bg_name:
                        bg_name = normalize_tex(translate(bg_name))
                        f.write(f"{{\\color{{orange}} 场景：{bg_name}}}")
                    if bgm_name:
                        if bg_name:
                            f.write(" ")
                        bgm_name = normalize_tex(translate(bgm_name))
                        f.write(f"{{\\color{{blue}} 音乐：{bgm_name}}}")
                    if bg_name or bgm_name:
                        f.write("\n\n")

                dialogue = normalize_dialogue(dialogue, keep_todo=["配音"])
                if dialogue:
                    dialogue = normalize_tex(dialogue)

                    if chara_name:
                        chara_name = normalize_tex(chara_name)
                        f.write(f"{{\\color{{gray}} {chara_name}：}}")

                    if highlight_chara and highlight_chara == chara_name:
                        f.write(f"\\hl{{{dialogue}}}\\filbreak\n\n")
                    elif (
                        highlight_chara and f"TODO：配音：{highlight_chara}" in dialogue
                    ):
                        dialogue = re.compile(
                            f"（TODO：配音：{highlight_chara}[^）]*）"
                        ).sub(r"\\hl{\g<0>}", dialogue)
                        f.write(f"{dialogue}\\filbreak\n\n")
                    else:
                        f.write(f"{dialogue}\\filbreak\n\n")
            # f.write("\\newpage\n\n")

        f.write("\\end{document}\n")


if __name__ == "__main__":
    import subprocess

    from utils import run_merge

    run_merge()
    main()
    subprocess.run(["xelatex", out_filename])
