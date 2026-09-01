using System;
using InvoicePro.ViewModels;

namespace InvoicePro.UI;

public static class NavigationService
{
    public static Action<ViewModelBase>? NavigateTo { get; set; }
}
