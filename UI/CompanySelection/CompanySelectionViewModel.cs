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
using Microsoft.EntityFrameworkCore;

namespace InvoicePro.UI.CompanySelection;

public partial class CompanySelectionViewModel : ViewModelBase
{
    private readonly Window _startupWindow;
    private readonly bool _createMainWindowOnSelect;

    public CompanySelectionViewModel(Window startupWindow, bool createMainWindowOnSelect = true)
    {
        _startupWindow = startupWindow;
        _createMainWindowOnSelect = createMainWindowOnSelect;
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

            var company = await db.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                company = new Company { Name = companyName };
                db.Companies.Add(company);
                await db.SaveChangesAsync();
            }

            SessionContext.CurrentCompany = company;
        }

        NavigationService.SetCompany?.Invoke(SessionContext.CurrentCompany?.Name ?? companyName);
        NavigationService.SetStatus?.Invoke("Ready");

        if (_createMainWindowOnSelect)
        {
            var mainVm = new MainViewModel(companyName);
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.Show();
            _startupWindow.Close();
        }
        else
        {
            // Close only the selection dialog; main app remains running and will react to SessionContext.CurrentCompanyChanged
            _startupWindow.Close();
        }
    }
}
