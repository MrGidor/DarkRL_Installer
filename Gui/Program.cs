using Avalonia;
using Modpack_Installer.Gui;
using System;

namespace Modpack_Installer.Gui;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect() // Make sure you have 'using Avalonia;' at the top and reference Avalonia.Desktop
            .WithInterFont()
            .LogToTrace();
}

