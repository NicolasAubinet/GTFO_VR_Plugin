import shutil
import sys
import os
from pathlib import Path
from argparse import ArgumentParser


dependency_targets = ["Newtonsoft.Json.dll", "openvr_api.dll", "Bhaptics.Tact.dll", "SteamVR_Standalone_IL2CPP.dll"]
dependency_dir_targets = ["bhaptics-patterns"]
plugin_targets = ["GTFO_VR.dll"]

gtfo_data_plugin_targets = ["openvr_api.dll"]
gtfo_data_dir_targets = ["StreamingAssets"]

unhollower_lib_targets = ['AssemblyUnhollower.dll', 'UnhollowerBaseLib.dll', 'UnhollowerRuntimeLib.dll']

readme_target = ["README.MD"]
changelog_target = ["Changelog.txt"]


def copy_files(list, source: Path, dest: Path, critical=True):
    print("Copying " + str(list) + " from\n " + str(source) + " to \n" + str(dest))
    for item in list:
        if((source/item).exists()):
            shutil.copy(str(source / item), str(dest / item))
        else:
            if(critical):
                sys.exit("Could not find target " + str(item) + ", build failed!")
            print("Warning: Could not find: " + str(item))
    print("Done...!")

def copy_dirs(list, source: Path, dest: Path):
    print("Copying " + str(list) + " from\n " + str(source) + " to \n" + str(dest))
    for dir in list:
        source_dir = source / dir
        dest_dir = dest / dir
        if dest_dir.exists():
            shutil.rmtree(dest_dir)
        shutil.copytree(str(source_dir), str(dest_dir))
        print("Done...!")

def copy_plugins_dir():
    staging_plugins_dir = staging_bepinex_dir / "plugins"
    staging_plugins_dir.mkdir(exist_ok=True)

    copy_files(plugin_targets, gtfo_plugin_dir, staging_plugins_dir)
    copy_files(dependency_targets, release_dependencies_lib_dir, staging_plugins_dir)
    copy_dirs(dependency_dir_targets, release_dependencies_lib_dir, staging_plugins_dir)

def create_dummy_dirs():
    (staging_bepinex_dir / "unity-libs").mkdir(exist_ok=True)
    (staging_bepinex_dir / "unhollowed").mkdir(exist_ok=True)

def copy_unhollowed_libs():
    staging_bepinex_core = staging_bepinex_dir / "core"
    staging_bepinex_core.mkdir(exist_ok=True)

    copy_files(unhollower_lib_targets, release_dependencies_lib_unhollower_dir, staging_bepinex_core)



def copy_gtfo_data_dir():
    gtfo_data_staging_dir = staging_dir / "GTFO_Data"
    gtfo_data_staging_dir.mkdir(exist_ok=True)

    gtfo_data_plugins_staging_dir = gtfo_data_staging_dir / "Plugins"
    gtfo_data_plugins_staging_dir.mkdir(exist_ok=True)

    copy_files(gtfo_data_plugin_targets, release_dependencies_lib_dir, gtfo_data_plugins_staging_dir)
    copy_dirs(gtfo_data_dir_targets, release_dependencies_dir, gtfo_data_staging_dir)

def copy_text_files():
    readme_path = current_dir / "README.md"
    readme_staging_path = staging_dir / "README.md"

    copy_files(readme_target, current_dir, staging_dir)
    copy_files(changelog_target, release_dependencies_dir, staging_dir, critical=False)


def find_version():
    version_file = current_dir / "GTFO_VR" / "Core" / "GTFO_VR_Plugin.cs"
    print("Trying to get version from: " + str(version_file))
    if not version_file.exists():
        sys.exit("Could not find version file!")
    version_cs = open(str(version_file), 'r', encoding='cp932', errors='ignore')
    lines = version_cs.readlines()
    for line in lines:
        if "VERSION =" in line:
            result = line.split('"')[1]
            return result
    version_cs.close()

print("Running release tool...")



parser = ArgumentParser()
parser.add_argument("-v", "--version", dest="version",
                    help="Create archive with given version")

args = parser.parse_args()

try:
    gtfo_dir = Path(os.environ['GTFO_PATH'])
except:
    print("Your GTFO env path seems to be invalid. Set GTFO_PATH to your GTFO installation.")

current_dir = Path(os.path.dirname(os.path.abspath(__file__)))

print("GTFO_PATH set to " + str(gtfo_dir))
gtfo_plugin_dir = gtfo_dir / "BepInEx/plugins/"

if not gtfo_plugin_dir.exists():
    os.exit("Your GTFO installation seems to not contain Bepinex/plugins/. This tool is made to grab newest dlls from"
            "there, so make sure this exists!")

print("Found plugin dir...")

release_dependencies_dir = current_dir / "Release_Dependencies"
release_dependencies_lib_dir = release_dependencies_dir / "libs"
release_dependencies_lib_unhollower_dir = release_dependencies_lib_dir / "Unhollower"

staging_dir = Path(current_dir / "Staging")
print("Setting up stating directory at - " + str(staging_dir))
staging_dir.mkdir(exist_ok=True)

staging_bepinex_dir = (staging_dir / "BepInEx")
staging_bepinex_dir.mkdir(exist_ok=True)

copy_plugins_dir()
create_dummy_dirs()
copy_unhollowed_libs()
copy_gtfo_data_dir()
copy_text_files()

if args.version is None:
    version = find_version()
    version = version.replace('.','_')
else:
    version = args.version
print("Got version " + str(version))

archive_name = "GTFO_VR_Release_" + version + '_Partial'
shutil.make_archive(archive_name, 'zip', str(staging_dir))

print("Created archive " + str(archive_name))