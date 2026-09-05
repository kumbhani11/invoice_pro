using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Services;
using InvoicePro.UI.Backup;
using InvoicePro.UI.Bills;
using InvoicePro.UI.Customers;
using InvoicePro.UI.NewBill;
using InvoicePro.UI.Products;
using InvoicePro.UI.Reports;
using InvoicePro.UI.Settings;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.Dashboard;

public partial class DashboardViewModel : ViewModelBase
{
    public string CompanyDisplayName => SessionContext.CurrentCompany?.Name ?? "—";

    [RelayCommand] private void NewBill()   => NavigationService.NavigateTo?.Invoke(new NewBillViewModel());
    [RelayCommand] private void Customers() => NavigationService.NavigateTo?.Invoke(new CustomersViewModel());
    [RelayCommand] private void Products()  => NavigationService.NavigateTo?.Invoke(new ProductsViewModel());
    [RelayCommand] private void AllBills()  => NavigationService.NavigateTo?.Invoke(new BillsViewModel());
    [RelayCommand] private void Reports()   => NavigationService.NavigateTo?.Invoke(new ReportsViewModel());
    [RelayCommand] private void Backup()    => NavigationService.NavigateTo?.Invoke(new BackupViewModel());
    [RelayCommand] private void Settings()  => NavigationService.NavigateTo?.Invoke(new SettingsViewModel());
}
