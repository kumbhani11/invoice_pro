using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.NewBill;

public partial class NewBillViewModel : ViewModelBase
{
    // Searchable lists
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private ObservableCollection<Product> _products = new();

    // Bill Header
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private DateTimeOffset _invoiceDate = DateTimeOffset.Now;

    // Item Entry
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _discountAmount = 0;

    // Bill Grid
    public ObservableCollection<InvoiceItem> InvoiceItems { get; } = new();

    // Totals
    [ObservableProperty] private int _totalQuantity;
    [ObservableProperty] private decimal _grossAmount;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _cgstAmount;
    [ObservableProperty] private decimal _sgstAmount;
    [ObservableProperty] private decimal _igstAmount;
    [ObservableProperty] private decimal _roundOff;
    [ObservableProperty] private decimal _netAmount;

    public NewBillViewModel()
    {
        _ = LoadDataAsync();
        InvoiceItems.CollectionChanged += (s, e) => CalculateTotals();
    }

    private async Task LoadDataAsync()
    {
        using var db = new BillingDbContext();
        Customers = new ObservableCollection<Customer>(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Customers));
        Products = new ObservableCollection<Product>(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Products));
    }

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedProduct == null || Quantity <= 0) return;

        var item = new InvoiceItem
        {
            ProductCode = SelectedProduct.ProductCode,
            Description = SelectedProduct.Description,
            Fit = SelectedProduct.Fit,
            Size = SelectedProduct.Size,
            HSN = SelectedProduct.HSN,
            UOM = SelectedProduct.UOM,
            Quantity = Quantity,
            Rate = SelectedProduct.Rate,
            Amount = Quantity * SelectedProduct.Rate
        };

        InvoiceItems.Add(item);
        
        // Reset entry fields for fast sequential entry
        SelectedProduct = null;
        Quantity = 1;
    }

    [RelayCommand]
    private void RemoveItem(InvoiceItem item)
    {
        if (item != null)
        {
            InvoiceItems.Remove(item);
        }
    }

    partial void OnDiscountAmountChanged(decimal value)
    {
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        TotalQuantity = InvoiceItems.Sum(i => i.Quantity);
        GrossAmount = InvoiceItems.Sum(i => i.Amount);
        
        TaxableAmount = GrossAmount - DiscountAmount;
        if (TaxableAmount < 0) TaxableAmount = 0;

        // Base 18% tax calculation logic for now (9% CGST, 9% SGST). 
        // Can be dynamically set based on TaxConfiguration later.
        CgstAmount = Math.Round(TaxableAmount * 0.09m, 2);
        SgstAmount = Math.Round(TaxableAmount * 0.09m, 2);
        IgstAmount = 0m; 

        decimal totalBeforeRound = TaxableAmount + CgstAmount + SgstAmount + IgstAmount;
        NetAmount = Math.Round(totalBeforeRound, 0, MidpointRounding.AwayFromZero);
        RoundOff = NetAmount - totalBeforeRound;
    }

    [RelayCommand]
    private async Task SaveInvoiceAsync()
    {
        if (InvoiceItems.Count == 0) return;

        using var db = new BillingDbContext();
        using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyMMddHHmmss}",
                InvoiceDate = InvoiceDate.DateTime,
                CustomerName = SelectedCustomer?.Name ?? "Cash Customer",
                CustomerGSTIN = SelectedCustomer?.GSTIN ?? "",
                CustomerPhone = SelectedCustomer?.Phone ?? "",
                CustomerAddress = SelectedCustomer?.Address ?? "",
                CustomerState = SelectedCustomer?.State ?? "",
                CustomerStateCode = SelectedCustomer?.StateCode ?? "",
                
                TotalQuantity = TotalQuantity,
                GrossAmount = GrossAmount,
                Discount = DiscountAmount,
                TaxableAmount = TaxableAmount,
                CGST = CgstAmount,
                SGST = SgstAmount,
                IGST = IgstAmount,
                RoundOff = RoundOff,
                NetAmount = NetAmount,
                IsCancelled = false
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(); // Saves and sets invoice.Id

            foreach (var item in InvoiceItems)
            {
                item.InvoiceId = invoice.Id;
                db.InvoiceItems.Add(item);
            }
            
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            ClearForm();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void ClearForm()
    {
        SelectedCustomer = null;
        SelectedProduct = null;
        Quantity = 1;
        DiscountAmount = 0;
        InvoiceItems.Clear();
        InvoiceDate = DateTimeOffset.Now;
    }
}
