#!/usr/bin/env python3

import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from time import sleep

from zipchmod import zipchmod

unity_version = "2020.3.48f1"


def prepare():
    path = Path.cwd()
    while True:
        if (path / ".git").exists():
            break
        if path.parent == path:
            raise RuntimeError("Cannot find git root dir")
        path = path.parent
    os.chdir(path)
    print("Git root dir:", path)

    if not Path("./Assets/Scenes/Main.unity").exists():
        raise RuntimeError("Not in project root dir")

    if Path("./Temp").exists():
        raise RuntimeError("Unity may be running. If it's not, try delete Temp dir")

    commit = subprocess.run(
        ["git", "rev-parse", "HEAD"], capture_output=True, encoding="utf-8"
    ).stdout.strip()
    print("Commit:", commit)

    target_commit = None
    if len(sys.argv) > 1:
        target_commit = sys.argv[1]
    if not target_commit:
        raise RuntimeError("Target commit not given")
    if target_commit != "--ignore-commit":
        if target_commit != commit:
            raise RuntimeError(f"Wrong commit, should be {target_commit}")

        out = subprocess.run(["git", "status"], capture_output=True, encoding="utf-8")
        if "nothing to commit, working tree clean" not in out.stdout:
            raise RuntimeError("Git working tree not clean")

    os.makedirs("./Build", exist_ok=True)


def get_unity_path():
    if sys.platform == "win32":
        return rf"C:\Program Files\Unity\Hub\Editor\{unity_version}\Editor\Unity.exe"
    elif sys.platform == "linux":
        return Path.home() / f"Unity/Hub/Editor/{unity_version}/Editor/Unity"
    elif sys.platform == "darwin":
        return f"/Applications/Unity/Hub/Editor/{unity_version}/Unity.app/Contents/MacOS/Unity"
    else:
        raise ValueError(f"Unknown platform: {sys.platform}")


def run_build(log_path, method):
    unity_path = get_unity_path()

    print(f"Running {method}...")
    out = subprocess.run(
        [
            unity_path,
            "-quit",
            "-batchmode",
            "-nographics",
            "-logFile",
            log_path,
            "-executeMethod",
            method,
        ]
    )
    if out.returncode != 0:
        raise RuntimeError(f"Build failed, check {log_path}")


def wait_log(log_path):
    print("Finishing build...")
    while True:
        path = Path(log_path)
        if path.exists():
            with open(path, "r", encoding="utf-8") as f:
                data = f.read()
            build_result = re.compile("Build result: .*$", re.MULTILINE).search(data)
            if build_result:
                build_result = build_result.group(0).strip()
                break
        sleep(1)

    print(build_result)
    if "Build result: Succeeded" not in build_result:
        raise RuntimeError(f"Build failed, check {log_path}")

    out_dir = re.compile("/Build/[^/]+/?").search(build_result).group(0)
    out_dir = "." + out_dir.rstrip("/")

    product_name = build_result.split("/")[-1]
    product_name = Path(product_name).stem

    return out_dir, product_name


def make_zip(out_dir):
    os.chdir(out_dir)

    if sys.platform == "win32":
        zip_exe_path = "../../Tools/Build/zip.exe"
    else:
        zip_exe_path = "zip"
    zip_filename = Path(out_dir).name + ".zip"

    if Path(zip_filename).exists():
        print("Warning: Remove old zip")
        os.remove(zip_filename)

    print("Making zip...")
    out = subprocess.run([zip_exe_path, "-9qr", zip_filename, "."])
    if out.returncode != 0:
        raise RuntimeError("Failed to make zip")

    shutil.move(zip_filename, "../")
    os.chdir("../..")


def build_windows():
    log_path = "./Build/build_windows.log"

    run_build(log_path, "Nova.Editor.NovaBuilder.BuildWindows")

    out_dir, product_name = wait_log(log_path)

    make_zip(out_dir)

    if sys.platform != "win32":
        shutil.rmtree(out_dir)


def build_linux():
    log_path = "./Build/build_linux.log"

    run_build(log_path, "Nova.Editor.NovaBuilder.BuildLinux")

    out_dir, product_name = wait_log(log_path)
    os.remove(f"{out_dir}/LinuxPlayer_s.debug")
    os.remove(f"{out_dir}/UnityPlayer_s.debug")

    make_zip(out_dir)

    if sys.platform == "win32":
        print("Setting permission...")
        zipchmod(f"{out_dir}.zip", [product_name])

    if sys.platform != "linux":
        shutil.rmtree(out_dir)


def build_macos():
    log_path = "./Build/build_macos.log"

    run_build(log_path, "Nova.Editor.NovaBuilder.BuildMacOS")

    out_dir, product_name = wait_log(log_path)

    make_zip(out_dir)

    if sys.platform == "win32":
        print("Setting permission...")
        zipchmod(
            f"{out_dir}.zip",
            [
                f"{product_name}.app/Contents/Frameworks/UnityPlayer.dylib",
                f"{product_name}.app/Contents/Frameworks/libMonoPosixHelper.dylib",
                f"{product_name}.app/Contents/Frameworks/libmonobdwgc-2.0.dylib",
                f"{product_name}.app/Contents/MacOS/{product_name}",
                f"{product_name}.app/Contents/PlugIns/tolua.bundle/Contents/MacOS/tolua",
            ],
        )

    if sys.platform != "darwin":
        shutil.rmtree(out_dir)


# Need to set signing key
def build_android():
    log_path = "./Build/build_android.log"

    run_build(log_path, "Nova.Editor.NovaBuilder.BuildAndroid")

    out_dir, product_name = wait_log(log_path)


# TODO: Build Xcode project
def build_ios():
    log_path = "./Build/build_ios.log"

    run_build(log_path, "Nova.Editor.NovaBuilder.BuildiOS")

    out_dir, product_name = wait_log(log_path)


def main():
    prepare()
    build_windows()
    build_linux()
    build_macos()
    # build_android()
    # build_ios()


if __name__ == "__main__":
    main()
