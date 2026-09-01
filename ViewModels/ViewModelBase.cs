using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.UI;

namespace InvoicePro.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [RelayCommand]
    protected void GoToDashboard()
    {
        NavigationService.NavigateTo?.Invoke(new InvoicePro.UI.Dashboard.DashboardViewModel());
    }
}
