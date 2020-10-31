#!/usr/bin/env python3

from collections import Counter

from nova_script_parser import normalize_dialogue, parse_chapters

in_filename = 'scenario.txt'

with open(in_filename, 'r', encoding='utf-8') as f:
    chapters = parse_chapters(f)

counter = Counter()
for chapter_name, entries, _, _ in chapters:
    print(chapter_name)

    for _, _, dialogue in entries:
        dialogue = normalize_dialogue(dialogue)
        if dialogue:
            counter[len(dialogue)] += 1

for k, v in sorted(counter.items()):
    print(k, v)
