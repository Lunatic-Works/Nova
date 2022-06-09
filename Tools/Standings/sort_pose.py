#!/usr/bin/env python3

import re

io_filename = '../../Assets/Nova/Lua/pose.lua'


def get_score(x):
    score = 0
    if '正常' in x or 'normal' in x:
        score = -1
    return score


def sort_lines(lines, idx_begin, idx_end):
    rows = []
    for i in range(idx_begin, idx_end):
        match = re.compile(r' *\[\'(.*)\'\] = {(.*)}').match(lines[i])
        name = match.group(1)
        tokens = [x.strip().strip('\'') for x in match.group(2).split(',')]
        rows.append((name, tokens))

    rows.sort(
        key=lambda x: [get_score(x[0])] + [(get_score(y), y) for y in x[1]])
    for i in range(len(rows) - 1):
        if rows[i][1] == rows[i + 1][1]:
            print(f'Duplicate poses: {rows[i][0]}, {rows[i + 1][0]}')

    for i, row in enumerate(rows):
        name, tokens = row
        value = ', '.join([f'\'{x}\'' for x in tokens])
        lines[idx_begin + i] = f'        [\'{name}\'] = {{{value}}},\n'


def main():
    with open(io_filename, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    i = 0
    while i < len(lines):
        if re.compile(r' *\[\'.*\'] = {$').match(lines[i]):
            j = i + 1
            while j < len(lines):
                if re.compile(r' *},$').match(lines[j]):
                    break
                j += 1
            sort_lines(lines, i + 1, j)
            i = j
        i += 1

    with open(io_filename, 'w', encoding='utf-8', newline='\n') as f:
        f.writelines(lines)


if __name__ == '__main__':
    main()
