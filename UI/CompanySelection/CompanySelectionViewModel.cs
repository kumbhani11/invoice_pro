using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.UI.Dashboard;
using InvoicePro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro.UI.CompanySelection;

public partial class CompanySelectionViewModel : ViewModelBase
{
    private string GetAppFolder()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var appFolder = Path.Join(path, "InvoicePro");
        if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
        return appFolder;
    }

    [RelayCommand]
    private async Task OpenAvaniAsync()
    {
        await OpenCompanyAsync("AVANI ENTERPRISE", "AVANI_ENTERPRISE.db");
    }

    [RelayCommand]
    private async Task OpenHardikaAsync()
    {
        await OpenCompanyAsync("HARDIKA CREATION", "HARDIKA_CREATION.db");
    }

    private async Task OpenCompanyAsync(string companyName, string dbFileName)
    {
        var folder = GetAppFolder();
        var dbPath = Path.Join(folder, dbFileName);
        
        bool isNew = !File.Exists(dbPath);
        
        BillingDbContext.CurrentDatabasePath = dbPath;

        using (var db = new BillingDbContext())
        {
            db.Database.Migrate();
            
            if (isNew)
            {
                db.Companies.Add(new Company { Name = companyName });
                await db.SaveChangesAsync();
            }
        }

        NavigationService.NavigateTo?.Invoke(new DashboardViewModel());
    }
}
