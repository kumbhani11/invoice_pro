using System;
using InvoicePro.ViewModels;

namespace InvoicePro.UI;

public static class NavigationService
{
    public static Action<ViewModelBase>? NavigateTo { get; set; }
    public static Action<string>? SetStatus { get; set; }
    public static Action<string>? SetCompany { get; set; }
    public static Action<string>? SetInvoiceCount { get; set; }
}
