#!/usr/bin/env python3

from nova_script_parser import (normalize_dialogue, parse_chapters,
                                walk_functions)

in_filename = 'scenario.txt'
chara_names = ['李竹内', '王二宫', '张浅野', '孙宝翠', '陈牧子']
other_filename = '其他'

with open(in_filename, 'r', encoding='utf-8') as f:
    chapters = parse_chapters(f)

out_files = {}
for chara_name in chara_names:
    out_filename = in_filename.replace('.txt', f'_{chara_name}.txt')
    out_files[chara_name] = open(out_filename,
                                 'w',
                                 encoding='utf-8',
                                 newline='\n')

for file_chara_name in chara_names:
    print(file_chara_name)

    f = out_files[file_chara_name]
    first_chapter = True
    dialogue_set = set()
    for chapter_name, entries, _, _ in chapters:
        print(chapter_name)

        first_line = True
        auto_voice_status = False
        auto_voice_id = 0
        auto_voice_overridden = False
        say_filename = ''
        for code, chara_name, dialogue in entries:
            for func_name, args, _ in walk_functions(code):
                if (func_name == 'auto_voice_on'
                        and args[0].s == file_chara_name):
                    auto_voice_status = True
                    auto_voice_id = args[1].n
                elif (func_name == 'auto_voice_off'
                      and args[0].s == file_chara_name):
                    auto_voice_status = False
                elif func_name == 'auto_voice_skip':
                    auto_voice_overridden = True
                elif func_name == 'say':
                    auto_voice_overridden = True
                    say_filename = args[1].s

            if chara_name and file_chara_name in chara_name.split('&'):
                if first_line:
                    first_line = False
                    if first_chapter:
                        first_chapter = False
                    else:
                        f.write('\n')
                    f.write(chapter_name + '\n\n')

                if auto_voice_status and not auto_voice_overridden:
                    idx_marker = f'{auto_voice_id % 1000:03d} '
                    auto_voice_id += 1
                else:
                    if say_filename:
                        idx_marker = say_filename + ' '
                    else:
                        idx_marker = ''

                dialogue = normalize_dialogue(dialogue)
                if dialogue in dialogue_set:
                    dup_marker = 'D '
                else:
                    dialogue_set.add(dialogue)
                    dup_marker = ''

                f.write(f'{idx_marker}{dup_marker}{dialogue}\n')

            auto_voice_overridden = False
            say_filename = ''

for x in out_files.values():
    x.close()

if not other_filename:
    exit()

out_filename = in_filename.replace('.txt', f'_{other_filename}.txt')
with open(out_filename, 'w', encoding='utf-8', newline='\n') as f:
    first_chapter = True
    for chapter_name, entries, _, _ in chapters:
        first_line = True
        for _, chara_name, dialogue in entries:
            if chara_name and all(x not in chara_names
                                  for x in chara_name.split('&')):
                if first_line:
                    first_line = False
                    if first_chapter:
                        first_chapter = False
                    else:
                        f.write('\n')
                    f.write(chapter_name + '\n\n')
                f.write(f'{chara_name}：{normalize_dialogue(dialogue)}\n')
