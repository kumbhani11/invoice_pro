using InvoicePro.Models;

namespace InvoicePro.Services;

public static class SessionContext
{
    public static Company? CurrentCompany { get; set; }
    public static string CurrentDatabasePath { get; set; } = string.Empty;
}
