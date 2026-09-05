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
    private readonly string _defaultCompanyName;
    private decimal _cgst, _sgst, _roundOff;

    // ── Company & Customer ────────────────────────────────────────────────
    [ObservableProperty] private CompanyProfile _selectedCompany = new();
    [ObservableProperty] private string _companyName     = string.Empty;
    [ObservableProperty] private string _companyAddress1 = string.Empty;
    [ObservableProperty] private string _companyAddress2 = string.Empty;
    [ObservableProperty] private string _companyPhone    = string.Empty;
    [ObservableProperty] private string _companyGstin    = string.Empty;
    [ObservableProperty] private string _companyState    = string.Empty;
    [ObservableProperty] private string _companyStateCode = string.Empty;
    [ObservableProperty] private ObservableCollection<CustomerModel> _customers = new();
    [ObservableProperty] private CustomerModel? _selectedCustomer;

    // ── Invoice info ──────────────────────────────────────────────────────
    [ObservableProperty] private string _invoiceNumber   = $"INV-{DateTime.Now:yyMMddHHmmss}";
    [ObservableProperty] private DateTimeOffset _invoiceDate = DateTimeOffset.Now;
    [ObservableProperty] private string _invoiceState     = string.Empty;
    [ObservableProperty] private string _invoiceStateCode = string.Empty;

    // ── Transport ─────────────────────────────────────────────────────────
    [ObservableProperty] private string _reverseCharge   = "No";
    [ObservableProperty] private string _transportName   = string.Empty;
    [ObservableProperty] private DateTimeOffset _dateOfSupply = DateTimeOffset.Now;
    [ObservableProperty] private string _placeOfSupply   = string.Empty;

    // ── Totals ────────────────────────────────────────────────────────────
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private decimal _subTotal;
    [ObservableProperty] private decimal _taxTotal;
    [ObservableProperty] private decimal _grandTotal;
    [ObservableProperty] private decimal _gstPercentage  = 5m;
    [ObservableProperty] private int     _totalQuantity;
    [ObservableProperty] private decimal _taxableAmount;
    [ObservableProperty] private decimal _cgstAmount;
    [ObservableProperty] private decimal _sgstAmount;
    [ObservableProperty] private decimal _roundOffAmount;

    // ── Preview — flat properties bound by InvoicePreviewView ─────────────
    [ObservableProperty] private string _previewCompanyName   = string.Empty;
    [ObservableProperty] private string _previewReverseCharge = string.Empty;
    [ObservableProperty] private string _previewTransport     = string.Empty;
    [ObservableProperty] private string _previewDateOfSupply  = string.Empty;
    [ObservableProperty] private string _previewPlaceOfSupply = string.Empty;
    [ObservableProperty] private string _previewCgstRate      = string.Empty;
    [ObservableProperty] private string _previewCgstAmount    = string.Empty;
    [ObservableProperty] private string _previewSgstRate      = string.Empty;
    [ObservableProperty] private string _previewSgstAmount    = string.Empty;
    [ObservableProperty] private string _previewIgstRate      = string.Empty;
    [ObservableProperty] private string _previewIgstAmount    = string.Empty;
    [ObservableProperty] private string _previewAmountInWords = string.Empty;
    [ObservableProperty] private string _previewGstLabel      = string.Empty;

    // Line items
    public ObservableCollection<InvoiceItemModel> InvoiceItems { get; } = new();

    // ── Constructors ──────────────────────────────────────────────────────
    public NewBillViewModel() : this("AVANI ENTERPRISE") { }

    public NewBillViewModel(string selectedCompanyName)
    {
        _defaultCompanyName = selectedCompanyName;
        SelectedCompany = DummyDataStore.GetCompanyProfile(selectedCompanyName);
        PreviewCompanyName = SelectedCompany.CompanyName;
        Customers = new ObservableCollection<CustomerModel>(DummyDataStore.Customers);
        InvoiceItems.CollectionChanged += OnCollectionChanged;
        _ = LoadCompanyFromDbAsync();
    }

    private async Task LoadCompanyFromDbAsync()
    {
        try
        {
            Company? company = SessionContext.CurrentCompany;

            if (company == null)
            {
                using var db = new BillingDbContext();
                company = await db.Companies.FirstOrDefaultAsync();
                if (company != null)
                    SessionContext.CurrentCompany = company;
            }

            if (company != null)
            {
                CompanyName      = company.Name;
                CompanyAddress1  = company.RegisteredOffice;
                CompanyAddress2  = company.SalesOffice;
                CompanyPhone     = company.Phone;
                CompanyGstin     = company.GSTIN;
                CompanyState     = company.State;
                CompanyStateCode = company.StateCode;
                PreviewCompanyName = company.Name;

                SelectedCompany = new CompanyProfile
                {
                    Id            = company.Id,
                    CompanyName   = company.Name,
                    AddressLine1  = company.RegisteredOffice,
                    AddressLine2  = company.SalesOffice,
                    Contact       = company.Phone,
                    GSTIN         = company.GSTIN,
                    BankName      = company.BankName,
                    BankBranch    = company.BankBranch,
                    BankAccountNo = company.BankAccount,
                    BankIFSC      = company.IFSC
                };
                return;
            }

            CompanyName      = SelectedCompany.CompanyName;
            CompanyAddress1  = SelectedCompany.AddressLine1;
            CompanyAddress2  = SelectedCompany.AddressLine2;
            CompanyPhone     = SelectedCompany.Contact;
            CompanyGstin     = SelectedCompany.GSTIN;
            CompanyState     = string.Empty;
            CompanyStateCode = string.Empty;
            PreviewCompanyName = SelectedCompany.CompanyName;
        }
        catch { /* fall back to DummyDataStore values already set */ }
    }

    // ── Auto-fill when customer is selected ───────────────────────────────
    partial void OnSelectedCustomerChanged(CustomerModel? value)
    {
        if (value is null) return;
        InvoiceState     = value.State;
        InvoiceStateCode = value.StateCode;
        PlaceOfSupply    = value.State;
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
        if (e.PropertyName is nameof(InvoiceItemModel.Amount))
            CalculateTotals();
    }

    private void ReindexRows()
    {
        for (int i = 0; i < InvoiceItems.Count; i++)
            InvoiceItems[i].SNo = i + 1;
    }

    // ── Totals ────────────────────────────────────────────────────────────
    partial void OnDiscountAmountChanged(decimal value) => CalculateTotals();
    partial void OnGstPercentageChanged(decimal value)  => CalculateTotals();

    private void CalculateTotals()
    {
        SubTotal = InvoiceItems.Sum(r => r.Amount);
        decimal taxable  = Math.Max(SubTotal - DiscountAmount, 0);
        decimal halfRate = GstPercentage / 2m;
        _cgst    = Math.Round(taxable * halfRate / 100m, 2);
        _sgst    = _cgst;
        TaxTotal = _cgst + _sgst;
        decimal total = taxable + TaxTotal;
        GrandTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        _roundOff  = GrandTotal - total;

        TotalQuantity        = (int)InvoiceItems.Sum(r => r.Qty);
        TaxableAmount        = taxable;
        CgstAmount           = _cgst;
        SgstAmount           = _sgst;
        RoundOffAmount       = _roundOff;
        PreviewCgstRate      = halfRate.ToString("0.#");
        PreviewCgstAmount    = _cgst.ToString("F2");
        PreviewSgstRate      = halfRate.ToString("0.#");
        PreviewSgstAmount    = _sgst.ToString("F2");
        PreviewIgstRate      = string.Empty;
        PreviewIgstAmount    = string.Empty;
        PreviewGstLabel      = $"GST {GstPercentage:0.#}%";
        PreviewAmountInWords = NumberToWordsConverter.ConvertAmount(GrandTotal);
    }

    // ── Preview toggle ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _isPreviewVisible = false;
    [RelayCommand] private void TogglePreview() => IsPreviewVisible = !IsPreviewVisible;

    // ── Row commands ──────────────────────────────────────────────────────
    [RelayCommand] private void AddRow()    => InvoiceItems.Add(new InvoiceItemModel());
    [RelayCommand] private void RemoveRow() { if (InvoiceItems.Count > 0) InvoiceItems.RemoveAt(InvoiceItems.Count - 1); }

    // ── Save & Print ──────────────────────────────────────────────────────
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
                CustomerName      = SelectedCustomer?.CustomerName ?? "Cash Customer",
                CustomerGSTIN     = SelectedCustomer?.GSTIN        ?? "",
                CustomerPhone     = "",
                CustomerAddress   = SelectedCustomer?.Address      ?? "",
                CustomerState     = SelectedCustomer?.State        ?? "",
                CustomerStateCode = SelectedCustomer?.StateCode    ?? "",
                TotalQuantity     = (int)InvoiceItems.Sum(r => r.Qty),
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
                    Description = row.ProductDescription,
                    Fit         = row.Fit,
                    Size        = row.Size,
                    HSN         = row.Hsn,
                    UOM         = row.Uom,
                    Quantity    = (int)row.Qty,
                    Rate        = row.Rate,
                    Amount      = row.Amount
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

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
            var model = DummyDataStore.GetDummyInvoice(SelectedCompany.CompanyName);
            model.InvoiceNo = invoiceNumber;
            if (SelectedCustomer is not null)
            {
                var party = new PartyModel
                {
                    Name      = SelectedCustomer.CustomerName,
                    Address   = SelectedCustomer.Address,
                    GSTIN     = SelectedCustomer.GSTIN,
                    State     = SelectedCustomer.State,
                    StateCode = SelectedCustomer.StateCode
                };
                model.BillingParty  = party;
                model.ShippingParty = party;
            }
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
        GstPercentage    = 5m;
        InvoiceItems.Clear();
        InvoiceDate      = DateTimeOffset.Now;
        InvoiceNumber    = $"INV-{DateTime.Now:yyMMddHHmmss}";
        InvoiceState     = string.Empty;
        InvoiceStateCode = string.Empty;
        ReverseCharge    = "No";
        TransportName    = string.Empty;
        DateOfSupply     = DateTimeOffset.Now;
        PlaceOfSupply    = string.Empty;
    }
}
