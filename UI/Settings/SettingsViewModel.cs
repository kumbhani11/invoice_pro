using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
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
        using var db = new BillingDbContext();
        var company = await db.Companies.FirstOrDefaultAsync();
        
        if (company != null)
        {
            CurrentCompany = company;
        }
        else
        {
            CurrentCompany = new Company { Name = "My Business" };
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
        StatusMessage = "Settings saved successfully!";
        
        // Clear message after a short delay
        _ = Task.Delay(3000).ContinueWith(_ => StatusMessage = string.Empty, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
