using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using InvoicePro.Data.SQLite;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.Reports;

public partial class ReportsViewModel : ViewModelBase
{
    [ObservableProperty] private DateTimeOffset _startDate = new DateTimeOffset(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, TimeSpan.Zero);
    [ObservableProperty] private DateTimeOffset _endDate = DateTimeOffset.Now;

    [ObservableProperty] private int _totalBills;
    [ObservableProperty] private decimal _totalGross;
    [ObservableProperty] private decimal _totalTax;
    [ObservableProperty] private decimal _totalNet;

    public ReportsViewModel()
    {
        _ = GenerateReportAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        using var db = new BillingDbContext();
        
        var start = StartDate.Date;
        var end = EndDate.Date.AddDays(1).AddTicks(-1); // End of the selected day

        var invoices = await db.Invoices
            .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end && !i.IsCancelled)
            .ToListAsync();

        TotalBills = invoices.Count;
        TotalGross = invoices.Sum(i => i.GrossAmount);
        TotalTax = invoices.Sum(i => i.CGST + i.SGST + i.IGST);
        TotalNet = invoices.Sum(i => i.NetAmount);
    }
}
