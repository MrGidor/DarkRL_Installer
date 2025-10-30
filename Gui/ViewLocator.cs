// -----------------------------------------------------------------------------
// File: ViewLocator.cs
// Author: Gidor 
// Description: Locates and creates views for view models
// License: MIT License (see LICENSE file in the project root for details)
// -----------------------------------------------------------------------------


using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Modpack_Installer.Gui.ViewModels;

namespace Modpack_Installer.Gui;

public class ViewLocator : IDataTemplate
{

    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
