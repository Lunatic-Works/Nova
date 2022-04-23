#!/usr/bin/env python3

import os

import numpy as np
import psd_tools
import skimage
import skimage.io

in_filename = 'in.psd'
out_dir = 'out'
out_prefix = ''
width = 2048
height = 4096
ignored_layer_names = []
ignored_group_names = []


def save_layer(layer, layer_name):
    img = np.zeros([height, width, 4])
    layer_np = layer.numpy()
    if layer_np.shape[2] == 3:
        layer_np = np.concatenate(
            [layer_np, np.ones_like(layer_np[:, :, :1])], axis=2)
    img[layer.top:layer.bottom, layer.left:layer.right, :] = layer_np
    img = skimage.img_as_ubyte(img)
    out_filename = os.path.join(
        out_dir, out_prefix + layer_name.replace('-', '_') + '.png')
    skimage.io.imsave(out_filename, img, check_contrast=False)


def walk(layer, layer_name):
    print(layer.name)
    if isinstance(layer, psd_tools.api.layers.PixelLayer):
        if layer.name not in ignored_layer_names:
            save_layer(layer, layer_name)
    elif isinstance(
            layer,
        (psd_tools.api.layers.Group, psd_tools.api.psd_image.PSDImage)):
        for child in layer:
            child_name = layer_name
            if child.name not in ignored_group_names:
                if child_name:
                    child_name += '_'
                child_name += child.name
            walk(child, child_name)
    else:
        raise ValueError('Unknown layer {}: {}'.format(type(layer),
                                                       layer_name))


def main():
    os.makedirs(out_dir, exist_ok=True)
    psd = psd_tools.PSDImage.open(in_filename)
    walk(psd, '')


if __name__ == '__main__':
    main()
