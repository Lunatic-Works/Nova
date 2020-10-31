#!/usr/bin/env python3

from nova_script_parser import normalize_dialogue, parse_chapters

in_filename = 'scenario.txt'
out_filename = 'scenario_no_code.txt'

with open(in_filename, 'r', encoding='utf-8') as f:
    chapters = parse_chapters(f)

with open(out_filename, 'w', encoding='utf-8', newline='\n') as f:
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)

        f.write(chapter_name + '\n\n')
        for _, chara_name, dialogue in entries:
            dialogue = normalize_dialogue(dialogue, remove_todo=False)
            if dialogue:
                if chara_name:
                    f.write(f'{chara_name}：{dialogue}\n\n')
                else:
                    f.write(dialogue + '\n\n')
