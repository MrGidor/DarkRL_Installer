using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class Program
{
    const string DEFAULT_URL = "https://mrgidor.github.io/downloads/DarkRL-Modpack.zip";

    static string GetMinecraftDir()
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

    static async Task DownloadModpack(string url, string downloadPath)
    {
        Console.WriteLine($"Downloading modpack from {url}...");
        using (var client = new HttpClient())
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

    static void RenameOldModsFolder(string modsFolder)
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

    static void CreateNewModsFolder(string modsFolder)
    {
        if (!Directory.Exists(modsFolder))
        {
            Console.WriteLine("Creating new mods folder...");
            Directory.CreateDirectory(modsFolder);
        }
    }

    static void UnzipModpack(string zipPath, string extractTo)
    {
        Console.WriteLine($"Extracting {zipPath} -> {extractTo} ...");
        ZipFile.ExtractToDirectory(zipPath, extractTo, true);
        Console.WriteLine("Extraction complete.");
    }

    static void EnsureDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    static async Task<int> Main(string[] args)
    {
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
        string url = DEFAULT_URL;
        bool yes = false;
        string? installerDir = null;

        // Simple argument parsing
        for (int i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "--url":
                case "-u":
                    if (i + 1 < args.Length) url = args[++i];
                    break;
                case "--yes":
                case "-y":
                    yes = true;
                    break;
                case "--installer-dir":
                    if (i + 1 < args.Length) installerDir = args[++i];
                    break;
            }
        }

        string mcDir;
        try
        {
            mcDir = GetMinecraftDir();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to determine Minecraft directory: {e.Message}");
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
            return 1;
        }

        string instDir = installerDir ?? Path.Combine(mcDir, "modpack_installer");
        string modsFolder = Path.Combine(mcDir, "mods");
        string zipPath = Path.Combine(instDir, "modpack.zip");

        try
        {
            EnsureDir(instDir);

            if (Directory.Exists(modsFolder) && !yes)
            {
                Console.Write($"Existing mods folder found at {modsFolder}. Rename to mods-old and install new mods? [y/N]: ");
                string? resp = Console.ReadLine()?.Trim().ToLower();
                if (resp.ToLower() != "y" && resp.ToLower() != "yes")
                {
                    Console.WriteLine("Aborted by user.");
                    return 0;
                }
            }

            await DownloadModpack(url, zipPath);
            RenameOldModsFolder(modsFolder);
            CreateNewModsFolder(modsFolder);
            UnzipModpack(zipPath, modsFolder);

            Console.WriteLine("Installation complete. You may now start Forge/Fabric.");
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
            return 0;
        }
        catch (HttpRequestException he)
        {
            Console.WriteLine($"Network error: {he.Message}");
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
            return 2;
        }
        catch (InvalidDataException)
        {
            Console.WriteLine("Downloaded file is not a valid zip.");
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
            return 3;
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
            Console.WriteLine("Press any key to close the app...");
            Console.ReadKey();
            return 4;
        }
    }
}