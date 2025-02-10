#!/usr/bin/env python3

import numpy as np
from joblib import Parallel, delayed
from lua_parser import walk_functions
from nova_script_parser import is_chapter, normalize_dialogue, parse_chapters

in_filename = "scenario.txt"
known_chara_names = ["李竹内", "王二宫", "张浅野", "孙西本", "陈高天"]
other_filename = "其他"
n_jobs = 8
parse_auto_voice = True


def parse_chara(chapters, file_chara_name):
    if file_chara_name == other_filename:
        parse_others(chapters)
        return None, None

    out_filename = in_filename.replace(".txt", f"_{file_chara_name}.txt")
    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        first_chapter = True
        dialogue_set = set()
        dialogue_counts = []
        dialogue_count = 0
        last_chapter_name = None
        for chapter_name, entries, head_eager_code, _ in chapters:
            if last_chapter_name is None:
                last_chapter_name = chapter_name
            if dialogue_count > 0:
                print(file_chara_name, last_chapter_name, dialogue_count)
                dialogue_counts.append(dialogue_count)
                dialogue_count = 0
            if is_chapter(head_eager_code):
                last_chapter_name = chapter_name

            # print(file_chara_name, chapter_name)

            first_line = True
            auto_voice_status = False
            auto_voice_id = 0
            auto_voice_overridden = False
            say_filename = ""
            for code, chara_name, dialogue, _ in entries:
                if parse_auto_voice:
                    for func_name, args, _ in walk_functions(code):
                        if func_name == "action":
                            func_name = args[0]
                            args = args[1:]

                        if func_name == "auto_voice_on" and args[0] == file_chara_name:
                            auto_voice_status = True
                            auto_voice_id = int(args[1])
                        elif (
                            func_name == "auto_voice_off" and args[0] == file_chara_name
                        ):
                            auto_voice_status = False
                        elif func_name == "auto_voice_skip":
                            auto_voice_overridden = True
                        elif func_name == "say":
                            auto_voice_overridden = True
                            say_filename = args[1]

                if any(x == file_chara_name for x in chara_name.split("&")):
                    dialogue_count += 1

                    if first_line:
                        first_line = False
                        if first_chapter:
                            first_chapter = False
                        else:
                            f.write("\n")
                        f.write(chapter_name + "\n\n")

                    dialogue = normalize_dialogue(dialogue)

                    if parse_auto_voice:
                        if auto_voice_status and not auto_voice_overridden:
                            idx_marker = f"{auto_voice_id % 1000:03d} "
                            auto_voice_id += 1
                        else:
                            if say_filename:
                                idx_marker = say_filename + " "
                            else:
                                idx_marker = ""

                        if dialogue in dialogue_set:
                            dup_marker = "D "
                        else:
                            dialogue_set.add(dialogue)
                            dup_marker = ""
                    else:
                        idx_marker = ""
                        dup_marker = ""

                    f.write(f"{idx_marker}{dup_marker}{dialogue}\n")

                auto_voice_overridden = False
                say_filename = ""

        if dialogue_count > 0:
            print(file_chara_name, last_chapter_name, dialogue_count)
            dialogue_counts.append(dialogue_count)

    return file_chara_name, sorted(dialogue_counts)


def parse_others(chapters):
    out_filename = in_filename.replace(".txt", f"_{other_filename}.txt")
    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        first_chapter = True
        for chapter_name, entries, _, _ in chapters:
            # print(other_filename, chapter_name)

            first_line = True
            for _, chara_name, dialogue, _ in entries:
                if chara_name and any(
                    x not in known_chara_names for x in chara_name.split("&")
                ):
                    if first_line:
                        first_line = False
                        if first_chapter:
                            first_chapter = False
                        else:
                            f.write("\n")
                        f.write(chapter_name + "\n\n")
                    dialogue = normalize_dialogue(dialogue)
                    f.write(f"{chara_name}：{dialogue}\n")


def main():
    with open(in_filename, "r", encoding="utf-8") as f:
        chapters = parse_chapters(f)

    stats = Parallel(n_jobs)(
        delayed(parse_chara)(chapters, file_chara_name)
        for file_chara_name in known_chara_names + [other_filename]
    )

    for name, dialogue_counts in stats:
        if name is None:
            continue

        dialogue_counts = [x for x in dialogue_counts]
        n_dialogue = sum(dialogue_counts)
        n_chapters = len(dialogue_counts)
        if dialogue_counts:
            mean = np.mean(dialogue_counts)
            std = np.std(dialogue_counts)
        else:
            mean = 0
            std = 0
        print(
            name, n_dialogue, n_chapters, f"{mean:.3g}", f"{std:.3g}", dialogue_counts
        )


if __name__ == "__main__":
    import subprocess

    subprocess.run("python merge.py", shell=True)
    main()
