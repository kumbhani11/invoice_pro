using System;
using InvoicePro.ViewModels;

namespace InvoicePro.UI;

public static class NavigationService
{
    public static Action<ViewModelBase>? NavigateTo { get; set; }
    public static Action<string>? SetStatus { get; set; }
    public static Action<string>? SetCompany { get; set; }
    public static Action<string>? SetInvoiceCount { get; set; }

    // Open company selection dialog (used for switch-company flow). Assigned by MainWindow.
    public static Func<System.Threading.Tasks.Task>? OpenCompanySelection { get; set; }
}
