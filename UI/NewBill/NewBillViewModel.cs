using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
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
    private bool _suppressInvoiceNumberChange;

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
    [ObservableProperty] private CustomerModel? _supplyToCustomer;
    [ObservableProperty] private bool _useSameCustomer;

    [ObservableProperty] private string _billToName = string.Empty;
    [ObservableProperty] private string _billToAddress = string.Empty;
    [ObservableProperty] private string _billToGstin = string.Empty;
    [ObservableProperty] private string _billToState = string.Empty;
    [ObservableProperty] private string _billToStateCode = string.Empty;

    [ObservableProperty] private string _supplyToName = string.Empty;
    [ObservableProperty] private string _supplyToAddress = string.Empty;
    [ObservableProperty] private string _supplyToGstin = string.Empty;
    [ObservableProperty] private string _supplyToState = string.Empty;
    [ObservableProperty] private string _supplyToStateCode = string.Empty;

    // ── Invoice info ──────────────────────────────────────────────────────
    [ObservableProperty] private string _invoiceNumber   = $"INV-{DateTime.Now:yyMMddHHmmss}";
    [ObservableProperty] private DateTimeOffset _invoiceDate = DateTimeOffset.Now;
    [ObservableProperty] private string _invoiceState     = string.Empty;
    [ObservableProperty] private string _invoiceStateCode = string.Empty;

    // Friendly Date-only property for XAML DatePicker binding
    [ObservableProperty] private DateTime _invoiceDateOnly = DateTime.Now;
    // Date string property (dd/MM/yyyy) for plain-text entry binding
    [ObservableProperty] private string _invoiceDateString = DateTime.Now.ToString("dd/MM/yyyy");

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
    [ObservableProperty] private decimal _totalAmount;
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

    public string AmountInWords
    {
        get => PreviewAmountInWords;
        set => PreviewAmountInWords = value;
    }

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
        // Load customers from the company-specific database (if available)
        _ = LoadCustomersFromDbAsync();
        // Reload customers when the current company changes at runtime
        SessionContext.CurrentCompanyChanged += async _ => await LoadCustomersFromDbAsync();
        // Reload customers when any other part of the app mutates customers
        SessionContext.CustomersChanged += async () => await LoadCustomersFromDbAsync();
        InvoiceItems.CollectionChanged += OnCollectionChanged;
        // ensure at least one blank row is visible by default for easier data entry
        if (InvoiceItems.Count == 0)
            InvoiceItems.Add(new InvoiceItemModel());
        _ = LoadCompanyFromDbAsync();
        UseSameCustomer = true;
    }

    private async Task LoadCustomersFromDbAsync()
    {
        try
        {
            using var db = new BillingDbContext();
            // read-only list — no tracking improves performance
            var list = await db.Customers.AsNoTracking().ToListAsync();
            if (list != null && list.Count > 0)
            {
                Customers = new ObservableCollection<CustomerModel>(
                    list.ConvertAll(c => new CustomerModel
                    {
                        Id = c.Id,
                        CustomerName = c.Name,
                        Address = c.Address,
                        GSTIN = c.GSTIN,
                        State = c.State,
                        StateCode = c.StateCode
                    })
                );
                return;
            }
        }
        catch
        {
            // ignore DB errors and fall back to dummy data
        }

        // Fallback to dummy store if DB empty or unavailable
        Customers = new ObservableCollection<CustomerModel>(DummyDataStore.Customers);
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
                // Prefill invoice state values from company info (editable by user)
                InvoiceState     = company.State ?? string.Empty;
                InvoiceStateCode = company.StateCode ?? string.Empty;
                InvoiceDate      = DateTimeOffset.Now;
                InvoiceDateOnly  = InvoiceDate.DateTime;
                // Prepare a company-prefixed invoice number: check ApplicationSettings for InvoicePrefix first
                try
                {
                    using var db2 = new BillingDbContext();
                    var prefixSetting = await db2.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == "InvoicePrefix");
                    string prefix = !string.IsNullOrWhiteSpace(prefixSetting?.Value)
                        ? prefixSetting.Value.Trim().ToUpperInvariant()
                        : (!string.IsNullOrWhiteSpace(company.Name) ? company.Name.Substring(0, 1).ToUpperInvariant() : "INV");

                    var lastInvoice = await db2.Invoices.OrderByDescending(i => i.Id).FirstOrDefaultAsync();
                    var nextNumber = (lastInvoice?.Id ?? 0) + 1;
                    _suppressInvoiceNumberChange = true;
                    InvoiceNumber = $"{prefix}-{nextNumber}";
                    _suppressInvoiceNumberChange = false;
                }
                catch { /* ignore settings fetch failures */ }
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
        if (value is null)
        {
            BillToName = string.Empty;
            BillToAddress = string.Empty;
            BillToGstin = string.Empty;
            BillToState = string.Empty;
            BillToStateCode = string.Empty;
            return;
        }

        BillToName = value.CustomerName;
        BillToAddress = value.Address;
        BillToGstin = value.GSTIN;
        BillToState = value.State;
        BillToStateCode = value.StateCode;

        if (UseSameCustomer || SupplyToCustomer is null)
            SupplyToCustomer = value;

        PlaceOfSupply = value.State;
    }

    partial void OnSupplyToCustomerChanged(CustomerModel? value)
    {
        if (value is null)
        {
            SupplyToName = string.Empty;
            SupplyToAddress = string.Empty;
            SupplyToGstin = string.Empty;
            SupplyToState = string.Empty;
            SupplyToStateCode = string.Empty;
            return;
        }

        SupplyToName = value.CustomerName;
        SupplyToAddress = value.Address;
        SupplyToGstin = value.GSTIN;
        SupplyToState = value.State;
        SupplyToStateCode = value.StateCode;
    }

    partial void OnUseSameCustomerChanged(bool value)
    {
        if (value && SelectedCustomer is not null)
        {
            SupplyToCustomer = SelectedCustomer;
        }
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

    // Keep InvoiceDateOnly and InvoiceDate in sync
    partial void OnInvoiceDateChanged(DateTimeOffset value)
    {
        if (InvoiceDateOnly.Date != value.DateTime.Date)
            InvoiceDateOnly = value.DateTime;
        var s = value.DateTime.ToString("dd/MM/yyyy");
        if (InvoiceDateString != s)
            InvoiceDateString = s;
    }

    partial void OnInvoiceDateOnlyChanged(DateTime value)
    {
        var dto = new DateTimeOffset(value.Date);
        if (InvoiceDate.Date != dto.Date)
            InvoiceDate = dto;
    }

    partial void OnInvoiceDateStringChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var txt = value.Trim();
        // Accept only dd/MM/yyyy numeric format
        if (DateTime.TryParseExact(txt, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            var dto = new DateTimeOffset(dt.Date);
            if (InvoiceDate.Date != dto.Date)
                InvoiceDate = dto;
            if (InvoiceDateOnly.Date != dt.Date)
                InvoiceDateOnly = dt.Date;
        }
        else
        {
            NavigationService.SetStatus?.Invoke("Invalid date format. Use dd/MM/yyyy (e.g. 12/08/2005).");
        }
    }

    // Validate and normalize invoice number format like "H-425" or comma-separated "H-425, A-876"
    partial void OnInvoiceNumberChanged(string value)
    {
        if (_suppressInvoiceNumberChange) return;
        if (string.IsNullOrWhiteSpace(value)) return;
        var txt = value.Trim();
        var parts = txt.Split(',');
        var normalizedParts = new System.Collections.Generic.List<string>();
        var pattern = new Regex("^([A-Za-z]+)-(\\d+)$");
        foreach (var p in parts)
        {
            var t = p.Trim();
            var m = pattern.Match(t);
            if (m.Success)
            {
                var letters = m.Groups[1].Value.ToUpperInvariant();
                var numbers = m.Groups[2].Value;
                normalizedParts.Add($"{letters}-{numbers}");
            }
            else
            {
                NavigationService.SetStatus?.Invoke("Invoice number should be like H-425 or multiple: H-425, A-876");
                return;
            }
        }

        var normalized = string.Join(", ", normalizedParts);
        if (normalized != value)
        {
            _suppressInvoiceNumberChange = true;
            InvoiceNumber = normalized; // reassign to normalized form
            _suppressInvoiceNumberChange = false;
        }
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
        var result = InvoiceCalculationService.Calculate(
            InvoiceItems.Select(item => new InvoiceLineItemInput { Quantity = item.Qty, Rate = item.Rate }),
            DiscountAmount,
            GstPercentage,
            isInterState: false);

        SubTotal = result.SubTotal;
        TotalQuantity = (int)InvoiceItems.Sum(r => r.Qty);
        TaxableAmount = result.TaxableAmount;
        CgstAmount = result.CgstAmount;
        SgstAmount = result.SgstAmount;
        _cgst = result.CgstAmount;
        _sgst = result.SgstAmount;
        _roundOff = result.RoundOffAmount;
        TaxTotal = result.TaxTotal;
        GrandTotal = result.GrandTotal;
        RoundOffAmount = result.RoundOffAmount;
        TotalAmount = result.GrandTotal;

        PreviewCgstRate = (GstPercentage / 2m).ToString("0.#");
        PreviewCgstAmount = result.CgstAmount.ToString("F2");
        PreviewSgstRate = (GstPercentage / 2m).ToString("0.#");
        PreviewSgstAmount = result.SgstAmount.ToString("F2");
        PreviewIgstRate = string.Empty;
        PreviewIgstAmount = string.Empty;
        PreviewGstLabel = $"GST {GstPercentage:0.#}%";
        PreviewAmountInWords = NumberToWordsConverter.ConvertAmount(result.GrandTotal);
        AmountInWords = PreviewAmountInWords;
    }

    partial void OnGrandTotalChanged(decimal value)
    {
        TotalAmount = value;
        PreviewAmountInWords = NumberToWordsConverter.ConvertAmount(value);
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
                CustomerName      = string.IsNullOrWhiteSpace(BillToName) ? (SelectedCustomer?.CustomerName ?? "Cash Customer") : BillToName,
                CustomerGSTIN     = string.IsNullOrWhiteSpace(BillToGstin) ? (SelectedCustomer?.GSTIN ?? "") : BillToGstin,
                CustomerPhone     = "",
                CustomerAddress   = string.IsNullOrWhiteSpace(BillToAddress) ? (SelectedCustomer?.Address ?? "") : BillToAddress,
                CustomerState     = string.IsNullOrWhiteSpace(BillToState) ? (SelectedCustomer?.State ?? "") : BillToState,
                CustomerStateCode = string.IsNullOrWhiteSpace(BillToStateCode) ? (SelectedCustomer?.StateCode ?? "") : BillToStateCode,
                TotalQuantity     = (int)InvoiceItems.Sum(r => r.Qty),
                GrossAmount       = SubTotal,
                Discount          = DiscountAmount,
                TaxableAmount     = taxable,
                CGST              = _cgst,
                SGST              = _sgst,
                IGST              = 0,
                RoundOff          = _roundOff,
                NetAmount         = GrandTotal,
                PrintedAmountInWords = PreviewAmountInWords,
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

            // Do not generate PDF here — use in-app preview instead.
            IsPreviewVisible = true;
            NavigationService.SetStatus?.Invoke($"Invoice {invoice.InvoiceNumber} saved. Preview available.");
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
