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
using InvoicePro.Utils;

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

    private static void Log(string message)
    {
        try
        {
            var logPath = Path.Join(GetAppFolder(), "invoicepro.log");
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch { }
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
        Log($"OpenCompanyAsync START: company={companyName} db={dbFileName}");

        // Yield to UI thread so the loading overlay renders before heavy work
        await Task.Yield();

        try
        {
            var dbPath = Path.Join(GetAppFolder(), dbFileName);
            Log($"DB path: {dbPath}  exists={File.Exists(dbPath)}");

            BillingDbContext.CurrentDatabasePath = dbPath;
            SessionContext.CurrentDatabasePath = dbPath;
            Log("BillingDbContext.CurrentDatabasePath set");

            using (var db = new BillingDbContext())
            {
                Log("Calling MigrateAsync...");
                await db.Database.MigrateAsync();
                Log("MigrateAsync done");

                var company = await db.Companies.FirstOrDefaultAsync();
                Log($"Company from DB: {company?.Name ?? "NULL"}");
                var profile = DummyDataStore.GetCompanyProfile(companyName);

                if (company == null)
                {
                    Log("Creating new company record");
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
                    Log("New company saved");
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
                        Log("Hydrating company fields from profile");
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
                        Log("Hydration saved");
                    }
                }

                SessionContext.CurrentCompany = company;
                Log($"SessionContext.CurrentCompany set to: {company.Name}");
            }

            NavigationService.SetCompany?.Invoke(SessionContext.CurrentCompany?.Name ?? companyName);
            NavigationService.SetStatus?.Invoke("Ready");
            Log($"_createMainWindowOnSelect={_createMainWindowOnSelect}");

            if (_createMainWindowOnSelect)
            {
                Log("Creating MainViewModel and MainWindow");
                var mainVm = new MainViewModel(companyName);
                var mainWindow = new MainWindow { DataContext = mainVm };
                mainWindow.Show();
                Log("MainWindow shown, closing startup window");
                _startupWindow.Close();
                Log("Startup window closed");
            }
            else
            {
                Log("Switch-company path: closing dialog");
                _startupWindow.Close();
            }
        }
        catch (Exception ex)
        {
            var msg = $"EXCEPTION in OpenCompanyAsync({companyName}): {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Log(msg);
            ErrorMessage = $"Unable to load {companyName}. Please try again.\n{ex.Message}";
            NavigationService.SetStatus?.Invoke($"Company load failed: {companyName}");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = string.Empty;
            Log($"OpenCompanyAsync END: company={companyName}");
        }
    }
}
