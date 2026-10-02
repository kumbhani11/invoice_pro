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

        // Yield to UI thread so the loading overlay renders before heavy work
        await Task.Yield();

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
                var profile = DummyDataStore.GetCompanyProfile(companyName);

                if (company == null)
                {
                    company = new Company
                    {
                        Name = companyName,
                        RegisteredOffice = profile.AddressLine1,
                        SalesOffice = profile.AddressLine2,
                        Phone = profile.Contact,
                        Email = string.Empty,
                        GSTIN = profile.GSTIN,
                        State = profile.State,
                        StateCode = profile.StateCode,
                        BankName = profile.BankName,
                        BankBranch = profile.BankBranch,
                        BankAccount = profile.BankAccountNo,
                        IFSC = profile.BankIFSC,
                        TermsAndConditions = string.Empty,
                        AuthorizedSignatoryText = string.Empty
                    };
                    db.Companies.Add(company);
                    await db.SaveChangesAsync();
                }
                else
                {
                    bool needsHydration = string.IsNullOrWhiteSpace(company.RegisteredOffice)
                        || string.IsNullOrWhiteSpace(company.SalesOffice)
                        || string.IsNullOrWhiteSpace(company.Phone)
                        || string.IsNullOrWhiteSpace(company.GSTIN)
                        || string.IsNullOrWhiteSpace(company.BankName)
                        || string.IsNullOrWhiteSpace(company.BankAccount)
                        || string.IsNullOrWhiteSpace(company.IFSC);

                    if (needsHydration)
                    {
                        company.Name = companyName;
                        company.RegisteredOffice = string.IsNullOrWhiteSpace(company.RegisteredOffice) ? profile.AddressLine1 : company.RegisteredOffice;
                        company.SalesOffice = string.IsNullOrWhiteSpace(company.SalesOffice) ? profile.AddressLine2 : company.SalesOffice;
                        company.Phone = string.IsNullOrWhiteSpace(company.Phone) ? profile.Contact : company.Phone;
                        company.GSTIN = string.IsNullOrWhiteSpace(company.GSTIN) ? profile.GSTIN : company.GSTIN;
                        company.State = string.IsNullOrWhiteSpace(company.State) ? profile.State : company.State;
                        company.StateCode = string.IsNullOrWhiteSpace(company.StateCode) ? profile.StateCode : company.StateCode;
                        company.BankName = string.IsNullOrWhiteSpace(company.BankName) ? profile.BankName : company.BankName;
                        company.BankBranch = string.IsNullOrWhiteSpace(company.BankBranch) ? profile.BankBranch : company.BankBranch;
                        company.BankAccount = string.IsNullOrWhiteSpace(company.BankAccount) ? profile.BankAccountNo : company.BankAccount;
                        company.IFSC = string.IsNullOrWhiteSpace(company.IFSC) ? profile.BankIFSC : company.IFSC;
                        await db.SaveChangesAsync();
                    }
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
