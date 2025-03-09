import os
import shutil
import zipfile
import requests

def download_modpack(url, download_path):
    """Downloads the modpack zip file from the given URL."""
    print(f"Downloading modpack from {url}...")
    response = requests.get(url, stream=True)
    response.raise_for_status()
    with open(download_path, 'wb') as file:
        for chunk in response.iter_content(chunk_size=8192):
            file.write(chunk)
    print(f"Modpack downloaded to {download_path}.")

def rename_old_mods_folder(mods_folder):
    """Renames the current mods folder to mods-old, deleting mods-old if it exists."""
    mods_old_folder = os.path.join(os.path.dirname(mods_folder), "mods-old")
    if os.path.exists(mods_old_folder):
        print("Deleting existing mods-old folder...")
        shutil.rmtree(mods_old_folder)
    if os.path.exists(mods_folder):
        print("Renaming mods folder to mods-old...")
        os.rename(mods_folder, mods_old_folder)

def create_new_mods_folder(mods_folder):
    """Creates a new mods folder."""
    if not os.path.exists(mods_folder):
        print("Creating new mods folder...")
        os.makedirs(mods_folder)

def unzip_modpack(zip_path, extract_to):
    """Extracts the modpack zip file to the specified folder."""
    print(f"Unzipping modpack to {extract_to}...")
    with zipfile.ZipFile(zip_path, 'r') as zip_ref:
        zip_ref.extractall(extract_to)
    print("Modpack successfully installed.")

def main():
    # Define paths
    appdata = os.getenv('APPDATA')
    minecraft_dir = os.path.join(appdata, ".minecraft")
    installer_dir = os.path.join(minecraft_dir, "darkrlinstaller")
    mods_folder = os.path.join(minecraft_dir, "mods")
    modpack_url = "https://horizons-smp.com/downloads/DarkRL-Modpack.zip"  # Replace with the actual URL
    zip_path = os.path.join(installer_dir, "modpack.zip")

    try:
        # Ensure the installer directory exists
        if not os.path.exists(installer_dir):
            os.makedirs(installer_dir)

        # Download the modpack
        download_modpack(modpack_url, zip_path)

        # Handle existing mods folder
        rename_old_mods_folder(mods_folder)

        # Create a new mods folder
        create_new_mods_folder(mods_folder)

        # Unzip the modpack into the new mods folder
        unzip_modpack(zip_path, mods_folder)

        print("Installation complete!")

    except Exception as e:
        print(f"An error occurred: {e}")

if __name__ == "__main__":
    main()
