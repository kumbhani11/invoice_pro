using Avalonia;
using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using InvoicePro.Data.SQLite;

namespace InvoicePro;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "InvoicePro");
        Directory.CreateDirectory(appFolder);
        BillingDbContext.CurrentDatabasePath = Path.Combine(appFolder, "billing.db");

        var runTests = Environment.GetEnvironmentVariable("INVOICEPRO_RUN_TESTS");
        if (!string.IsNullOrEmpty(runTests) && runTests != "0")
        {
            // Run internal tests and exit
            var code = TestRunner.RunAll();
            Environment.Exit(code);
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
