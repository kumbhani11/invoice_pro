using System;
using InvoicePro.Models;

namespace InvoicePro.Services;

public static class SessionContext
{
    private static Company? _currentCompany;
    public static Company? CurrentCompany
    {
        get => _currentCompany;
        set
        {
            if (_currentCompany == value) return;
            _currentCompany = value;
            CurrentCompanyChanged?.Invoke(_currentCompany);
        }
    }

    public static string CurrentDatabasePath { get; set; } = string.Empty;

    public static event Action<Company?>? CurrentCompanyChanged;

    // Raised when customer/product data changes so views can refresh their lists
    public static event Action? CustomersChanged;

    public static void NotifyCustomersChanged() => CustomersChanged?.Invoke();
}
