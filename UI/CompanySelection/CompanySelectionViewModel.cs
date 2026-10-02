using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _loadingMessage = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

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
        if (IsLoading)
            return;

        IsLoading = true;
        LoadingMessage = $"Loading {companyName}...";
        ErrorMessage = string.Empty;

        try
        {
            var dbPath = Path.Join(GetAppFolder(), dbFileName);
            bool isNew = !File.Exists(dbPath);

            BillingDbContext.CurrentDatabasePath = dbPath;
            SessionContext.CurrentDatabasePath = dbPath;

            using (var db = new BillingDbContext())
            {
                await db.Database.MigrateAsync();

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
                _startupWindow.Close();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load {companyName}. Please try again. {ex.Message}";
            NavigationService.SetStatus?.Invoke($"Company load failed: {companyName}");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
        }
    }
}
