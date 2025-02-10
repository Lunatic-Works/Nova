#!/usr/bin/env python3

import os

import imageio
import numpy as np
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

    img = np.rint(img * 255).astype(np.uint8)
    layer_name = layer_name.replace("-", "_")
    out_filename = os.path.join(out_dir, f"{out_prefix}{layer_name}.png")
    imageio.imsave(out_filename, img, check_contrast=False, compress_level=1)


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


def main():
    print(in_filename)
    print(out_dir)

    os.makedirs(out_dir, exist_ok=True)
    psd = PSDImage.open(in_filename)
    print("size", psd.size)
    walk(psd, "", psd.size)


if __name__ == "__main__":
    main()
