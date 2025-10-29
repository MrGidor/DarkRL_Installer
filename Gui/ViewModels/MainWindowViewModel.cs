using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Modpack_Installer.Core;
using System;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Modpack_Installer.Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Downloader _downloader = new();

    private string modPath = string.Empty;

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


            string? installerDir = null;

            string mcDir = _downloader.GetMinecraftDir();
            string instDir = installerDir ?? Path.Combine(mcDir, "modpack_installer");
            string modsFolder = Path.Combine(mcDir, "mods");
            modPath = modsFolder;
            string zipPath = Path.Combine(instDir, "modpack.zip");

            // Downloader
            await DownloadWithProgress(ModpackUrl, zipPath);
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
        finally
        {
            IsDownloading = false;
        }
    }

    private async Task DownloadWithProgress(string url, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        using var sourceStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        // Wrap the source stream with a progress-tracking loop
        while ((read = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;

            if (total > 0)
                Progress = Math.Round((double)totalRead / total * 100, 1); 
        }

        await fileStream.FlushAsync();

        fileStream.Close();
        sourceStream.Close();

        Progress = 100;

        if (BackupMods)
            _downloader.RenameOldModsFolder(modPath);
        else if (Directory.Exists(modPath))
            Directory.Delete(modPath, true);

        _downloader.EnsureDir(modPath);

        int retries = 3;
        while (true)
        {
            try
            {
                _downloader.UnzipModpack(path, modPath);
                break;
            }
            catch (IOException)
            {
                if (retries-- == 0) throw;
                await Task.Delay(200);
            }
        }
    }


}
