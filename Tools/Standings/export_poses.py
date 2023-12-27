#!/usr/bin/env python3

import os

import cv2
import numpy as np
from joblib import Parallel, delayed
from lupa.lua52 import LuaRuntime
from tqdm import tqdm

pose_filename = "../../Assets/Nova/Lua/pose.lua"
chara_name = "gaotian"
in_dir = "../../Assets/StandingsUncropped/Gaotian"
out_dir = "./out"
use_gpu = True

if use_gpu:
    import cupy as cp
else:
    cp = np
    cp.asnumpy = lambda x: x


def export_pose(pose_name, layer_names):
    out_filename = f"{out_dir}/{pose_name}.png"
    if os.path.exists(out_filename):
        return

    img = cp.zeros((4096, 2048, 4))
    for layer_name in layer_names:
        in_filename = f"{in_dir}/{layer_name}.png"
        # Use numpy instead of cv2 to handle Unicode filename
        layer = np.fromfile(in_filename, dtype=np.uint8)
        layer = cv2.imdecode(layer, cv2.IMREAD_UNCHANGED)
        assert layer.dtype == np.uint8
        layer = cp.asarray(layer)
        layer = layer.astype(cp.float32) / 255
        alpha = layer[:, :, 3:]

        img[:, :, :3] = (1 - alpha) * img[:, :, :3] + alpha * layer[:, :, :3]
        img[:, :, 3:] = (1 - alpha) * img[:, :, 3:] + alpha

    img = cp.round(img * 255).astype(cp.uint8)
    img = cp.asnumpy(img)
    ret, img = cv2.imencode(
        os.path.splitext(out_filename)[1], img, (cv2.IMWRITE_PNG_COMPRESSION, 1)
    )
    assert ret is True
    img.tofile(out_filename)


def main():
    with open(pose_filename, "r", encoding="utf-8") as f:
        code = f.read()

    lua = LuaRuntime(unpack_returned_tuples=True)
    lua.execute(code + "_poses = poses")
    poses = lua.eval("_poses")
    poses = poses[chara_name]
    poses = [
        (pose_name, list(layer_names.values()))
        for pose_name, layer_names in poses.items()
    ]

    Parallel(n_jobs=2)(delayed(export_pose)(*pose) for pose in tqdm(poses))


if __name__ == "__main__":
    main()
