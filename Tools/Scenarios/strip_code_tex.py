#!/usr/bin/env python3

import re

from luaparser import astnodes
from nova_script_parser import (get_node_name, normalize_dialogue,
                                parse_chapters, walk_functions)

in_filename = 'scenario.txt'
out_filename = 'scenario_no_code.tex'

translate_data = [
    ('room', '房间'),
]
translate_data = sorted(translate_data, key=lambda x: len(x[0]), reverse=True)


def camel_to_snake(s):
    s = re.compile('(.)([A-Z][a-z]+)').sub(r'\1_\2', s)
    s = re.compile('([a-z0-9])([A-Z])').sub(r'\1_\2', s)
    s = s.lower()
    return s


def translate(s):
    s = camel_to_snake(s)
    for x, y in translate_data:
        s = s.replace(x, y)
    s = s.replace('_', '')
    assert not any('A' <= c <= 'Z' or 'a' <= c <= 'z' for c in s), s
    return s


def parse_code(code, f):
    bg_name = None
    bgm_name = None
    for func_name, args, _ in walk_functions(code):
        if (func_name in [
                'show', 'trans', 'trans2', 'trans_fade', 'trans_left',
                'trans_right', 'trans_up', 'trans_down'
        ] and args and get_node_name(args[0]) == 'bg'
                and isinstance(args[1], astnodes.String)
                and not args[1].s.startswith('chapter')):
            bg_name = args[1].s
        elif (func_name == 'show_loop' and args
              and get_node_name(args[0]) == 'bg'):
            bg_name = args[1].fields[0].value.s
        elif func_name == 'timeline':
            bg_name = args[0].s
        elif (func_name in ['play', 'fade_in'] and args
              and get_node_name(args[0]) == 'bgm'):
            bgm_name = args[1].s
    return bg_name, bgm_name


def normalize_tex(s):
    s = s.replace('\\', '\\textbackslash')
    for x in ' &%$#_{}':
        s = s.replace(x, '\\' + x)
    s = s.replace('~', '\\textasciitilde')
    s = s.replace('^', '\\textasciicircum')
    s = s.replace('\n', ' \\\\\n')
    s = s.replace(' \\\\\n \\\\\n', '\n\n')
    return s


def main():
    with open(in_filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f)

    with open(out_filename, 'w', encoding='utf-8', newline='\n') as f:
        f.write(r"""\documentclass{article}
\usepackage[a4paper,left=1in,right=1in,top=1in,bottom=1in]{geometry}
\usepackage[hidelinks]{hyperref}
\usepackage{xcolor}
\usepackage{xeCJK}

\setlength{\parindent}{0pt}
\setlength{\parskip}{1ex}

""")

        f.write('\\begin{document}\n\n')

        for chapter_name, entries, _, _ in chapters:
            print(chapter_name)

            chapter_name = normalize_tex(chapter_name)
            f.write(f'\\section{{{chapter_name}}}\n\n')
            for code, chara_name, dialogue in entries:
                bg_name, bgm_name = parse_code(code, f)
                if bg_name:
                    bg_name = normalize_tex(translate(bg_name))
                    f.write(f'{{\\color{{orange}} 场景：{bg_name}}}\n\n')
                if bgm_name:
                    bgm_name = normalize_tex(translate(bgm_name))
                    f.write(f'{{\\color{{blue}} 音乐：{bgm_name}}}\n\n')

                dialogue = normalize_dialogue(dialogue, keep_todo=['配音'])
                if dialogue:
                    dialogue = normalize_tex(dialogue)
                    if chara_name:
                        chara_name = normalize_tex(chara_name)
                        f.write(
                            f'{{\\color{{lightgray}} {chara_name}}}{dialogue}\n\n'
                        )
                    else:
                        f.write(dialogue + '\n\n')
            f.write('\\newpage\n\n')

        f.write('\\end{document}\n')


if __name__ == '__main__':
    main()
