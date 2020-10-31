#!/usr/bin/env python3

from luaparser import astnodes
from nova_script_parser import get_node_name, parse_chapters, walk_functions

in_filename = 'scenario.txt'


def do_chapter(entries, bg_list):
    for code, _, _ in entries:
        if not code:
            continue
        for func_name, args, _ in walk_functions(code):
            if (func_name in [
                    'show', 'trans', 'trans2', 'trans_fade', 'trans_left',
                    'trans_right', 'trans_up', 'trans_down'
            ] and args and get_node_name(args[0]).startswith('bg')
                    and isinstance(args[1], astnodes.String)):
                bg_name = args[1].s
                if bg_name not in bg_list:
                    bg_list.append(bg_name)
            elif (func_name == 'show_loop' and args
                  and get_node_name(args[0]).startswith('bg')):
                for field in args[1].fields:
                    bg_name = field.value.s
                    if bg_name not in bg_list:
                        bg_list.append(bg_name)


def main():
    with open(in_filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f)

    bg_list = []
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        do_chapter(entries, bg_list)
    print()

    for x in bg_list:
        print(x)


if __name__ == '__main__':
    main()
