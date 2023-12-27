#!/usr/bin/env python3

import os
from glob import glob

import skimage.io
from pytoshop.core import PsdFile
from pytoshop.enums import Compression
from pytoshop.layers import ChannelImageData, LayerRecord

in_filenames = glob("./out/*.png")
out_filename = "./out.psd"
compression = Compression.rle

psd = PsdFile(num_channels=3, height=4096, width=2048, compression=compression)
for in_filename in in_filenames:
    print(in_filename)
    img = skimage.io.imread(in_filename)
    layer_name = os.path.splitext(os.path.basename(in_filename))[0]
    layer = LayerRecord(
        channels={
            (-1 if k == 3 else k): ChannelImageData(
                image=img[:, :, k], compression=compression
            )
            for k in range(4)
        },
        top=0,
        bottom=img.shape[0],
        left=0,
        right=img.shape[1],
        name=layer_name,
    )
    psd.layer_and_mask_info.layer_info.layer_records.append(layer)

with open(out_filename, "wb") as f:
    psd.write(f)
