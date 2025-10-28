# Minecraft Modpack Installer

A **simple, cross-platform tool** created for people who **don't want to struggle with installing mods** for Minecraft!  
This project is designed to make the process of updating or installing large modpacks as painless as possible—no more manual downloads, file copying, or Minecraft crashes due to missing files.

---

## ✨ Features

- **Easy to use**: Just run and follow the instructions—no complex setup!
- **Cross-platform**: Works on **Windows, macOS, and Linux**.
- **Backs up your old mods**: Automatically renames your current `mods` folder to `mods-old` before installing new mods.
- **Download and extract**: Pulls your chosen modpack ZIP from a URL and installs it in seconds.

---

## 🚀 Usage

### The Simple Way

1. **Download the prebuilt executable** for your operating system from the [Releases](../../releases) tab.
2. Place it in your **Downloads** directory (or wherever you want).
3. Open `cmd` (Windows) or your terminal (macOS/Linux).
4. Run:
   ```
   ModpackInstaller-Win.exe --url https://your.modpack.url/modpack.zip
   ```
   or (on macOS/Linux):
   ```
   ./ModpackInstaller-Linux --url https://your.modpack.url/modpack.zip
   ./ModpackInstaller-OSX --url https://your.modpack.url/modpack.zip
   ```
5. Follow the on-screen prompts. Your mods are installed!

> **Note:**  
> The modpack **must be a ZIP file** containing all necessary mods.

---

### For Forks & Server Owners

Want to make this even easier for your players or server community?

- **Fork this repository**.
- Change the `DEFAULT_URL` variable in the code to point to your own hosted modpack ZIP.
- Build the tool (see below).
- Distribute your custom installer or provide it in your repository!

---

## 🛠 Building From Source

1. Install [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Clone this repo:
   ```
   git clone https://github.com/MrGidor/Minecraft_Modpack_Installer.git
   ```
3. (Optional) Edit the `DEFAULT_URL` in `Program.cs` to your modpack.
4. Build:
   ```
   dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=false
   ```
   Replace `win-x64` with `linux-x64` or `osx-x64` as needed.
5. Your standalone executable will be in the `\bin\Release\net8.0\win-x64\publish` folder.

---

## 📦 What Kind of Modpacks?

- Your modpack **must be a `.zip` file**.
- Inside the ZIP, include all mods you want installed (typically all `.jar` files for `/mods`).
- You can include additional folders (e.g., configs), but the installer extracts everything into your Minecraft `mods` folder, so it's worthless to include those in the zip.

---

## 🤝 Contributing

PRs are welcome! If you have an idea for a new feature, open an issue or submit a pull request.

---

## 📄 License

[MIT](LICENSE)

---

**Enjoy your modded Minecraft, hassle-free!**
