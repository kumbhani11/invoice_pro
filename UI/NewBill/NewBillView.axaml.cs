using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.ViewModels;
using QuestPDF.Fluent;

namespace InvoicePro.UI.NewBill;

public partial class NewBillView : UserControl
{
    public NewBillView()
    {
        InitializeComponent();
        this.DataContextChanged += (_, __) => HookViewModel(this.DataContext as NewBillViewModel);
        if (this.DataContext is NewBillViewModel vm) HookViewModel(vm);

        // Wire Switch button to MainViewModel via visual tree
        var switchBtn = this.FindControl<Button>("SwitchCompanyBtn");
        if (switchBtn != null)
            switchBtn.Click += OnSwitchCompanyClick;
    }

    private void OnSwitchCompanyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var win = this.VisualRoot as Window;
        if (win?.DataContext is InvoicePro.ViewModels.MainViewModel mvm &&
            mvm.SwitchCompanyCommand.CanExecute(null))
            mvm.SwitchCompanyCommand.Execute(null);
    }

    private void HookViewModel(NewBillViewModel? vm)
    {
        if (vm == null) return;

        // Wire print: PDF generation + shell open
        vm.PrintRequested = async () =>
        {
            await Task.Run(() =>
            {
                var pdfPath = GenerateA4InvoicePdf(vm);
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            });
            vm.StatusText = "Ready";
            NavigationService.SetStatus?.Invoke($"Invoice {vm.InvoiceNumber} printed.");
        };

        // Wire DataGrid auto-edit on selection
        var grid = this.FindControl<DataGrid>("ItemsGrid");
        if (grid == null) return;

        grid.SelectionChanged += (s, e) =>
        {
            try
            {
                if (grid.SelectedItem != null)
                {
                    grid.Focus();
                    grid.BeginEdit();
                }
            }
            catch { }
        };

        vm.InvoiceItems.CollectionChanged += (s, e) =>
        {
            try
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                {
                    var newItem = e.NewItems[0];
                    grid.ScrollIntoView(newItem, grid.Columns.Count > 0 ? grid.Columns[0] : null);
                    grid.SelectedItem = newItem;
                    grid.Focus();
                    grid.BeginEdit();
                }
            }
            catch { }
        };
    }

    private static string BuildSafeFileName(string value)
    {
        foreach (var ch in Path.GetInvalidFileNameChars())
            value = value.Replace(ch, '_');
        return string.IsNullOrWhiteSpace(value) ? "Invoice" : value.Trim();
    }

    private static string GenerateA4InvoicePdf(NewBillViewModel vm)
    {
        var company = vm.SelectedCompany ?? new CompanyProfile();
        var printModel = new PrintDocumentModel
        {
            DocumentType   = "TAX_INVOICE",
            DocumentNumber = vm.InvoiceNumber,
            DocumentDate   = vm.InvoiceDate.DateTime.ToString("dd-MMM-yyyy"),
            Company = new Company
            {
                Name             = company.CompanyName,
                RegisteredOffice = company.AddressLine1,
                SalesOffice      = company.AddressLine2,
                Phone            = company.Contact,
                GSTIN            = company.GSTIN,
                State            = vm.InvoiceState,
                StateCode        = vm.InvoiceStateCode
            },
            Customer = new PrintCustomerModel
            {
                Name      = vm.BillToName,
                Address   = vm.BillToAddress,
                Phone     = string.Empty,
                GSTIN     = vm.BillToGstin,
                State     = vm.BillToState,
                StateCode = vm.BillToStateCode
            },
            Supply = new PrintCustomerModel
            {
                Name      = vm.SupplyToName,
                Address   = vm.SupplyToAddress,
                Phone     = string.Empty,
                GSTIN     = vm.SupplyToGstin,
                State     = vm.SupplyToState,
                StateCode = vm.SupplyToStateCode
            },
            Transport            = vm.TransportName,
            ReverseCharge        = vm.ReverseCharge.Equals("Yes", StringComparison.OrdinalIgnoreCase),
            DateOfSupply         = vm.DateOfSupply.DateTime.ToString("dd-MMM-yyyy"),
            PlaceOfSupply        = vm.PlaceOfSupply,
            ProductAttributeLabel = "FIT",
            GrossAmount          = vm.SubTotal,
            Discount             = vm.DiscountAmount,
            TaxableAmount        = vm.TaxableAmount,
            CGST                 = vm.CgstAmount,
            SGST                 = vm.SgstAmount,
            RoundOff             = vm.RoundOffAmount,
            NetAmount            = vm.GrandTotal,
            CgstRate             = vm.GstPercentage / 2m,
            SgstRate             = vm.GstPercentage / 2m,
            Items = vm.InvoiceItems.Select((row, _) => new PrintItemModel
            {
                ProductDescription = row.ProductDescription,
                ProductAttribute   = row.Fit,
                Size               = row.Size,
                HSN                = row.Hsn,
                Quantity           = row.Qty,
                UOM                = row.Uom,
                Rate               = row.Rate,
                Amount             = row.Amount
            }).ToList()
        };

        var fileName = BuildSafeFileName(vm.InvoiceNumber).Replace(" ", "_");
        var pdfPath  = Path.Combine(Path.GetTempPath(), $"Invoice_{fileName}.pdf");
        var doc      = new MasterDocumentTemplate(printModel);
        doc.GeneratePdf(pdfPath);
        return pdfPath;
    }
}
