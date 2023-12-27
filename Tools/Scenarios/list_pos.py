#!/usr/bin/env python3

from lua_parser import is_nil, walk_functions
from nova_script_parser import parse_chapters

in_filename = "scenario.txt"

DEFAULT_BG_POS = (0, 0, 2.5, 10, 0)
DEFAULT_CAM_POS = (0, 0, 60, -10, 0)


def normalize_bg_name(s):
    tokens = s.split("_")
    while tokens[-1].isnumeric() or tokens[-1] in [
        "day",
        "sunset",
        "night",
        "overcast",
        "rain",
        "blur",
    ]:
        tokens = tokens[:-1]
    out = "_".join(tokens)
    return out


def normalize_tuple(x):
    if isinstance(x, (tuple, list)):
        return tuple(normalize_tuple(y) for y in x)
    if isinstance(x, float) and x.is_integer():
        x = int(x)
    return x


def update_pos(old_pos, new_pos, default_pos):
    if is_nil(old_pos):
        old_pos = []
    if is_nil(new_pos):
        new_pos = []

    out = []
    for i in range(len(default_pos)):
        if len(new_pos) > i and not is_nil(new_pos[i]):
            out.append(new_pos[i])
        elif len(old_pos) > i and not is_nil(old_pos[i]):
            out.append(old_pos[i])
        else:
            out.append(default_pos[i])

    out = normalize_tuple(out)
    out = tuple(0 if x == (0, 0, 0) else x for x in out)

    i = len(out) - 1
    while i >= 0 and out[i] == default_pos[i]:
        i -= 1
    out = out[: i + 1]

    return out


def dict_set_add(d, k, v):
    if k in d:
        d[k].add(v)
    else:
        d[k] = {v}


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
    elif is_nil(x):
        return 3, None
    else:
        raise ValueError(f"Unknown type: {type(x)}")


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    bg_name_to_pos_set = {}
    bg_name_to_cam_pos_set = {}
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)
        now_bg_name = None
        now_bg_pos = ()
        now_cam_pos = ()
        for code, _, _, _ in entries:
            if not code:
                continue

            for func_name, args, _ in walk_functions(code):
                if func_name == "show" and args[0] == "bg" and isinstance(args[1], str):
                    now_bg_name = normalize_bg_name(args[1])
                    if len(args) >= 3:
                        now_bg_pos = update_pos(now_bg_pos, args[2], DEFAULT_BG_POS)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name, now_cam_pos)

                elif (
                    func_name
                    in [
                        "trans",
                        "trans2",
                        "trans_fade",
                        "trans_left",
                        "trans_right",
                        "trans_up",
                        "trans_down",
                    ]
                    and args[0] == "bg"
                    and isinstance(args[1], str)
                ):
                    now_bg_name = normalize_bg_name(args[1])
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name, now_cam_pos)

                elif func_name == "show_loop" and args[0] == "bg":
                    now_bg_name = normalize_bg_name(args[1][0])
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name, now_cam_pos)

                elif func_name == "hide" and args[0] == "bg":
                    now_bg_name = None

                elif func_name == "move" and args[0] == "bg":
                    now_bg_pos = update_pos(now_bg_pos, args[1], DEFAULT_BG_POS)
                    dict_set_add(bg_name_to_pos_set, now_bg_name, now_bg_pos)

                elif func_name == "move" and args[0] == "cam":
                    now_cam_pos = update_pos(now_cam_pos, args[1], DEFAULT_CAM_POS)
                    dict_set_add(bg_name_to_cam_pos_set, now_bg_name, now_cam_pos)

    print()

    keys = [x for x in bg_name_to_pos_set.keys() if x]
    for k in sorted(keys):
        print(k)
        print("pos:")
        for pos in sorted(bg_name_to_pos_set[k], key=typed_item):
            print(pos)
        print("cam_pos:")
        for pos in sorted(bg_name_to_cam_pos_set[k], key=typed_item):
            print(pos)
        print()


if __name__ == "__main__":
    import subprocess

    subprocess.run("python merge.py", shell=True)
    main()
