using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.Utils;
using InvoicePro.ViewModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace InvoicePro.UI.NewBill;

public partial class NewBillViewModel : ViewModelBase
{
    private readonly string _companyName;
    private decimal _cgst, _sgst, _roundOff;
    public CompanyProfile CompanyProfile { get; private set; } = new();

    // ── DB customers (left-panel combo) ───────────────────────────────────
    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;

    // ── Left-panel header fields ───────────────────────────────────────────
    [ObservableProperty] private string _invoiceNumber  = $"INV-{DateTime.Now:yyMMddHHmmss}";
    [ObservableProperty] private string _referenceNumber = string.Empty;
    [ObservableProperty] private string _paymentTerms    = string.Empty;
    [ObservableProperty] private DateTimeOffset _invoiceDate = DateTimeOffset.Now;

    // ── Left-panel totals ─────────────────────────────────────────────────
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _taxTotal;
    [ObservableProperty] private decimal _grandTotal;

    // Small helpers exposed for the XAML preview bindings
    [ObservableProperty] private int _totalQuantity;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _cgstAmount;
    [ObservableProperty] private decimal _sgstAmount;
    [ObservableProperty] private decimal _roundOffAmount;

    // ══════════════════════════════════════════════════════════════════════
    // PREVIEW — flat observable properties bound directly by InvoicePreviewView
    // ══════════════════════════════════════════════════════════════════════

    // Company block
    [ObservableProperty] private string _previewCompanyName    = string.Empty;
    [ObservableProperty] private string _previewAddressLine1   = string.Empty;
    [ObservableProperty] private string _previewAddressLine2   = string.Empty;
    [ObservableProperty] private string _previewContact        = string.Empty;
    [ObservableProperty] private string _previewGstin          = string.Empty;

    // Invoice meta
    [ObservableProperty] private string _previewInvoiceNo      = string.Empty;
    [ObservableProperty] private string _previewDate           = string.Empty;
    [ObservableProperty] private string _previewState          = string.Empty;
    [ObservableProperty] private string _previewStateCode      = string.Empty;

    // Billing party
    [ObservableProperty] private string _previewBillName       = string.Empty;
    [ObservableProperty] private string _previewBillAddress    = string.Empty;
    [ObservableProperty] private string _previewBillGstin      = string.Empty;
    [ObservableProperty] private string _previewBillState      = string.Empty;
    [ObservableProperty] private string _previewBillStateCode  = string.Empty;

    // Shipping party
    [ObservableProperty] private string _previewShipName       = string.Empty;
    [ObservableProperty] private string _previewShipAddress    = string.Empty;
    [ObservableProperty] private string _previewShipGstin      = string.Empty;
    [ObservableProperty] private string _previewShipState      = string.Empty;
    [ObservableProperty] private string _previewShipStateCode  = string.Empty;

    // Transport row
    [ObservableProperty] private string _previewReverseCharge  = string.Empty;
    [ObservableProperty] private string _previewTransport      = string.Empty;
    [ObservableProperty] private string _previewDateOfSupply   = string.Empty;
    [ObservableProperty] private string _previewPlaceOfSupply  = string.Empty;

    // Totals block
    [ObservableProperty] private string _previewTotalQty       = string.Empty;
    [ObservableProperty] private string _previewGrossAmount    = string.Empty;
    [ObservableProperty] private string _previewDiscount       = string.Empty;
    [ObservableProperty] private string _previewTaxableAmount  = string.Empty;
    [ObservableProperty] private string _previewCgstRate       = string.Empty;
    [ObservableProperty] private string _previewCgstAmount     = string.Empty;
    [ObservableProperty] private string _previewSgstRate       = string.Empty;
    [ObservableProperty] private string _previewSgstAmount     = string.Empty;
    [ObservableProperty] private string _previewIgstRate       = string.Empty;
    [ObservableProperty] private string _previewIgstAmount     = string.Empty;
    [ObservableProperty] private string _previewRoundOff       = string.Empty;
    [ObservableProperty] private string _previewNetAmount      = string.Empty;
    [ObservableProperty] private string _previewAmountInWords  = string.Empty;
    [ObservableProperty] private string _previewGstLabel       = string.Empty;
    [ObservableProperty] private string _previewGstTotal        = string.Empty;

    // Bank details
    [ObservableProperty] private string _previewBankName       = string.Empty;
    [ObservableProperty] private string _previewBankBranch     = string.Empty;
    [ObservableProperty] private string _previewBankAccount    = string.Empty;
    [ObservableProperty] private string _previewBankIfsc       = string.Empty;

    // Line items shown in the preview ItemsControl
    public ObservableCollection<LineItem> PreviewItems { get; } = new();

    // Line items for the left-panel DataGrid
    public ObservableCollection<InvoiceItemModel> InvoiceItems { get; } = new();

    // ── Constructors ──────────────────────────────────────────────────────
    public NewBillViewModel() : this("AVANI ENTERPRISE") { }

    public NewBillViewModel(string selectedCompanyName)
    {
        _companyName = selectedCompanyName;
        // load static company profile and an initial preview from dummy data
        CompanyProfile = DummyDataStore.GetCompanyProfile(_companyName);
        LoadPreviewFromDummyData();
        _ = LoadCustomersAsync();
        InvoiceItems.CollectionChanged += OnCollectionChanged;
    }

    // ── Dummy data → flat preview properties ─────────────────────────────
    private void LoadPreviewFromDummyData()
    {
        var inv = DummyDataStore.GetDummyInvoice(_companyName);
        var cp  = inv.Company;
        var bp  = inv.BillingParty;
        var sp  = inv.ShippingParty;
        var t   = inv.Totals;

        PreviewCompanyName   = cp.CompanyName;
        PreviewAddressLine1  = cp.AddressLine1;
        PreviewAddressLine2  = cp.AddressLine2;
        PreviewContact       = cp.Contact;
        PreviewGstin         = cp.GSTIN;

        PreviewInvoiceNo     = inv.InvoiceNo;
        PreviewDate          = inv.Date;
        PreviewState         = inv.State;
        PreviewStateCode     = inv.StateCode;

        PreviewBillName      = bp.Name;
        PreviewBillAddress   = bp.Address;
        PreviewBillGstin     = bp.GSTIN;
        PreviewBillState     = bp.State;
        PreviewBillStateCode = bp.StateCode;

        PreviewShipName      = sp.Name;
        PreviewShipAddress   = sp.Address;
        PreviewShipGstin     = sp.GSTIN;
        PreviewShipState     = sp.State;
        PreviewShipStateCode = sp.StateCode;

        PreviewReverseCharge = inv.ReverseCharge;
        PreviewTransport     = inv.Transport;
        PreviewDateOfSupply  = inv.DateOfSupply;
        PreviewPlaceOfSupply = inv.PlaceOfSupply;

        PreviewTotalQty      = t.TotalQty.ToString("0.##");
        PreviewGrossAmount   = t.GrossAmount.ToString("F2");
        PreviewDiscount      = t.Discount.ToString("F2");
        PreviewTaxableAmount = t.TaxableAmount.ToString("F2");
        PreviewCgstRate      = t.CgstRate.ToString("0.#");
        PreviewCgstAmount    = t.CgstAmount.ToString("F2");
        PreviewSgstRate      = t.SgstRate.ToString("0.#");
        PreviewSgstAmount    = t.SgstAmount.ToString("F2");
        PreviewIgstRate      = t.IgstRate > 0 ? t.IgstRate.ToString("0.#") : "";
        PreviewIgstAmount    = t.IgstAmount > 0 ? t.IgstAmount.ToString("F2") : "";
        PreviewRoundOff      = t.RoundOff.ToString("F2");
        PreviewNetAmount     = t.NetAmount.ToString("F2");
        PreviewAmountInWords = t.AmountInWords;
        PreviewGstLabel      = $"GST {t.CgstRate + t.SgstRate + t.IgstRate:0.#}%";
        PreviewGstTotal      = (t.CgstAmount + t.SgstAmount + t.IgstAmount).ToString("F2");

        PreviewBankName      = cp.BankName;
        PreviewBankBranch    = cp.BankBranch;
        PreviewBankAccount   = cp.BankAccountNo;
        PreviewBankIfsc      = cp.BankIFSC;

        PreviewItems.Clear();
        foreach (var item in inv.Items)
            PreviewItems.Add(item);
    }

    // ── DB load ───────────────────────────────────────────────────────────
    private async Task LoadCustomersAsync()
    {
        using var db = new BillingDbContext();
        Customers = new ObservableCollection<Customer>(await db.Customers.ToListAsync());
    }

    // ── Collection wiring ─────────────────────────────────────────────────
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (InvoiceItemModel row in e.NewItems)
                row.PropertyChanged += OnRowChanged;
        if (e.OldItems != null)
            foreach (InvoiceItemModel row in e.OldItems)
                row.PropertyChanged -= OnRowChanged;
        ReindexRows();
        CalculateTotals();
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(InvoiceItemModel.Amount) or nameof(InvoiceItemModel.TaxAmount))
            CalculateTotals();
    }

    private void ReindexRows()
    {
        for (int i = 0; i < InvoiceItems.Count; i++)
            InvoiceItems[i].ItemIndex = i + 1;
    }

    // ── Totals (left panel only — no PDF generation here) ─────────────────
    partial void OnDiscountAmountChanged(decimal value) => CalculateTotals();

    private void CalculateTotals()
    {
        SubTotal = InvoiceItems.Sum(r => r.Amount - r.TaxAmount);
        decimal taxable = Math.Max(SubTotal - DiscountAmount, 0);
        TaxTotal = InvoiceItems.Sum(r => r.TaxAmount);
        _cgst    = Math.Round(TaxTotal / 2, 2);
        _sgst    = _cgst;
        decimal total = taxable + TaxTotal;
        GrandTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        _roundOff  = GrandTotal - total;

        // expose a few helpful read-only preview fields for XAML bindings
        TotalQuantity = InvoiceItems.Sum(r => r.Quantity);
        TaxableAmount = taxable;
        CgstAmount = _cgst;
        SgstAmount = _sgst;
        RoundOffAmount = _roundOff;
        PreviewAmountInWords = NumberToWordsConverter.ConvertAmount(GrandTotal);
    }

    // ── Row commands ──────────────────────────────────────────────────────
    [RelayCommand] private void AddRow()    => InvoiceItems.Add(new InvoiceItemModel());
    [RelayCommand] private void RemoveRow() { if (InvoiceItems.Count > 0) InvoiceItems.RemoveAt(InvoiceItems.Count - 1); }

    // ── Save & Print — QuestPDF runs ONLY here ────────────────────────────
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
                InvoiceNumber     = InvoiceNumber,
                InvoiceDate       = InvoiceDate.DateTime,
                CustomerName      = SelectedCustomer?.Name        ?? "Cash Customer",
                CustomerGSTIN     = SelectedCustomer?.GSTIN       ?? "",
                CustomerPhone     = SelectedCustomer?.Phone       ?? "",
                CustomerAddress   = SelectedCustomer?.Address     ?? "",
                CustomerState     = SelectedCustomer?.State       ?? "",
                CustomerStateCode = SelectedCustomer?.StateCode   ?? "",
                TotalQuantity     = InvoiceItems.Sum(r => r.Quantity),
                GrossAmount       = SubTotal,
                Discount          = DiscountAmount,
                TaxableAmount     = taxable,
                CGST              = _cgst,
                SGST              = _sgst,
                IGST              = 0,
                RoundOff          = _roundOff,
                NetAmount         = GrandTotal,
                IsCancelled       = false
            };

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            foreach (var row in InvoiceItems)
            {
                db.InvoiceItems.Add(new InvoiceItem
                {
                    InvoiceId   = invoice.Id,
                    ProductCode = row.ProductCode,
                    Description = row.Description,
                    HSN         = row.HSN,
                    Quantity    = row.Quantity,
                    Rate        = row.Rate,
                    Amount      = row.Amount
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // ── Generate PDF after successful DB save ──────────────────
            GenerateAndSavePdf(invoice.InvoiceNumber);

            NavigationService.SetStatus?.Invoke($"Invoice {invoice.InvoiceNumber} saved.");
            ClearForm();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private void GenerateAndSavePdf(string invoiceNumber)
    {
        try
        {
            var model = DummyDataStore.GetDummyInvoice(_companyName);
            model.InvoiceNo = invoiceNumber;
            var doc  = new InvoiceDocument(model);
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"Invoice_{invoiceNumber}.pdf");
            doc.GeneratePdf(path);
            NavigationService.SetStatus?.Invoke($"PDF saved: {path}");
        }
        catch { /* PDF failure must not block the save */ }
    }

    // ── Clear ─────────────────────────────────────────────────────────────
    [RelayCommand]
    private void ClearForm()
    {
        SelectedCustomer = null;
        DiscountAmount   = 0;
        InvoiceItems.Clear();
        InvoiceDate      = DateTimeOffset.Now;
        InvoiceNumber    = $"INV-{DateTime.Now:yyMMddHHmmss}";
        ReferenceNumber  = string.Empty;
        PaymentTerms     = string.Empty;
    }
}
