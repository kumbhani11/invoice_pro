using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using InvoicePro.ViewModels;

namespace InvoicePro;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var viewName = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Assembly.GetEntryAssembly()?.GetType(viewName);

        if (type != null)
            return (Control)Activator.CreateInstance(type)!;

        return new TextBlock { Text = "View not found: " + viewName };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
