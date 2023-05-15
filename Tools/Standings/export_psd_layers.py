#!/usr/bin/env python3

import json
import os
import re

import numpy as np
import pandas as pd
import skimage
import skimage.io
from psd_tools import PSDImage
from psd_tools.api.layers import (Group, PixelLayer, ShapeLayer,
                                  SmartObjectLayer, TypeLayer)

chara_var = None

in_filename = 'in.psd'
out_dir = 'static'
out_prefix = ''
ignored_layer_names = []
ignored_group_names = []


def save_layer(layer, layer_name, size):
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

    img = skimage.img_as_ubyte(img)
    out_filename = os.path.join(
        out_dir, out_prefix + layer_name.replace('-', '_') + '.png')
    skimage.io.imsave(out_filename,
                      img,
                      check_contrast=False,
                      compress_level=1)


def walk(layer, layer_name, size):
    print(layer.name)
    if isinstance(layer,
                  (PixelLayer, ShapeLayer, SmartObjectLayer, TypeLayer)):
        if layer.name not in ignored_layer_names:
            save_layer(layer, layer_name, size)
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


def convert_xlsx_to_csv(target, name):
    df = pd.read_excel(target + '/' + name + '.xlsx',
                       index_col=None).fillna('')
    df.to_csv(target + '/' + name + '.csv', index=False)
    with open(target + '/' + name + '.csv', 'r', encoding='utf-8') as f:
        lines = f.readlines()
        lines = [re.sub(r',Unnamed: \d', '', line) for line in lines]
    with open(target + '/' + name + '.csv', 'w', encoding='utf-8') as f:
        f.writelines(lines)


def main(target):
    print(in_filename)
    print(out_dir)

    os.makedirs(out_dir, exist_ok=True)
    psd = PSDImage.open(in_filename)
    print('size', psd.size)
    walk(psd, '', psd.size)

    convert_xlsx_to_csv(target, 'layer')
    convert_xlsx_to_csv(target, 'desc')


if __name__ == '__main__':
    with open('chara_set.json', 'r', encoding='utf-8') as f:
        chara_var = json.load(f)

    for target in chara_var.values():
        if os.path.exists(target):
            in_filename = target + "/in.psd"
            out_dir = "../../Resources/Standings/" + target
            main(target)
