#!/usr/bin/env python3

import hashlib
import json
import os

import numpy as np
import pandas as pd
from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import (
    Group,
    PixelLayer,
    ShapeLayer,
    SmartObjectLayer,
    TypeLayer,
)

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

    layers[layer_name] = img


def walk(layer, layer_name, size):
    print(f"- {layer.name}")
    if isinstance(layer, (PixelLayer, ShapeLayer, SmartObjectLayer, TypeLayer)):
        if layer.name not in ignored_layer_names:
            save_layer(layer, layer.name, size)
    elif isinstance(layer, (Group, PSDImage)):
        for child in layer:
            child_name = layer_name
            if child.name not in ignored_group_names:
                if child_name:
                    child_name += "_"
                child_name += child.name
            walk(child, child_name, size)
    else:
        raise ValueError(f"Unknown layer {type(layer)}: {layer_name}")


def alpha_composite(back, front):
    result = np.empty_like(back)
    alpha = front[:, :, 3:]
    result[:, :, :3] = alpha * front[:, :, :3] + (1 - alpha) * back[:, :, :3]
    alpha = alpha.squeeze(axis=2)
    result[:, :, 3] = alpha + (1 - alpha) * back[:, :, 3]
    return result


def merge_file(target):
    global layers
    layers = {}

    in_filename = f"{target}/in.psd"
    out_dir = f"./Preview/{target}"
    print(in_filename)

    os.makedirs(out_dir, exist_ok=True)
    psd = PSDImage.open(in_filename)
    print("size", psd.size)
    walk(psd, "", psd.size)

    to_merge = pd.read_excel(f"{target}/layer.xlsx", index_col=None).to_numpy()
    desc = pd.read_excel(f"{target}/desc.xlsx", index_col=None).to_numpy()

    for i, row in enumerate(to_merge):
        mlist = [x for x in row if not np.isnan(x)]
        mlist = mlist[::-1]

        merged = None
        for layer in mlist:
            if not layer:
                continue

            if layer in layers:
                if merged is None:
                    merged = layer
                else:
                    merged = alpha_composite(merged, layer)
            else:
                if merged is None:
                    merged = np.zeros((psd.size[1], psd.size[0], 4))
                print("! Missing", layer)

        merged = Image.fromarray((merged * 255).astype(np.uint8), "RGBA")

        fname = "_".join([x for x in desc[i] if not np.isnan(x)])
        fname = f"{i}_{fname}"
        print(f"<- {out_dir}/{fname}.png")
        merged.save(f"{out_dir}/{fname}.png")


def main():
    with open("chara_set.json", "r", encoding="utf-8") as f:
        chara_var = json.load(f)

    for target in chara_var.values():
        if not os.path.exists(target):
            continue

        hash_filename = f"./Preview/{target}/.hash"
        if os.path.exists(hash_filename):
            with open(hash_filename, "r") as f:
                old_hash = f.read()
        else:
            old_hash = ""

        flist = [f"{target}/in.psd", f"{target}/layer.xlsx", f"{target}/desc.xlsx"]
        new_hash = ""
        for fname in flist:
            if not os.path.exists(f):
                continue

            with open(fname, "rb") as f:
                new_hash += hashlib.md5(f.read()).hexdigest()

        if old_hash == new_hash:
            print("Skip", target)
            continue

        merge_file(target)

        with open(hash_filename, "w", encoding="utf-8", newline="\n") as f:
            f.write(new_hash)

        print("Done", target, "\n")


if __name__ == "__main__":
    main()
