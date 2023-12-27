#!/usr/bin/env python3

from joblib import Parallel, delayed
from lua_parser import walk_functions
from nova_script_parser import normalize_dialogue, parse_chapters

in_filename = "scenario.txt"
known_chara_names = ["李竹内", "王二宫", "张浅野", "孙西本", "陈高天"]
other_filename = "其他"
n_jobs = 8
parse_auto_voice = True


def parse_chara(chapters, file_chara_name):
    if file_chara_name == other_filename:
        parse_others(chapters)
        return

    out_filename = in_filename.replace(".txt", f"_{file_chara_name}.txt")
    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        first_chapter = True
        dialogue_set = set()
        for chapter_name, entries, _, _ in chapters:
            print(file_chara_name, chapter_name)

            first_line = True
            auto_voice_status = False
            auto_voice_id = 0
            auto_voice_overridden = False
            say_filename = ""
            for code, chara_name, dialogue, _ in entries:
                if parse_auto_voice:
                    for func_name, args, _ in walk_functions(code):
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

                if chara_name == file_chara_name:
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


def parse_others(chapters):
    out_filename = in_filename.replace(".txt", f"_{other_filename}.txt")
    with open(out_filename, "w", encoding="utf-8", newline="\n") as f:
        first_chapter = True
        for chapter_name, entries, _, _ in chapters:
            print(other_filename, chapter_name)

            first_line = True
            for _, chara_name, dialogue, _ in entries:
                if chara_name and chara_name not in known_chara_names:
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

    Parallel(n_jobs)(
        delayed(parse_chara)(chapters, file_chara_name)
        for file_chara_name in known_chara_names + [other_filename]
    )


if __name__ == "__main__":
    import subprocess

    subprocess.run("python merge.py", shell=True)
    main()
