import os
import sys
import shutil
import zipfile
import requests
import argparse
import platform

DEFAULT_URL = "https://mrgidor.github.io/downloads/DarkRL-Modpack.zip" # Example Modpack URL

def get_minecraft_dir():
    system = platform.system()
    if system == "Windows":
        appdata = os.getenv("APPDATA")
        if not appdata:
            raise EnvironmentError("APPDATA not set on Windows.")
        return os.path.join(appdata, ".minecraft")
    elif system == "Darwin":
        return os.path.expanduser("~/Library/Application Support/minecraft")
    else:  # assume Linux / other Unix
        return os.path.expanduser("~/.minecraft")

def download_modpack(url, download_path):
    print(f"Downloading modpack from {url}...")
    resp = requests.get(url, stream=True, timeout=60)
    resp.raise_for_status()
    with open(download_path, "wb") as f:
        for chunk in resp.iter_content(chunk_size=8192):
            if chunk:
                f.write(chunk)
    print(f"Downloaded to {download_path}")

def rename_old_mods_folder(mods_folder):
    mods_old = os.path.join(os.path.dirname(mods_folder), "mods-old")
    if os.path.exists(mods_old):
        print("Removing existing mods-old...")
        shutil.rmtree(mods_old)
    if os.path.exists(mods_folder):
        print("Renaming current mods to mods-old...")
        os.rename(mods_folder, mods_old)

def create_new_mods_folder(mods_folder):
    if not os.path.exists(mods_folder):
        print("Creating new mods folder...")
        os.makedirs(mods_folder, exist_ok=True)

def unzip_modpack(zip_path, extract_to):
    print(f"Extracting {zip_path} -> {extract_to} ...")
    with zipfile.ZipFile(zip_path, "r") as z:
        z.extractall(extract_to)
    print("Extraction complete.")

def ensure_dir(path):
    if not os.path.exists(path):
        os.makedirs(path, exist_ok=True)

def main():
    parser = argparse.ArgumentParser(description="Modpack Installer (Windows/macOS/Linux)")
    parser.add_argument("--url", "-u", default=DEFAULT_URL, help="Modpack zip URL")
    parser.add_argument("--yes", "-y", action="store_true", help="No prompts, overwrite existing mods")
    parser.add_argument("--installer-dir", help="Custom installer directory (for testing)")
    args = parser.parse_args()

    try:
        mc_dir = get_minecraft_dir()
    except Exception as e:
        print(f"Failed to determine Minecraft directory: {e}")
        sys.exit(1)

    installer_dir = args.installer_dir or os.path.join(mc_dir, "modpack_installer")
    mods_folder = os.path.join(mc_dir, "mods")
    zip_path = os.path.join(installer_dir, "modpack.zip")

    try:
        ensure_dir(installer_dir)

        if os.path.exists(mods_folder) and not args.yes:
            resp = input(f"Existing mods folder found at {mods_folder}. Rename to mods-old and install new mods? [y/N]: ").strip().lower()
            if resp not in ("y", "yes"):
                print("Aborted by user.")
                return

        download_modpack(args.url, zip_path)
        rename_old_mods_folder(mods_folder)
        create_new_mods_folder(mods_folder)
        unzip_modpack(zip_path, mods_folder)

        print("Installation complete.")
    except requests.RequestException as re:
        print(f"Network error: {re}")
        sys.exit(2)
    except zipfile.BadZipFile:
        print("Downloaded file is not a valid zip.")
        sys.exit(3)
    except Exception as e:
        print(f"An error occurred: {e}")
        sys.exit(4)

if __name__ == "__main__":
    main()