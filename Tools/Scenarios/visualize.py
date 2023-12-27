#!/usr/bin/env python3

import numpy as np
import skimage.io
from lua_parser import walk_functions
from nova_script_parser import is_chapter, parse_chapters
from scipy.stats.qmc import Sobol

in_filename = "scenario.txt"
out_filename = "scenario.png"

MONOLOGUE_COLOR = (128, 128, 128)
BG_NONE_COLOR = (0, 0, 0)
BGM_NONE_COLOR = (0, 0, 0)

dialogue_width = 32
bg_width = 4
bgm_width = 4

str_to_color_config = {
    "black": (0, 0, 0),
    "white": (255, 255, 255),
}

bg_suffixes = ["blur"]

str_to_color_cache = {}
sobol = Sobol(d=3, seed=0)


def str_to_color(s):
    if s in str_to_color_config:
        return str_to_color_config[s]
    if s in str_to_color_cache:
        return str_to_color_cache[s]

    rgb = sobol.random()[0]
    rgb = (rgb * 128).astype(int) + 64
    rgb = rgb.tolist()
    str_to_color_cache[s] = rgb

    return rgb


def normalize_bg_name(s):
    tokens = s.split("_")
    while tokens[-1].isnumeric() or tokens[-1] in bg_suffixes:
        tokens = tokens[:-1]
    out = "_".join(tokens)
    return out


def chapter_to_tape(entries, tape, chara_set, bg_set, timeline_set, bgm_set):
    dialogue_color = MONOLOGUE_COLOR
    bg_color = BG_NONE_COLOR
    timeline_color = BG_NONE_COLOR
    bgm_color = BGM_NONE_COLOR
    for code, chara_name, _, _ in entries:
        if chara_name:
            chara_set.add(chara_name)
            dialogue_color = str_to_color(chara_name)
        else:
            dialogue_color = MONOLOGUE_COLOR

        if code:
            for func_name, args, _ in walk_functions(code):
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
                    and args[0] == "bg"
                    and isinstance(args[1], str)
                ):
                    bg_name = normalize_bg_name(args[1])
                    bg_set.add(bg_name)
                    bg_color = str_to_color(bg_name)
                elif func_name == "show_loop" and args[0] == "bg":
                    bg_name = normalize_bg_name(args[1][0])
                    bg_set.add(bg_name)
                    bg_color = str_to_color(bg_name)
                elif func_name == "hide" and args[0] == "bg":
                    bg_color = BG_NONE_COLOR

                elif func_name == "timeline":
                    timeline_name = args[0]
                    timeline_set.add(timeline_name)
                    timeline_color = str_to_color(timeline_name)
                elif func_name == "timeline_hide":
                    timeline_color = BG_NONE_COLOR

                elif func_name in ["play", "fade_in"] and args[0] == "bgm":
                    bgm_name = args[1]
                    bgm_set.add(bgm_name)
                    bgm_color = str_to_color(bgm_name)
                elif func_name in ["stop", "fade_out"] and args and args[0] == "bgm":
                    bgm_color = BGM_NONE_COLOR

        if bg_color != BG_NONE_COLOR:
            _bg_color = bg_color
        else:
            _bg_color = timeline_color
        tape.append((dialogue_color, _bg_color, bgm_color))


def tapes_to_img(tapes):
    tape_width = dialogue_width + bg_width + bgm_width
    img_height = max(len(tape) for tape in tapes)
    img = np.zeros((img_height, len(tapes) * tape_width, 3), dtype=np.uint8)
    for tape_idx, tape in enumerate(tapes):
        img_tape = img[:, tape_idx * tape_width : (tape_idx + 1) * tape_width :, :]
        for idx, (dialogue_color, bg_color, bgm_color) in enumerate(tape):
            img_tape[idx, :dialogue_width, :] = dialogue_color
            img_tape[idx, dialogue_width : dialogue_width + bg_width, :] = bg_color
            img_tape[idx, dialogue_width + bg_width :, :] = bgm_color
    return img


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    tapes = []
    tape = []
    chara_set = set()
    bg_set = set()
    timeline_set = set()
    bgm_set = set()
    for chapter_name, entries, head_eager_code, _ in chapters:
        print(chapter_name)
        if tape and is_chapter(head_eager_code):
            tapes.append(tape)
            tape = []
        chapter_to_tape(entries, tape, chara_set, bg_set, timeline_set, bgm_set)
    print()
    tapes.append(tape)

    print("Characters:")
    for x in sorted(chara_set):
        print(x)
    print()
    print("Backgrounds:")
    for x in sorted(bg_set):
        print(x)
    print()
    print("Timelines:")
    for x in sorted(timeline_set):
        print(x)
    print()
    print("BGM:")
    for x in sorted(bgm_set):
        print(x)
    print()

    img = tapes_to_img(tapes)
    skimage.io.imsave(out_filename, img, compress_level=1)


if __name__ == "__main__":
    import subprocess

    subprocess.run("python merge.py", shell=True)
    main()
