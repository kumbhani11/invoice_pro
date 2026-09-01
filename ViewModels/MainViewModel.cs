using CommunityToolkit.Mvvm.ComponentModel;
using InvoicePro.UI;
using InvoicePro.UI.CompanySelection;

namespace InvoicePro.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainViewModel()
    {
        _currentPage = new CompanySelectionViewModel();
        NavigationService.NavigateTo = (page) => CurrentPage = page;
    }
}
