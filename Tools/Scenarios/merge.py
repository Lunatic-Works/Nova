#!/usr/bin/env python3

import os

in_dir = '../../Assets/Resources/Scenarios/'
template_filename = 'template.txt'
out_filename = 'scenario.txt'

with open(template_filename, 'r', encoding='utf-8') as f_template:
    with open(out_filename, 'w', encoding='utf-8', newline='\n') as f_out:
        for line in f_template:
            if line.startswith('@include'):
                include_filename = os.path.join(in_dir,
                                                line.strip().split()[1])
                if not os.path.exists(include_filename):
                    print('File not found:', include_filename)
                    continue
                with open(include_filename, 'r',
                          encoding='utf-8') as f_include:
                    f_out.write(f_include.read())
            else:
                f_out.write(line)
