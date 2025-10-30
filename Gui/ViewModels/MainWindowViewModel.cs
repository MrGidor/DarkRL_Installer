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

        // Only on macOS: try curl first (uses system TLS stack, often more compatible)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "curl",
                    RedirectStandardError = true,   // capture progress/errors
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Progress = 0;
                psi.ArgumentList.Add("-f");   // fail on HTTP errors
                psi.ArgumentList.Add("-L");   // follow redirects
                psi.ArgumentList.Add("-S");   // show errors
                psi.ArgumentList.Add("--retry"); psi.ArgumentList.Add("3"); // retry network issues
                psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(path);    // output file
                psi.ArgumentList.Add(url);

                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start curl.");

                var stderrTask = proc.StandardError.ReadToEndAsync();

                await proc.WaitForExitAsync();
                var stderr = await stderrTask;

                if (proc.ExitCode == 0)
                {
                    FinishDownload(path);

                    return;
                }
                else
                {
                    Console.WriteLine($"curl failed (exit {proc.ExitCode}): {stderr.Trim()}, falling back to HttpClient...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"curl attempt threw: {ex.Message}, falling back to HttpClient...");
            }
        }

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

        FinishDownload(path);

    }

    private async void FinishDownload(string path)
    {
        if (BackupMods)
            _downloader.RenameOldModsFolder(modPath);
        else if (Directory.Exists(modPath))
            Directory.Delete(modPath, true);

        _downloader.EnsureDir(modPath);

        _downloader.UnzipModpack(path, modPath);

        var box = MessageBoxManager
        .GetMessageBoxStandard("Minecraft Modpack Downloader", $"Modpack downloaded to {path} and unzipped to the mods folder.",
            ButtonEnum.Ok);

        var result = await box.ShowAsync();
    }
}
