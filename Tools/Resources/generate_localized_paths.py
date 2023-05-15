#!/usr/bin/env python3

import os

resources_dir = '../../Assets/Resources/'
out_filename = os.path.join(resources_dir, 'LocalizedResourcePaths.txt')

with open(out_filename, 'w', encoding='utf-8', newline='\n') as f:
    for root, dirs, files in os.walk(
            os.path.join(resources_dir, 'LocalizedResources')):
        for file in sorted(files):
            if file.endswith('.asset') or file.endswith('.meta'):
                continue
            filename = os.path.join(root, os.path.splitext(file)[0])
            filename = filename.replace('\\', '/')
            filename = filename.replace(resources_dir, '')
            print(filename)
            f.write(filename + '\n')
