using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro.UI.Settings;

public partial class SettingsViewModel : ViewModelBase
{
    [ObservableProperty] private Company _currentCompany = new();
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel()
    {
        _ = LoadSettingsAsync();
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        if (SessionContext.CurrentCompany is not null)
        {
            CurrentCompany = SessionContext.CurrentCompany;
            return;
        }

        using var db = new BillingDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();

        if (company != null)
        {
            CurrentCompany = company;
            SessionContext.CurrentCompany = company;
        }
        else
        {
            CurrentCompany = new Company { Name = "My Business" };
            SessionContext.CurrentCompany = CurrentCompany;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        using var db = new BillingDbContext();

        if (CurrentCompany.Id == 0)
        {
            db.Companies.Add(CurrentCompany);
        }
        else
        {
            db.Companies.Update(CurrentCompany);
        }

        await db.SaveChangesAsync();
        SessionContext.CurrentCompany = CurrentCompany;
        NavigationService.SetCompany?.Invoke(CurrentCompany.Name);
        StatusMessage = "Settings saved successfully!";

        // Clear message after a short delay
        _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
