using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.ViewModels;
using InvoicePro.Views;
using InvoicePro.UI.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro.UI.CompanySelection;

public partial class CompanySelectionViewModel : ViewModelBase
{
    private readonly Window _startupWindow;

    public CompanySelectionViewModel(Window startupWindow)
    {
        _startupWindow = startupWindow;
    }

    private static string GetAppFolder()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Join(path, "InvoicePro");
        if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
        return appFolder;
    }

    [RelayCommand]
    private Task OpenAvaniAsync() => OpenCompanyAsync("AVANI ENTERPRISE", "AVANI_ENTERPRISE.db");

    [RelayCommand]
    private Task OpenHardikaAsync() => OpenCompanyAsync("HARDIKA CREATION", "HARDIKA_CREATION.db");

    private async Task OpenCompanyAsync(string companyName, string dbFileName)
    {
        var dbPath = Path.Join(GetAppFolder(), dbFileName);
        bool isNew = !File.Exists(dbPath);

        BillingDbContext.CurrentDatabasePath = dbPath;
        SessionContext.CurrentDatabasePath = dbPath;

        using (var db = new BillingDbContext())
        {
            db.Database.Migrate();

            if (isNew)
            {
                db.Companies.Add(new Company { Name = companyName });
                await db.SaveChangesAsync();
            }

            SessionContext.CurrentCompany = await db.Companies.FirstOrDefaultAsync();
        }

        var mainVm = new MainViewModel();
        NavigationService.SetCompany?.Invoke(SessionContext.CurrentCompany?.Name ?? companyName);
        NavigationService.SetStatus?.Invoke("Ready");
        NavigationService.NavigateTo?.Invoke(new DashboardViewModel());

        var mainWindow = new MainWindow { DataContext = mainVm };
        mainWindow.Show();
        _startupWindow.Close();
    }
}
