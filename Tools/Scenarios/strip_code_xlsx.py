#!/usr/bin/env python3

from nova_script_parser import normalize_dialogue, parse_chapters
from openpyxl import Workbook

in_filename = 'scenario.txt'
out_filename = 'scenario_no_code.xlsx'

with open(in_filename, 'r', encoding='utf-8') as f:
    chapters = parse_chapters(f)

wb = Workbook()
for chapter_name, entries, _, _ in chapters:
    print(chapter_name)

    ws = wb.create_sheet(chapter_name)
    for _, chara_name, dialogue in entries:
        dialogue = normalize_dialogue(dialogue,
                                      remove_rich=False,
                                      remove_todo=False)
        if dialogue:
            if chara_name:
                ws.append([f'{chara_name}：{dialogue}'])
            else:
                ws.append([dialogue])

wb.save(out_filename)
