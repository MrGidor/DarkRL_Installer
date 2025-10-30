// -----------------------------------------------------------------------------
// File: Program.cs (CLI Version)
// Author: Gidor 
// Description: Handles the terminal version of the modpack installer
// License: MIT License (see LICENSE file in the project root for details)
// -----------------------------------------------------------------------------

using Modpack_Installer.Core;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
namespace Modpack_Installer.Cli;

public class Program
{

    // move to cli
    static async Task<int> Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

        var MDownloader = new Downloader();

        string url = ""; 
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
            mcDir = MDownloader.GetMinecraftDir();
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
            MDownloader.EnsureDir(instDir);

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

            await MDownloader.DownloadModpack(url, zipPath);
            MDownloader.RenameOldModsFolder(modsFolder);
            MDownloader.CreateNewModsFolder(modsFolder);
            MDownloader.UnzipModpack(zipPath, modsFolder);

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