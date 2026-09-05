using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro.UI.NewBill;

public partial class NewBillViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();

    // Header
    [ObservableProperty] private string _invoiceNumber = $"INV-{DateTime.Now:yyMMddHHmmss}";
    [ObservableProperty] private string _referenceNumber = string.Empty;
    [ObservableProperty] private string _paymentTerms = string.Empty;
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private DateTimeOffset _invoiceDate = DateTimeOffset.Now;

    // Discount
    [ObservableProperty] private decimal _discountAmount;

    // Totals
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _taxTotal;
    [ObservableProperty] private decimal _grandTotal;

    private decimal _cgst, _sgst, _roundOff;

    public ObservableCollection<InvoiceItem> InvoiceItems { get; } = new();

    public NewBillViewModel()
    {
        _ = LoadDataAsync();
        InvoiceItems.CollectionChanged += (_, _) => CalculateTotals();
    }

    private async Task LoadDataAsync()
    {
        using var db = new BillingDbContext();
        Customers = new ObservableCollection<Customer>(await db.Customers.ToListAsync());
    }

    [RelayCommand]
    private void AddRow() => InvoiceItems.Add(new InvoiceItem { Quantity = 1 });

    [RelayCommand]
    private void RemoveRow()
    {
        if (InvoiceItems.Count > 0)
            InvoiceItems.RemoveAt(InvoiceItems.Count - 1);
    }

    partial void OnDiscountAmountChanged(decimal value) => CalculateTotals();

    private void CalculateTotals()
    {
        SubTotal = InvoiceItems.Sum(i => i.Amount);
        decimal taxable = Math.Max(SubTotal - DiscountAmount, 0);
        _cgst = Math.Round(taxable * 0.09m, 2);
        _sgst = Math.Round(taxable * 0.09m, 2);
        TaxTotal = _cgst + _sgst;
        decimal total = taxable + TaxTotal;
        GrandTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        _roundOff = GrandTotal - total;
    }

    [RelayCommand]
    private async Task SaveInvoiceAsync()
    {
        if (InvoiceItems.Count == 0) return;

        using var db = new BillingDbContext();
        using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            decimal taxable = Math.Max(SubTotal - DiscountAmount, 0);
            var invoice = new Invoice
            {
                InvoiceNumber   = InvoiceNumber,
                InvoiceDate     = InvoiceDate.DateTime,
                CustomerName    = SelectedCustomer?.Name    ?? "Cash Customer",
                CustomerGSTIN   = SelectedCustomer?.GSTIN   ?? "",
                CustomerPhone   = SelectedCustomer?.Phone   ?? "",
                CustomerAddress = SelectedCustomer?.Address ?? "",
                CustomerState   = SelectedCustomer?.State   ?? "",
                CustomerStateCode = SelectedCustomer?.StateCode ?? "",
                TotalQuantity   = InvoiceItems.Sum(i => i.Quantity),
                GrossAmount     = SubTotal,
                Discount        = DiscountAmount,
                TaxableAmount   = taxable,
                CGST            = _cgst,
                SGST            = _sgst,
                IGST            = 0,
                RoundOff        = _roundOff,
                NetAmount       = GrandTotal,
                IsCancelled     = false
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            foreach (var item in InvoiceItems)
            {
                item.InvoiceId = invoice.Id;
                db.InvoiceItems.Add(item);
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            NavigationService.SetStatus?.Invoke($"Invoice {invoice.InvoiceNumber} saved.");
            ClearForm();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedCustomer = null;
        DiscountAmount   = 0;
        InvoiceItems.Clear();
        InvoiceDate    = DateTimeOffset.Now;
        InvoiceNumber  = $"INV-{DateTime.Now:yyMMddHHmmss}";
        ReferenceNumber = string.Empty;
        PaymentTerms    = string.Empty;
    }
}
