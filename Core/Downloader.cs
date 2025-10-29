using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Modpack_Installer.Core;

public class Downloader
{
    public string GetMinecraftDir()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string? appData = Environment.GetEnvironmentVariable("APPDATA");
            if (string.IsNullOrEmpty(appData))
                throw new Exception("APPDATA not set on Windows.");
            return Path.Combine(appData, ".minecraft");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
                throw new Exception("HOME not set on macOS.");
            return Path.Combine(home, "Library", "Application Support", "minecraft");
        }
        else // Linux/Unix
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home))
                throw new Exception("HOME not set on Unix.");
            return Path.Combine(home, ".minecraft");
        }
    }

    public async Task DownloadModpack(string url, string downloadPath)
    {
        Console.WriteLine($"Downloading modpack from {url}...");

        // Ensure target directory exists
        var dir = Path.GetDirectoryName(downloadPath);
        if (!string.IsNullOrEmpty(dir))
            EnsureDir(dir);

        var handler = new SocketsHttpHandler
        {
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls13
            }
        };

        // Only on macOS: try curl first (uses system TLS stack, often more compatible)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "curl",
                    RedirectStandardError = true,
                    RedirectStandardOutput = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // args: -f (fail on HTTP errors), -L (follow redirects), -S (show error), -o <file> <url>
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add("-L");
                psi.ArgumentList.Add("-S");
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(downloadPath);
                psi.ArgumentList.Add(url);

                using var proc = Process.Start(psi);
                if (proc == null)
                    throw new InvalidOperationException("Failed to start curl process.");

                var stderrTask = proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                var stderr = await stderrTask;

                if (proc.ExitCode == 0)
                {
                    Console.WriteLine($"Downloaded to {downloadPath}");
                    return;
                }
                else
                {
                    Console.WriteLine($"curl failed with exit code {proc.ExitCode}. stderr: {stderr.Trim()}");
                    Console.WriteLine("Falling back to HttpClient download...");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // 'curl' not found or cannot be executed
                Console.WriteLine("curl not found in PATH or not executable on this system. Falling back to HttpClient.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"curl attempt threw: {ex.Message}. Falling back to HttpClient.");
            }
        }

        using (var client = new HttpClient(handler))
        using (var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            resp.EnsureSuccessStatusCode();
            using (var fs = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await resp.Content.CopyToAsync(fs);
            }
        }
        Console.WriteLine($"Downloaded to {downloadPath}");
    }

    public void RenameOldModsFolder(string modsFolder)
    {
        string modsOld = Path.Combine(Path.GetDirectoryName(modsFolder)!, "mods-old");
        if (Directory.Exists(modsOld))
        {
            Console.WriteLine("Removing existing mods-old...");
            Directory.Delete(modsOld, true);
        }
        if (Directory.Exists(modsFolder))
        {
            Console.WriteLine("Renaming current mods to mods-old...");
            Directory.Move(modsFolder, modsOld);
        }
    }

    public void CreateNewModsFolder(string modsFolder)
    {
        if (!Directory.Exists(modsFolder))
        {
            Console.WriteLine("Creating new mods folder...");
            Directory.CreateDirectory(modsFolder);
        }
    }

    public void UnzipModpack(string zipPath, string extractTo)
    {
        Console.WriteLine($"Extracting {zipPath} -> {extractTo} ...");
        ZipFile.ExtractToDirectory(zipPath, extractTo, true);
        Console.WriteLine("Extraction complete.");
    }

    public void EnsureDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
