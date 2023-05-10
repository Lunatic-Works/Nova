#!/usr/bin/env python3

import hashlib
import json
import os

import numpy as np
import pandas as pd
from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import (Group, PixelLayer, ShapeLayer,
                                  SmartObjectLayer, TypeLayer)

chara_var = None

in_filename = 'in.psd'
out_dir = 'static'
out_prefix = ''
ignored_layer_names = []
ignored_group_names = []

layers = {}


def save_layer(layer, layer_name, size):
    global layers

    layer_np = layer.numpy()
    top = layer.top
    bottom = layer.bottom
    left = layer.left
    right = layer.right
    if top < 0:
        layer_np = layer_np[-top:, :, :]
        top = 0
    if bottom > size[1]:
        layer_np = layer_np[:size[1] - bottom, :, :]
        bottom = size[1]
    if left < 0:
        layer_np = layer_np[:, -left:, :]
        left = 0
    if right > size[0]:
        layer_np = layer_np[:, :size[0] - right, :]
        right = size[0]

    if layer_np.shape[2] == 3:
        layer_np = np.pad(layer_np, ((0, 0), (0, 0), (0, 1)),
                          constant_values=1)

    img = np.zeros((size[1], size[0], 4))
    img[top:bottom, left:right, :] = layer_np

    layers[layer_name] = img


def walk(layer, layer_name, size):
    print("- " + layer.name)
    if isinstance(layer,
                  (PixelLayer, ShapeLayer, SmartObjectLayer, TypeLayer)):
        if layer.name not in ignored_layer_names:
            save_layer(layer, layer.name, size)
    elif isinstance(layer, (Group, PSDImage)):
        for child in layer:
            child_name = layer_name
            if child.name not in ignored_group_names:
                if child_name:
                    child_name += '_'
                child_name += child.name
            walk(child, child_name, size)
    else:
        raise ValueError(f'Unknown layer {type(layer)}: {layer_name}')


def alpha_composite(back, front):
    result = np.zeros_like(back)
    result[:, :, :3] = (1 - front[:, :, 3:4]) * back[:, :, :3] + (
        front[:, :, 3:4]) * front[:, :, :3]
    result[:, :, 3] = front[:, :, 3] + \
        (1 - front[:, :, 3]) * np.squeeze(back[:, :, 3])
    return result


def main(target):
    global layers
    layers = {}

    print(in_filename)

    os.makedirs(out_dir, exist_ok=True)
    psd = PSDImage.open(in_filename)
    print('size', psd.size)
    walk(psd, '', psd.size)

    to_merge = pd.read_excel(target + '/layer.xlsx', index_col=None).to_numpy()
    desc = pd.read_excel(target + '/desc.xlsx', index_col=None).to_numpy()

    for i in range(len(to_merge)):
        mlist = [x for x in to_merge[i] if str(x) != 'nan']
        mlist = mlist[::-1]
        if mlist[0] in layers:
            merged = layers[mlist[0]]
        else:
            merged = np.zeros((psd.size[1], psd.size[0], 4))
            print('! Missing', mlist[0])

        for j in range(1, len(mlist)):
            if mlist[j]:
                if mlist[j] in layers:
                    merged = alpha_composite(merged, layers[mlist[j]])
                else:
                    print('! Missing', mlist[j])

        merged = Image.fromarray((merged * 255).astype(np.uint8), 'RGBA')

        fname = str(i) + "_" + \
            "_".join([x for x in desc[i] if str(x) != 'nan'])
        print('<- [', out_dir + '/' + fname + '.png]')
        merged.save(out_dir + '/' + fname + '.png')


if __name__ == '__main__':
    with open('chara_set.json', 'r', encoding='utf8') as f:
        chara_var = json.load(f)

    for target in chara_var.values():
        if os.path.exists(target):
            if os.path.exists("./Preview/" + target + '/.hash'):
                with open("./Preview/" + target + '/.hash', 'r') as f:
                    old_hash = f.read()
            else:
                old_hash = ''

            flist = [
                target + '/in.psd', target + '/layer.xlsx',
                target + '/desc.xlsx'
            ]
            new_hash = ''
            for f in flist:
                if os.path.exists(f):
                    new_hash += hashlib.md5(open(f, 'rb').read()).hexdigest()

            if old_hash == new_hash:
                print('Skip', target)
                continue

            in_filename = target + "/in.psd"
            out_dir = "./Preview/" + target
            main(target)

            with open("./Preview/" + target + '/.hash', 'w') as f:
                f.write(new_hash)

            print('Done ', target, '\n')
