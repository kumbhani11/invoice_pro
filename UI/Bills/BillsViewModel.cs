using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.Bills;

public partial class BillsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Invoice> _invoicesList = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private DateTimeOffset? _searchDate;
    [ObservableProperty] private Invoice? _selectedInvoice;

    public BillsViewModel()
    {
        _ = LoadInvoicesAsync();
    }

    [RelayCommand]
    private async Task LoadInvoicesAsync()
    {
        using var db = new BillingDbContext();
        var query = db.Invoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(i => i.InvoiceNumber.Contains(SearchText) || i.CustomerName.Contains(SearchText));
        }

        if (SearchDate.HasValue)
        {
            var date = SearchDate.Value.Date;
            query = query.Where(i => i.InvoiceDate.Date == date);
        }

        var result = await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync();
        InvoicesList = new ObservableCollection<Invoice>(result);
    }

    async partial void OnSearchTextChanged(string value)
    {
        await LoadInvoicesAsync();
    }
    
    async partial void OnSearchDateChanged(DateTimeOffset? value)
    {
        await LoadInvoicesAsync();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SearchDate = null;
    }

    [RelayCommand]
    private async Task PrintSelectedAsync()
    {
        if (SelectedInvoice == null) return;
        
        using var db = new BillingDbContext();
        var fullInvoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == SelectedInvoice.Id);
            
        var company = await db.Companies.FirstOrDefaultAsync() ?? new Company { Name = "COMPANY NAME" };

        if (fullInvoice != null)
        {
            try
            {
                var printModel = new PrintDocumentModel
                {
                    DocumentType = "TAX_INVOICE",
                    DocumentNumber = fullInvoice.InvoiceNumber,
                    DocumentDate = fullInvoice.InvoiceDate.ToString("dd-MMM-yyyy"),
                    Company = company,
                    Customer = new PrintCustomerModel 
                    { 
                        Name = fullInvoice.CustomerName ?? "", 
                        Address = fullInvoice.CustomerAddress ?? "",
                        Phone = fullInvoice.CustomerPhone ?? "",
                        GSTIN = fullInvoice.CustomerGSTIN ?? "",
                        City = "",
                        State = "",
                        StateCode = ""
                    },
                    Supply = new PrintCustomerModel 
                    { 
                        Name = fullInvoice.CustomerName ?? "", 
                        Address = fullInvoice.CustomerAddress ?? "",
                        Phone = fullInvoice.CustomerPhone ?? "",
                        GSTIN = fullInvoice.CustomerGSTIN ?? "",
                        City = "",
                        State = "",
                        StateCode = ""
                    },
                    GrossAmount = fullInvoice.GrossAmount,
                    Discount = fullInvoice.Discount,
                    TaxableAmount = fullInvoice.TaxableAmount,
                    CGST = fullInvoice.CGST,
                    SGST = fullInvoice.SGST,
                    RoundOff = fullInvoice.RoundOff,
                    NetAmount = fullInvoice.NetAmount,
                    CgstRate = 9m, // Since Hardika default is 9%
                    SgstRate = 9m,
                    Items = fullInvoice.Items.Select(x => new PrintItemModel 
                    {
                        ProductDescription = x.Description ?? "",
                        HSN = x.HSN ?? "",
                        Quantity = x.Quantity,
                        Rate = x.Rate,
                        Amount = x.Amount
                    }).ToList()
                };

                var doc = new MasterDocumentTemplate(printModel);
                string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Invoice_{fullInvoice.InvoiceNumber}.pdf");
                doc.GeneratePdf(path);
                
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error printing: " + ex.Message);
            }
        }
    }

    [RelayCommand]
    private async Task CancelSelectedAsync()
    {
        if (SelectedInvoice == null || SelectedInvoice.IsCancelled) return;
        
        using var db = new BillingDbContext();
        var invoice = await db.Invoices.FindAsync(SelectedInvoice.Id);
        if (invoice != null)
        {
            invoice.IsCancelled = true;
            await db.SaveChangesAsync();
            await LoadInvoicesAsync();
        }
    }
}
