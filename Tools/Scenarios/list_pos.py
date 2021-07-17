#!/usr/bin/env python3

from luaparser import astnodes
from nova_script_parser import (get_node_name, parse_chapters, parse_table,
                                walk_functions)

in_filename = 'scenario.txt'

DEFAULT_BG_POS = (0, 0, 2.5, 10, 0)
DEFAULT_CAM_POS = (0, 0, 60, -10, 0)


def normalize_bg_name(s):
    tokens = s.split('_')
    while (tokens[-1].isnumeric() or tokens[-1]
           in ['day', 'sunset', 'night', 'overcast', 'rain', 'blur']):
        tokens = tokens[:-1]
    out = '_'.join(tokens)
    return out


def update_pos(old_pos, new_pos, default_pos):
    out = []
    for i in range(5):
        if new_pos and len(new_pos) > i and new_pos[i] is not None:
            out.append(new_pos[i])
        elif old_pos and len(old_pos) > i and old_pos[i] is not None:
            out.append(old_pos[i])
        else:
            out.append(default_pos[i])
    for i in range(4, 1, -1):
        if out[i] == (0, 0, 0):
            out[i] = 0
        if len(out) > i and out[i] == default_pos[i]:
            out = out[:-1]
        else:
            break
    out = tuple(out)
    return out


def dict_set_add(d, k, v):
    if k in d:
        d[k].add(v)
    else:
        d[k] = set(v)


def typed_sign(x):
    if x == 0:
        return 0
    elif x > 0:
        return 1
    else:
        return 2


def typed_item(x):
    if isinstance(x, (int, float)):
        return 0, typed_sign(x), abs(x)
    elif isinstance(x, str):
        return 1, x
    elif isinstance(x, tuple):
        return 2, tuple(typed_item(y) for y in x)
    elif x is None:
        return 3, None
    else:
        raise ValueError(f'Unknown type: {type(x)}')


def main():
    with open(in_filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f)

    bg_name_to_pos_set = {}
    bg_name_to_cam_pos_set = {}
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        now_bg_name = None
        now_bg_pos = update_pos(None, None, DEFAULT_BG_POS)
        now_cam_pos = update_pos(None, None, DEFAULT_CAM_POS)
        for code, _, _ in entries:
            if not code:
                continue
            for func_name, args, _ in walk_functions(code):
                if (func_name == 'show' and args
                        and get_node_name(args[0]) == 'bg'
                        and isinstance(args[1], astnodes.String)):
                    now_bg_name = normalize_bg_name(args[1].s)
                    if len(args) >= 3:
                        now_bg_pos = update_pos(now_bg_pos,
                                                parse_table(args[2]),
                                                DEFAULT_BG_POS)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name,
                                 now_cam_pos)
                elif (func_name in [
                        'trans', 'trans2', 'trans_fade', 'trans_left',
                        'trans_right', 'trans_up', 'trans_down'
                ] and args and get_node_name(args[0]) == 'bg'
                      and isinstance(args[1], astnodes.String)):
                    now_bg_name = normalize_bg_name(args[1].s)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name,
                                 now_cam_pos)
                elif (func_name == 'show_loop' and args
                      and get_node_name(args[0]) == 'bg'):
                    now_bg_name = normalize_bg_name(args[1].fields[0].value.s)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name,
                                 now_cam_pos)
                elif (func_name == 'hide' and args
                      and get_node_name(args[0]) == 'bg'):
                    now_bg_name = None

                elif (func_name == 'move' and args
                      and get_node_name(args[0]) == 'bg'):
                    now_bg_pos = update_pos(now_bg_pos, parse_table(args[1]),
                                            DEFAULT_BG_POS)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)

                elif (func_name == 'move' and args
                      and get_node_name(args[0]) == 'cam'):
                    now_cam_pos = update_pos(now_cam_pos, parse_table(args[1]),
                                             DEFAULT_CAM_POS)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name,
                                 now_cam_pos)

    print()

    keys = [x for x in bg_name_to_pos_set.keys() if x]
    for k in sorted(keys):
        print(k)
        print('pos:')
        for pos in sorted(bg_name_to_pos_set[k], key=typed_item):
            print(pos)
        print('cam_pos:')
        for pos in sorted(bg_name_to_cam_pos_set[k], key=typed_item):
            print(pos)
        print()


if __name__ == '__main__':
    main()
