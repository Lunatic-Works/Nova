#!/usr/bin/env python3

import re

from hyphen import Hyphenator
from nova_script_parser import parse_chapters

filename = '../../Assets/Resources/LocalizedResources/English/Scenarios/001.txt'

SOFT_HYPHEN = '\xad'
NBSP = '\xa0'


def normalize(s):
    s = s.replace(SOFT_HYPHEN, '')
    s = s.replace(NBSP, ' ')
    return s


def add_soft_hyphens(hyphenator, s):
    tokens = re.compile(r'(<.*?>|\w+)').split(s)
    for i in range(len(tokens)):
        token = tokens[i]
        if not re.compile(r'\w+').fullmatch(token):
            continue
        syllables = hyphenator.syllables(token)
        if not syllables:
            continue
        token = SOFT_HYPHEN.join(syllables)
        tokens[i] = token
    s = ''.join(tokens)
    return s


def add_nbsp(s):
    s = s.replace('Mr. ', 'Mr.' + NBSP)
    s = s.replace('Ms. ', 'Ms.' + NBSP)
    return s


def main():
    hyphenator = Hyphenator('en_GB')

    with open(filename, 'r', encoding='utf-8') as f:
        chapters = parse_chapters(f)

    with open(filename, 'w', encoding='utf-8', newline='\n') as f:
        for chapter_idx, (chapter_name, entries, head_eager_code,
                          tail_eager_code) in enumerate(chapters):
            print(chapter_name)

            f.write(f'@<|\n{head_eager_code}\n|>\n')

            for entry_idx, (code, chara_name, dialogue) in enumerate(entries):
                if code:
                    f.write(f'<|\n{code}\n|>\n')
                if dialogue:
                    dialogue = normalize(dialogue)
                    dialogue = add_soft_hyphens(hyphenator, dialogue)
                    dialogue = add_nbsp(dialogue)
                    if chara_name:
                        f.write(f'{chara_name}::{dialogue}\n')
                    else:
                        f.write(dialogue + '\n')
                    if entry_idx < len(entries) - 1:
                        f.write('\n')

            if tail_eager_code:
                f.write(f'@<|\n{tail_eager_code}\n|>\n')
            else:
                f.write('@<||>\n')
            if chapter_idx < len(chapters) - 1:
                f.write('\n')


if __name__ == '__main__':
    main()
