// -----------------------------------------------------------------------------
// File: MainWindowsViewModel.cs
// Author: Gidor 
// Description: Handles the main window view model for the modpack installer
// License: MIT License (see LICENSE file in the project root for details)
// -----------------------------------------------------------------------------

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Modpack_Installer.Core;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Path = System.IO.Path;

namespace Modpack_Installer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Downloader _downloader = new();

    private string modPath = string.Empty;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private string modpackUrl = "https://mrgidor.github.io/downloads/DarkRL-Modpack.zip";

    [ObservableProperty]
    private bool backupMods = false;

    [ObservableProperty]
    private double progress = 0;

    [ObservableProperty]
    private bool isDownloading = false;
    public bool IsNotDownloading => !IsDownloading;

    [RelayCommand]
    private async Task DownloadAsync()
    {
        try
        {
            IsDownloading = true;
            Progress = 0;

            StatusMessage = "Starting download...";

            string? installerDir = null;
            await Task.Delay(50);
            string mcDir = _downloader.GetMinecraftDir();
            string instDir = installerDir ?? Path.Combine(mcDir, "modpack_installer");
            string modsFolder = Path.Combine(mcDir, "mods");
            Progress = 5;
            await Task.Delay(50);
            modPath = modsFolder;
            string zipPath = Path.Combine(instDir, "modpack.zip");

            Progress = 10;

            // Downloader
            StatusMessage = "Downloading modpack...";
            await _downloader.DownloadModpack(ModpackUrl, zipPath);

            Progress = 24;

            FinishDownload(zipPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            // Log error to a file
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "modpack_installer_error.log");
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {ex}\n";
                await File.AppendAllTextAsync(logPath, logMessage);
            }
            catch
            {
                // ignore logging errors to prevent recursive failure
            }
        }

    }


    private async void FinishDownload(string path)
    {
        StatusMessage = "Preparing to unzip modpack...";

        if (BackupMods)
            _downloader.RenameOldModsFolder(modPath);
        else if (Directory.Exists(modPath))
            Directory.Delete(modPath, true);

        _downloader.EnsureDir(modPath);

        await Task.Delay(20);

        StatusMessage = "Unzipping modpack...";

        _downloader.UnzipModpack(path, modPath);

        await Task.Delay(100);

        StatusMessage = "Modpack installation complete.";

        var box = MessageBoxManager
        .GetMessageBoxStandard("Minecraft Modpack Downloader", $"Modpack downloaded to {path} and unzipped to the mods folder.",
            ButtonEnum.Ok);

        var result = await box.ShowAsync();
        IsDownloading = false;
    }
}
