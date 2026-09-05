using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.UI;
using InvoicePro.UI.Backup;
using InvoicePro.UI.Bills;
using InvoicePro.UI.Customers;
using InvoicePro.UI.NewBill;
using InvoicePro.UI.Products;
using InvoicePro.UI.Reports;
using InvoicePro.UI.Settings;

namespace InvoicePro.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentPage;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _companyName = "—";
    [ObservableProperty] private string _invoiceCountText = string.Empty;

    // Tab active states (Dashboard removed)
    [ObservableProperty] private bool _isNewBillActive;
    [ObservableProperty] private bool _isBillsActive;
    [ObservableProperty] private bool _isCustomersActive;
    [ObservableProperty] private bool _isProductsActive;
    [ObservableProperty] private bool _isReportsActive;
    [ObservableProperty] private bool _isCompanyActive;

    public MainViewModel(string companyName = "AVANI ENTERPRISE")
    {
        _currentPage = new NewBillViewModel(companyName);
        _companyName = companyName;
        NavigationService.NavigateTo    = Navigate;
        NavigationService.SetStatus     = text => StatusText = text;
        NavigationService.SetCompany    = name => CompanyName = name;
        NavigationService.SetInvoiceCount = text => InvoiceCountText = text;
        IsNewBillActive = true;
    }

    private string _selectedCompanyName = "AVANI ENTERPRISE";

    private void Navigate(ViewModelBase page)
    {
        CurrentPage       = page;
        IsNewBillActive   = page is NewBillViewModel;
        IsBillsActive     = page is BillsViewModel;
        IsCustomersActive = page is CustomersViewModel;
        IsProductsActive  = page is ProductsViewModel;
        IsReportsActive   = page is ReportsViewModel;
        IsCompanyActive   = page is SettingsViewModel;
    }

    [RelayCommand] private void NewBill()   => Navigate(new NewBillViewModel(_selectedCompanyName));
    [RelayCommand] private void AllBills()  => Navigate(new BillsViewModel());
    [RelayCommand] private void Customers() => Navigate(new CustomersViewModel());
    [RelayCommand] private void Products()  => Navigate(new ProductsViewModel());
    [RelayCommand] private void Reports()   => Navigate(new ReportsViewModel());
    [RelayCommand] private void Company()   => Navigate(new SettingsViewModel());
    [RelayCommand] private void Backup()    => Navigate(new BackupViewModel());
    [RelayCommand] private void Settings()  => Navigate(new SettingsViewModel());
    [RelayCommand] private void Exit()      => Environment.Exit(0);
}
