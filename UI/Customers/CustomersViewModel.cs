using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.Customers;

public partial class CustomersViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Customer> _customersList = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private Customer _currentCustomer = new();

    public CustomersViewModel()
    {
        _ = LoadCustomersAsync();
    }

    [RelayCommand]
    private async Task LoadCustomersAsync()
    {
        using var db = new BillingDbContext();
        var query = db.Customers.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(c => c.Name.Contains(SearchText) || c.GSTIN.Contains(SearchText) || c.Phone.Contains(SearchText));
        }
            
        CustomersList = new ObservableCollection<Customer>(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query));
    }

    async partial void OnSearchTextChanged(string value)
    {
        await LoadCustomersAsync();
    }

    [RelayCommand]
    private void AddNew()
    {
        CurrentCustomer = new Customer();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedCustomer == null) return;
        
        CurrentCustomer = new Customer
        {
            Id = SelectedCustomer.Id,
            Name = SelectedCustomer.Name,
            GSTIN = SelectedCustomer.GSTIN,
            Phone = SelectedCustomer.Phone,
            Address = SelectedCustomer.Address,
            State = SelectedCustomer.State,
            StateCode = SelectedCustomer.StateCode
        };
        IsEditing = true;
    }

    [RelayCommand]
    private void EditCustomer(Customer? customer)
    {
        if (customer == null) return;

        CurrentCustomer = new Customer
        {
            Id = customer.Id,
            Name = customer.Name,
            GSTIN = customer.GSTIN,
            Phone = customer.Phone,
            Address = customer.Address,
            State = customer.State,
            StateCode = customer.StateCode
        };
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentCustomer?.Name))
        {
            NavigationService.SetStatus?.Invoke("Customer name is required.");
            return;
        }

        try
        {
            using var db = new BillingDbContext();
            if (CurrentCustomer.Id == 0)
            {
                db.Customers.Add(CurrentCustomer);
            }
            else
            {
                db.Customers.Update(CurrentCustomer);
            }
            await db.SaveChangesAsync();

            IsEditing = false;
            await LoadCustomersAsync();
            // notify others (e.g., NewBill) that customers changed
            SessionContext.NotifyCustomersChanged();
            NavigationService.SetStatus?.Invoke("Customer saved.");
        }
        catch (System.Exception ex)
        {
            NavigationService.SetStatus?.Invoke($"Failed to save customer: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (SelectedCustomer == null) return;
        await DeleteCustomer(SelectedCustomer);
    }

    [RelayCommand]
    private async Task DeleteCustomer(Customer? customer)
    {
        if (customer == null) return;
        try
        {
            using var db = new BillingDbContext();
            // Attach a stub entity to ensure deletion works even if detached
            var stub = new Customer { Id = customer.Id };
            db.Entry(stub).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
            await db.SaveChangesAsync();
            await LoadCustomersAsync();
            SessionContext.NotifyCustomersChanged();
            NavigationService.SetStatus?.Invoke("Customer deleted.");
        }
        catch (System.Exception ex)
        {
            NavigationService.SetStatus?.Invoke($"Failed to delete customer: {ex.Message}");
        }
    }
}
