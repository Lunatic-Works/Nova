#!/usr/bin/env python3

import json
import os
import re

import numpy as np
import pandas as pd
import skimage
import skimage.io
from psd_tools import PSDImage
from psd_tools.api.layers import (
    Group,
    PixelLayer,
    ShapeLayer,
    SmartObjectLayer,
    TypeLayer,
)

in_filename = "in.psd"
out_dir = "out"
out_prefix = ""
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
        layer_np = layer_np[: size[1] - bottom, :, :]
        bottom = size[1]
    if left < 0:
        layer_np = layer_np[:, -left:, :]
        left = 0
    if right > size[0]:
        layer_np = layer_np[:, : size[0] - right, :]
        right = size[0]

    if layer_np.shape[2] == 3:
        layer_np = np.pad(layer_np, ((0, 0), (0, 0), (0, 1)), constant_values=1)

    img = np.zeros((size[1], size[0], 4))
    img[top:bottom, left:right, :] = layer_np

    img = skimage.img_as_ubyte(img)
    layer_name = layer_name.replace("-", "_")
    out_filename = os.path.join(out_dir, f"{out_prefix}{layer_name}.png")
    skimage.io.imsave(out_filename, img, check_contrast=False, compress_level=1)


def walk(layer, layer_name, size):
    print(layer.name)
    if isinstance(layer, (PixelLayer, ShapeLayer, SmartObjectLayer, TypeLayer)):
        if layer.name not in ignored_layer_names:
            save_layer(layer, layer_name, size)
    elif isinstance(layer, (Group, PSDImage)):
        for child in layer:
            child_name = layer_name
            if child.name not in ignored_group_names:
                if child_name:
                    child_name += "_"
                child_name += child.name
            walk(child, child_name, size)
    else:
        print(f"Unknown layer {type(layer)}: {layer_name}")


def convert_xlsx_to_csv(target, name):
    data = pd.read_excel(f"{target}/{name}.xlsx", index_col=None).fillna("")
    data.to_csv(f"{target}/{name}.csv", index=False)
    with open(f"{target}/{name}.csv", "r", encoding="utf-8") as f:
        lines = f.readlines()
        lines = [re.compile(r",Unnamed: \d").sub("", line) for line in lines]
    with open(f"{target}/{name}.csv", "w", encoding="utf-8") as f:
        f.writelines(lines)


def export_file(in_filename, out_dir):
    print(in_filename)
    print(out_dir)

    os.makedirs(out_dir, exist_ok=True)
    psd = PSDImage.open(in_filename)
    print("size", psd.size)
    walk(psd, "", psd.size)


def export_all():
    with open("chara_set.json", "r", encoding="utf-8") as f:
        chara_var = json.load(f)

    for target in chara_var.values():
        if not os.path.exists(target):
            continue

        in_filename = f"{target}/in.psd"
        out_dir = f"../../Resources/Standings/{target}"
        export_file(in_filename, out_dir)

        convert_xlsx_to_csv(target, "layer")
        convert_xlsx_to_csv(target, "desc")


if __name__ == "__main__":
    export_file(in_filename, out_dir)
