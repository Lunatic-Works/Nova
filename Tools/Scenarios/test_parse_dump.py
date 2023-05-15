#!/usr/bin/env python3

from nova_script_parser import parse_chapters

in_filename = 'scenario.txt'
out_filename = 'scenario_out.txt'

with open(in_filename, 'r', encoding='utf-8') as f:
    chapters = parse_chapters(f)

with open(out_filename, 'w', encoding='utf-8', newline='\n') as f:
    for chapter_idx, (chapter_name, entries, head_fast_code,
                      tail_fast_code) in enumerate(chapters):
        print(chapter_name)

        f.write(f'@<|\n{head_fast_code}\n|>\n')

        for entry_idx, (code, chara_name, dialogue) in enumerate(entries):
            if code:
                f.write(f'<|\n{code}\n|>\n')
            if dialogue:
                if chara_name:
                    f.write(f'{chara_name}：：{dialogue}\n')
                else:
                    f.write(dialogue + '\n')
                if entry_idx < len(entries) - 1:
                    f.write('\n')

        if tail_fast_code:
            f.write(f'@<|\n{tail_fast_code}\n|>\n')
        else:
            f.write('@<||>\n')
        if chapter_idx < len(chapters) - 1:
            f.write('\n')
