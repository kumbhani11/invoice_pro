using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using InvoicePro.Models;
using InvoicePro.Services;
using QuestPDF.Fluent;

namespace InvoicePro.UI.NewBill;

public partial class NewBillView : UserControl
{
    public NewBillView()
    {
        InitializeComponent();
        this.DataContextChanged += (_, __) => HookViewModel(this.DataContext as NewBillViewModel);
        if (this.DataContext is NewBillViewModel vm) HookViewModel(vm);
    }

    private void HookViewModel(NewBillViewModel? vm)
    {
        try
        {
            var grid = this.FindControl<Avalonia.Controls.DataGrid>("ItemsGrid");
            if (grid != null)
            {
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

                if (vm?.InvoiceItems != null)
                {
                    vm.InvoiceItems.CollectionChanged += (s, e) =>
                    {
                        try
                        {
                            if (e.NewItems != null && e.NewItems.Count > 0)
                            {
                                var newItem = e.NewItems[0];
                                if (grid.Columns != null && grid.Columns.Count > 0)
                                    grid.ScrollIntoView(newItem, grid.Columns[0]);
                                else
                                    grid.ScrollIntoView(newItem, null);
                                grid.SelectedItem = newItem;
                                grid.Focus();
                                grid.BeginEdit();
                            }
                        }
                        catch { }
                    };
                }
            }
        }
        catch { }
    }

    private static string BuildSafeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var ch in invalidChars)
            value = value.Replace(ch, '_');

        return string.IsNullOrWhiteSpace(value) ? "Invoice" : value.Trim();
    }

    private static string GenerateA4InvoicePdf(NewBillViewModel vm)
    {
        var company = vm.SelectedCompany ?? new CompanyProfile();
        var printModel = new PrintDocumentModel
        {
            DocumentType = "TAX_INVOICE",
            DocumentNumber = vm.InvoiceNumber,
            DocumentDate = vm.InvoiceDate.DateTime.ToString("dd-MMM-yyyy"),
            Company = new Company
            {
                Name = company.CompanyName,
                RegisteredOffice = company.AddressLine1,
                SalesOffice = company.AddressLine2,
                Phone = company.Contact,
                GSTIN = company.GSTIN,
                State = vm.InvoiceState,
                StateCode = vm.InvoiceStateCode
            },
            Customer = new PrintCustomerModel
            {
                Name = vm.SelectedCustomer?.CustomerName ?? "Cash Customer",
                Address = vm.SelectedCustomer?.Address ?? string.Empty,
                Phone = string.Empty,
                GSTIN = vm.SelectedCustomer?.GSTIN ?? string.Empty,
                State = vm.SelectedCustomer?.State ?? vm.InvoiceState,
                StateCode = vm.SelectedCustomer?.StateCode ?? vm.InvoiceStateCode
            },
            Supply = new PrintCustomerModel
            {
                Name = vm.SelectedCustomer?.CustomerName ?? "Cash Customer",
                Address = vm.SelectedCustomer?.Address ?? string.Empty,
                Phone = string.Empty,
                GSTIN = vm.SelectedCustomer?.GSTIN ?? string.Empty,
                State = vm.SelectedCustomer?.State ?? vm.InvoiceState,
                StateCode = vm.SelectedCustomer?.StateCode ?? vm.InvoiceStateCode
            },
            Transport = vm.TransportName,
            ReverseCharge = vm.ReverseCharge.Equals("Yes", StringComparison.OrdinalIgnoreCase),
            DateOfSupply = vm.DateOfSupply.DateTime.ToString("dd-MMM-yyyy"),
            PlaceOfSupply = vm.PlaceOfSupply,
            ProductAttributeLabel = "FIT",
            GrossAmount = vm.SubTotal,
            Discount = vm.DiscountAmount,
            TaxableAmount = vm.TaxableAmount,
            CGST = vm.CgstAmount,
            SGST = vm.SgstAmount,
            RoundOff = vm.RoundOffAmount,
            NetAmount = vm.GrandTotal,
            CgstRate = vm.GstPercentage / 2m,
            SgstRate = vm.GstPercentage / 2m,
            Items = vm.InvoiceItems.Select((row, index) => new PrintItemModel
            {
                ProductDescription = row.ProductDescription,
                ProductAttribute = row.Fit,
                Size = row.Size,
                HSN = row.Hsn,
                Quantity = row.Qty,
                UOM = row.Uom,
                Rate = row.Rate,
                Amount = row.Amount
            }).ToList()
        };

        var fileName = BuildSafeFileName(vm.InvoiceNumber).Replace(" ", "_");
        var pdfPath = Path.Combine(Path.GetTempPath(), $"Invoice_{fileName}.pdf");
        var doc = new MasterDocumentTemplate(printModel);
        doc.GeneratePdf(pdfPath);
        return pdfPath;
    }

    private async void PreviewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var preview = new InvoicePreviewView
            {
                DataContext = this.DataContext,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Width = 794,
                Height = 1123
            };

            var fitButton = new Button { Content = "Fit Width", Width = 90 };
            var actualButton = new Button { Content = "100%", Width = 60 };
            var printButton = new Button { Content = "Print A4", Width = 90 };
            var toolbar = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, Margin = new Thickness(6) };
            toolbar.Children.Add(fitButton);
            toolbar.Children.Add(actualButton);
            toolbar.Children.Add(printButton);

            var scroller = new ScrollViewer
            {
                Content = preview
            };

            var grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("Auto, *");
            Grid.SetRow(toolbar, 0);
            Grid.SetRow(scroller, 1);
            grid.Children.Add(toolbar);
            grid.Children.Add(scroller);

            var win = new Window
            {
                Title = "Invoice Preview",
                Width = 850,
                Height = 1180,
                MinWidth = 600,
                MinHeight = 600,
                CanResize = true,
                Content = grid,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var owner = this.VisualRoot as Window;
            bool fitMode = true;
            fitButton.Click += (_, __) =>
            {
                fitMode = !fitMode;
                if (fitMode)
                {
                    fitButton.Content = "Fit:On";
                    try
                    {
                        var initialWidth = (owner != null) ? owner.Bounds.Width : win.Width;
                        preview.Width = System.Math.Max(0, initialWidth - 48);
                    }
                    catch { preview.Width = System.Math.Max(0, win.Width - 48); }
                }
                else
                {
                    fitButton.Content = "Fit Width";
                    preview.Width = double.NaN;
                }
            };

            fitButton.Content = "Fit:On";
            try
            {
                var initialWidth = (owner != null) ? owner.Bounds.Width : win.Width;
                preview.Width = System.Math.Max(0, initialWidth - 48);
            }
            catch { preview.Width = System.Math.Max(0, win.Width - 48); }

            actualButton.Click += (_, __) =>
            {
                fitMode = false;
                fitButton.Content = "Fit Width";
                preview.Width = double.NaN;
            };

            printButton.Click += (_, __) =>
            {
                try
                {
                    var vm = this.DataContext as NewBillViewModel;
                    if (vm == null) return;

                    var pdfPath = GenerateA4InvoicePdf(vm);
                    Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
                    NavigationService.SetStatus?.Invoke("A4 PDF ready. Print from your PDF viewer.");
                }
                catch (Exception ex)
                {
                    NavigationService.SetStatus?.Invoke($"Print failed: {ex.Message}");
                }
            };

            win.PropertyChanged += (s, ev) =>
            {
                if (ev.Property == Window.BoundsProperty && fitMode)
                {
                    try
                    {
                        var rect = (Avalonia.Rect)ev.NewValue!;
                        preview.Width = System.Math.Max(0, rect.Width - 48);
                    }
                    catch { }
                }
            };

            if (owner != null)
                await win.ShowDialog(owner);
            else
                win.Show();
        }
        catch
        {
            // Swallow errors opening preview window to avoid breaking main UI
        }
    }
}