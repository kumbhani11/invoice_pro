using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoicePro.Models;
using InvoicePro.Utils;
using System.Linq;

namespace InvoicePro.Services;

public class MasterDocumentTemplate : IDocument
{
    private PrintDocumentModel _model;

    // Brand Colors
    private readonly string TextPrimary = "#1F2937";
    private readonly string TextMuted = "#6B7280";
    private readonly string BorderColor = "#E5E7EB";
    private readonly string HeaderBg = "#F3F4F6";
    private readonly string Accent = "#0F172A";

    public MasterDocumentTemplate(PrintDocumentModel model)
    {
        _model = model;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor(TextPrimary));

                page.Content().Element(ComposeContent);
            });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            col.Spacing(20);

            col.Item().Element(ComposeHeader);
            col.Item().Element(ComposeAddresses);
            col.Item().Element(ComposeTransportStrip);
            col.Item().Element(ComposeLineItems);
            
            // Ensure the summary block doesn't split across pages
            col.Item().EnsureSpace().Element(ComposeSummaryAndBank);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Left: Company Details
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(string.IsNullOrEmpty(_model.Company.Name) ? "COMPANY NAME" : _model.Company.Name)
                    .FontSize(16).Bold().FontColor(Accent);
                
                col.Item().PaddingTop(4).DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted)).Text(t =>
                {
                    if (!string.IsNullOrEmpty(_model.Company.RegisteredOffice)) t.Line($"Regd: {_model.Company.RegisteredOffice}");
                    if (!string.IsNullOrEmpty(_model.Company.SalesOffice)) t.Line($"Sales: {_model.Company.SalesOffice}");
                });

                col.Item().PaddingTop(4).DefaultTextStyle(x => x.FontSize(8).FontColor(TextMuted)).Text(t =>
                {
                    if (!string.IsNullOrEmpty(_model.Company.Phone))
                    {
                        t.Span($"Phone: {_model.Company.Phone}   ");
                    }
                    if (!string.IsNullOrEmpty(_model.Company.GSTIN))
                    {
                        t.Span("GSTIN: ").Medium(); 
                        t.Span(_model.Company.GSTIN);
                    }
                });
            });

            // Right: Invoice Metadata
            row.RelativeItem().AlignRight().Column(col =>
            {
                string title = _model.DocumentType == "TAX_INVOICE" ? "TAX INVOICE" : "CREDIT NOTE";
                col.Item().Text(title).FontSize(16).Bold().FontColor(Accent);
                
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    void AddMetaRow(string label, string value)
                    {
                        table.Cell().AlignRight().PaddingRight(8).Text(label).FontSize(8).Medium().FontColor(TextMuted);
                        table.Cell().AlignRight().Text(value).FontSize(9).SemiBold();
                    }

                    if (_model.DocumentType == "TAX_INVOICE")
                    {
                        AddMetaRow("INVOICE NO.", _model.DocumentNumber);
                        AddMetaRow("DATE", _model.DocumentDate);
                    }
                    else
                    {
                        AddMetaRow("CREDIT NOTE NO.", _model.DocumentNumber);
                        AddMetaRow("DATE", _model.DocumentDate);
                        AddMetaRow("ORIGINAL INV NO.", _model.OriginalInvoiceNumber);
                    }
                    
                    AddMetaRow("STATE", _model.Company.State);
                    AddMetaRow("STATE CODE", _model.Company.StateCode);
                });
            });
        });
    }

    private void ComposeAddresses(IContainer container)
    {
        container.Row(row =>
        {
            string billToTitle = _model.DocumentType == "TAX_INVOICE" ? "BILL TO" : "DEBIT TO";
            
            // Left Card
            row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(12).Column(col =>
            {
                col.Item().Text(billToTitle).FontSize(10).SemiBold().FontColor(TextMuted);
                col.Item().PaddingTop(4).Text(_model.Customer.Name).FontSize(10).Bold();
                
                col.Item().PaddingTop(4).Text(t =>
                {
                    if (!string.IsNullOrEmpty(_model.Customer.Address)) t.Line(_model.Customer.Address);
                    if (!string.IsNullOrEmpty(_model.Customer.City)) t.Line(_model.Customer.City);
                });
                
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(60); c.RelativeColumn(); });
                    
                    void AddLine(string lbl, string val)
                    {
                        table.Cell().Text(lbl).FontSize(8).Medium().FontColor(TextMuted);
                        table.Cell().Text(val).FontSize(9).Medium();
                    }
                    
                    AddLine("GSTIN:", _model.Customer.GSTIN);
                    AddLine("STATE:", $"{_model.Customer.State} ({_model.Customer.StateCode})");
                });
            });

            row.ConstantItem(20); // Spacer

            // Right Card
            row.RelativeItem().Border(1).BorderColor(BorderColor).Padding(12).Column(col =>
            {
                col.Item().Text("SHIP TO").FontSize(10).SemiBold().FontColor(TextMuted);
                col.Item().PaddingTop(4).Text(_model.Supply.Name).FontSize(10).Bold();
                
                col.Item().PaddingTop(4).Text(t =>
                {
                    if (!string.IsNullOrEmpty(_model.Supply.Address)) t.Line(_model.Supply.Address);
                    if (!string.IsNullOrEmpty(_model.Supply.City)) t.Line(_model.Supply.City);
                });
                
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.ConstantColumn(60); c.RelativeColumn(); });
                    
                    void AddLine(string lbl, string val)
                    {
                        table.Cell().Text(lbl).FontSize(8).Medium().FontColor(TextMuted);
                        table.Cell().Text(val).FontSize(9).Medium();
                    }
                    
                    AddLine("GSTIN:", _model.Supply.GSTIN);
                    AddLine("STATE:", $"{_model.Supply.State} ({_model.Supply.StateCode})");
                });
            });
        });
    }

    private void ComposeTransportStrip(IContainer container)
    {
        container.Background(HeaderBg).PaddingHorizontal(12).PaddingVertical(8).Row(row =>
        {
            void AddDetail(string label, string value)
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(label).FontSize(8).Medium().FontColor(TextMuted);
                    c.Item().Text(value).FontSize(9).SemiBold();
                });
            }

            AddDetail("TRANSPORT", _model.Transport);
            AddDetail("DATE OF SUPPLY", _model.DateOfSupply);
            AddDetail("PLACE OF SUPPLY", _model.PlaceOfSupply);
            AddDetail("REVERSE CHARGE", _model.ReverseCharge ? "Yes" : "No");
        });
    }

    private void ComposeLineItems(IContainer container)
    {
        container.Table(table =>
        {
            // Columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);   // S.No
                columns.RelativeColumn(4);    // Description
                columns.RelativeColumn(2);    // Fit/Design
                columns.RelativeColumn(1.5f); // Size
                columns.RelativeColumn(1.5f); // HSN
                columns.RelativeColumn(1.5f); // QTY
                columns.RelativeColumn(1.2f); // UOM
                columns.RelativeColumn(2);    // Rate
                columns.RelativeColumn(2.5f); // Amount
            });

            // Header
            table.Header(header =>
            {
                IContainer HeaderCell() => header.Cell().Background(HeaderBg).Padding(8);

                HeaderCell().AlignCenter().Text("S.N.").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignLeft().Text("DESCRIPTION").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignLeft().Text(_model.ProductAttributeLabel.ToUpper()).FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignCenter().Text("SIZE").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignCenter().Text("HSN").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignRight().Text("QTY").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignCenter().Text("UOM").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignRight().Text("RATE").FontSize(9).SemiBold().FontColor(TextMuted);
                HeaderCell().AlignRight().Text("AMOUNT").FontSize(9).SemiBold().FontColor(TextMuted);
            });

            // Rows
            var i = 1;
            foreach (var item in _model.Items)
            {
                IContainer Cell() => table.Cell().BorderBottom(1).BorderColor(BorderColor).PaddingVertical(8).PaddingHorizontal(4);

                Cell().AlignCenter().Text(i++.ToString());
                Cell().AlignLeft().Text(item.ProductDescription);
                Cell().AlignLeft().Text(item.ProductAttribute);
                Cell().AlignCenter().Text(item.Size);
                Cell().AlignCenter().Text(item.HSN);
                Cell().AlignRight().Text(item.Quantity.ToString("0.##"));
                Cell().AlignCenter().Text(item.UOM).FontColor(TextMuted);
                Cell().AlignRight().Text(item.Rate.ToString("F2"));
                Cell().AlignRight().Text(item.Amount.ToString("F2")).SemiBold();
            }
        });
    }

    private void ComposeSummaryAndBank(IContainer container)
    {
        container.Row(row =>
        {
            // Left: 60% Bank Details & T&C
            row.RelativeItem(6).Column(col =>
            {
                col.Spacing(16);

                // Bank Info Card
                col.Item().Border(1).BorderColor(BorderColor).Padding(12).Column(c =>
                {
                    c.Item().Text("BANK DETAILS").FontSize(10).SemiBold().FontColor(TextMuted);
                    c.Item().PaddingTop(8).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.ConstantColumn(80); cd.RelativeColumn(); });

                        void AddRow(string lbl, string val)
                        {
                            t.Cell().PaddingBottom(4).Text(lbl).FontSize(8).Medium().FontColor(TextMuted);
                            t.Cell().PaddingBottom(4).Text(val).FontSize(9).SemiBold();
                        }

                        AddRow("BANK NAME:", _model.Company.BankName);
                        AddRow("BRANCH:", _model.Company.BankBranch);
                        AddRow("A/C NUMBER:", _model.Company.BankAccount);
                        AddRow("IFSC CODE:", _model.Company.IFSC);
                    });
                });

                // T&C
                col.Item().Column(c =>
                {
                    c.Item().Text("TERMS & CONDITIONS").FontSize(9).SemiBold().FontColor(TextMuted);
                    string tc = string.IsNullOrEmpty(_model.Company.TermsAndConditions) 
                        ? "1. Payment will be accepted by Cross Cheque or Order Draft only.\n2. Goods once sold will not be taken back.\n3. Payment strictly within 30 days." 
                        : _model.Company.TermsAndConditions;
                    c.Item().PaddingTop(4).Text(tc).FontSize(8).FontColor(TextMuted);
                });
            });

            row.ConstantItem(20);

            // Right: 40% Calculations
            row.RelativeItem(4).Column(col =>
            {
                col.Spacing(8);

                void AddCalcRow(string label, decimal amount, bool isBold = false, bool isTotal = false)
                {
                    col.Item().Row(r =>
                    {
                        var lbl = r.RelativeItem().Text(label);
                        var amt = r.ConstantItem(90).AlignRight().Text(amount.ToString("F2"));

                        if (isTotal)
                        {
                            lbl.FontSize(12).Bold().FontColor(Accent);
                            amt.FontSize(12).Bold().FontColor(Accent);
                        }
                        else if (isBold)
                        {
                            lbl.SemiBold();
                            amt.SemiBold();
                        }
                        else
                        {
                            lbl.Medium().FontColor(TextMuted);
                        }
                    });
                }

                AddCalcRow("Gross Amount", _model.GrossAmount);
                if (_model.Discount > 0) AddCalcRow("Discount", -_model.Discount);
                
                col.Item().LineHorizontal(1).LineColor(BorderColor);
                AddCalcRow("Taxable Amount", _model.TaxableAmount, true);
                
                if (_model.CGST > 0) AddCalcRow($"CGST ({_model.CgstRate:0.#}%)", _model.CGST);
                if (_model.SGST > 0) AddCalcRow($"SGST ({_model.SgstRate:0.#}%)", _model.SGST);
                if (_model.IGST > 0) AddCalcRow($"IGST ({_model.IgstRate:0.#}%)", _model.IGST);
                if (_model.RoundOff != 0) AddCalcRow("Round Off", _model.RoundOff);

                col.Item().PaddingTop(8).Background(HeaderBg).Padding(12).Row(r =>
                {
                    r.RelativeItem().Text("NET AMOUNT").FontSize(12).Bold().FontColor(Accent);
                    r.ConstantItem(120).AlignRight().Text($"₹ {_model.NetAmount:F2}").FontSize(12).Bold().FontColor(Accent);
                });
                
                col.Item().Text(t =>
                {
                    t.Span("Amount in words: ").Medium().FontColor(TextMuted).FontSize(8);
                    t.Span(NumberToWordsConverter.ConvertAmount(_model.NetAmount)).SemiBold().FontSize(8);
                });

                // Signature Box
                col.Item().PaddingTop(30).Column(c =>
                {
                    c.Item().AlignRight().Text($"For {_model.Company.Name}").FontSize(9).Bold();
                    c.Item().PaddingTop(50).AlignRight().Text("Authorised Signatory").FontSize(9).Medium().FontColor(TextMuted);
                });
            });
        });
    }
}
