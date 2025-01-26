from __future__ import annotations

from zipfile import ZipFile
from typing import Callable
from pathlib import Path
import subprocess
import argparse
import shutil
import json
import os


class BuildInfos:

    def __init__(self, dir: Path):

        with open(Path(dir, "build.json"), "rb") as reader:
            datas = json.load(reader)

        self.name = datas["name"]
        self.csproj = Path(datas["csproj"])
        self.include = [Path(dir, path) for path in datas["include"]]
        self.dependencies = [Path(dir, path) for path in datas["dependencies"]]


class CommandLineInterface:

    def __init__(self):
        parser = argparse.ArgumentParser()
        subparsers = parser.add_subparsers()

        # fmt: off
        self._add_subcommand(subparsers, "compile", compile, "Compile the project and create a zip archive ready for testing")
        self._add_subcommand(subparsers, "release", release, "Compile the project and create the release zip archive")
        self._add_subcommand(subparsers, "shut-down", shut_down, "Close all instances of the game")
        self._add_subcommand(subparsers, "start-local", start_local, "Compile the project, then start a local game")
        # fmt: on

        args = parser.parse_args()

        if hasattr(args, "func"):
            args.func()
        else:
            parser.print_help()

    def _add_subcommand(self, subparsers, name: str, func: Callable, help_text: str):
        """Ajoute une sous-commande à l'argument parser"""
        subcommand_parser = subparsers.add_parser(name, help=help_text)
        subcommand_parser.set_defaults(func=func)


MOD_NAME = "TheDescent"
PATH_7D2D = Path(os.environ["PATH_7D2D"])
PATH_7D2D_USER = Path(os.environ["APPDATA"], "7DaysToDie")
PATH_7D2D_EXE = Path(PATH_7D2D, "7DaysToDie.exe")
MOD_PATH = Path(PATH_7D2D, "Mods", MOD_NAME)
PREFABS_PATH = Path(PATH_7D2D_USER, "LocalPrefabs/leopard/Prefabs")

BUILD_ZIP = Path(f"{MOD_NAME}.zip")
BUILD_DIR = Path("build")
BUILD_INFOS = BuildInfos("")


def _return_code(command: str) -> int:
    return subprocess.run(command).returncode


def _include_file(path: Path, move: bool = False):

    dst = Path(BUILD_DIR, path)

    if not dst.parent.exists():
        os.makedirs(dst.parent)

    if move is True:
        shutil.move(path, dst)
    else:
        shutil.copy(path, dst)


def _include_dir(dir: Path):
    shutil.copytree(dir, Path(BUILD_DIR, dir))


def compile(buildInfos: BuildInfos = BUILD_INFOS):

    if _return_code(f"dotnet build --no-incremental {buildInfos.csproj.absolute()}") != 0:
        raise SystemExit()

    if BUILD_ZIP.exists():
        os.remove(BUILD_ZIP)

    if BUILD_DIR.exists():
        shutil.rmtree(BUILD_DIR)

    os.makedirs(BUILD_DIR)

    _include_dir("Config")
    _include_dir("UIAtlases")

    _include_file(f"{MOD_NAME}.dll", move=True)
    _include_file(f"{MOD_NAME}.pdb", move=True)
    _include_file("ModInfo.xml")

    shutil.make_archive(MOD_NAME, "zip", BUILD_DIR)


def install():
    if MOD_PATH.exists():
        shutil.rmtree(MOD_PATH)

    with ZipFile(BUILD_ZIP, "r") as zip_file:
        zip_file.extractall(MOD_PATH)


def start_local():

    compile()
    install()
    shut_down()

    subprocess.Popen(
        cwd=PATH_7D2D,
        executable=PATH_7D2D_EXE,
        args=["--noeac"],
    )

    clear_world("Old Honihebu County")  # default 2048
    clear_world("Old Wosayuwe Valley")  # default 4096


def shut_down():
    # fmt: off
    subprocess.run("taskkill /IM 7DaysToDie.exe /F", capture_output=True)
    subprocess.run("taskkill /IM 7DaysToDieServer.exe /F", capture_output=True)
    # fmt: on


def clear_world(world_name: str, save_name: str = "Caves", hard: bool = False):

    world_dir = Path(PATH_7D2D_USER, f"GeneratedWorlds/{world_name}")
    save_dir = Path(PATH_7D2D_USER, f"Saves/{world_name}/{save_name}")

    shutil.rmtree(Path(save_dir, "Region"), ignore_errors=True)
    shutil.rmtree(Path(save_dir, "DynamicMeshes"), ignore_errors=True)
    shutil.rmtree(Path(save_dir, "decoration.7dt"), ignore_errors=True)

    if hard:
        shutil.rmtree(world_dir)


def release():
    """
    TODOC
    """
    compile()

    shutil.rmtree(BUILD_DIR, ignore_errors=True)
    os.makedirs(BUILD_DIR)

    for path in DEPENDENCIES + [BUILD_ZIP]:
        if not path.exists():
            raise SystemExit(f"Missing dependency: '{path}'")

        if not path.is_file():
            raise SystemExit(f"Dependency is not a file: '{path}'")

        dst = Path(BUILD_DIR, path.stem)

        with ZipFile(path, "r") as zip_file:
            zip_file.extractall(dst)

    shutil.make_archive(f"{MOD_NAME}-release", "zip", BUILD_DIR)


if __name__ == "__main__":
    CommandLineInterface()
