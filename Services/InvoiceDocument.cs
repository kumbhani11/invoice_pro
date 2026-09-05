using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoicePro.Models;

namespace InvoicePro.Services;

public class InvoiceDocument : IDocument
{
    private readonly InvoiceModel _inv;

    public InvoiceDocument(InvoiceModel inv) => _inv = inv;

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(10, Unit.Millimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor("#000000"));

            // Outer border replicating .invoice-box
            page.Content()
                .Border(1.5f)
                .BorderColor("#000000")
                .Padding(6)
                .Column(col =>
                {
                    col.Item().Element(ComposeTopHeader);
                    col.Item().Element(ComposeHeaderGrid);
                    col.Item().Element(ComposePartiesGrid);
                    col.Item().Element(ComposeTransportRow);
                    col.Item().Element(ComposeProductTable);
                    col.Item().Element(ComposeCalculationsSection);
                    col.Item().Element(ComposeFooterBar);
                    // Note: ComposeFooterBar already includes words + bank + signatory + bottom bar
                });
        });
    }

    // ── 1. "TAX INVOICE" + "Original for Recepient" ──────────────────────
    private void ComposeTopHeader(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().Text(""); // left spacer
            row.RelativeItem(3).AlignCenter().Text("TAX INVOICE").FontSize(16).Bold();
            row.ConstantItem(130).AlignRight().Text("Original for Recepient").FontSize(11).Bold();
        });
    }

    // ── 2. Company Info (60%) | Invoice Meta (40%) ────────────────────────
    private void ComposeHeaderGrid(IContainer c)
    {
        var cp = _inv.Company;
        c.Border(1).BorderColor("#000000").Row(row =>
        {
            // Left 60% — company details
            row.RelativeItem(6).BorderRight(1).BorderColor("#000000").Padding(4).Column(col =>
            {
                col.Item().Text(cp.CompanyName).FontSize(14).Bold();
                col.Item().Text(cp.AddressLine1).FontSize(9);
                col.Item().Text(cp.AddressLine2).FontSize(9);
                col.Item().Text(cp.Contact).FontSize(9);
                col.Item().Text(t =>
                {
                    t.Span("GSTIN: ").Bold();
                    t.Span(cp.GSTIN).Bold();
                });
            });

            // Right 40% — invoice meta
            row.RelativeItem(4).Padding(4).Table(table =>
            {
                table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });

                void MetaRow(string label, string value, float valueFontSize = 9)
                {
                    table.Cell().Text(label).Bold().FontSize(9);
                    table.Cell().AlignRight().Text(value).Bold().FontSize(valueFontSize);
                }

                MetaRow("INVOICE NO.:", $": {_inv.InvoiceNo}", 13);
                MetaRow("DATE",         $": {_inv.Date}");
                MetaRow("State",        $": {_inv.State}");
                MetaRow("State Code",   $": {_inv.StateCode}");
            });
        });
    }

    // ── 3. Bill to Party (left, dashed right) | Supply to Party (right) ──
    private void ComposePartiesGrid(IContainer c)
    {
        c.Border(1).BorderColor("#000000").Row(row =>
        {
            row.RelativeItem().Padding(5).Column(col => PartyColumn(col, "Bill to party:", _inv.BillingParty));

            // Dashed divider
            row.ConstantItem(1).Background("#000000"); // solid fallback; QuestPDF has no native dashed border

            row.RelativeItem().Padding(5).Column(col => PartyColumn(col, "Supply to party", _inv.ShippingParty));
        });
    }

    private static void PartyColumn(ColumnDescriptor col, string title, PartyModel party)
    {
        col.Item().Text(t =>
        {
            t.Span(title + " ").Bold();
            t.Span(party.Name).Bold().FontSize(11);
        });
        col.Item().PaddingLeft(70).Text(party.Address).FontSize(9);
        col.Item().PaddingTop(6).Text(t =>
        {
            t.Span("GSTIN        : ").Bold();
            t.Span(party.GSTIN).Bold();
        });
        col.Item().Text(t =>
        {
            t.Span("State          : ").Bold();
            t.Span(party.State + "      ").Bold();
            t.Span("State Code:  ").Bold();
            t.Span(party.StateCode).Bold();
        });
    }

    // ── 4. Transport Row ──────────────────────────────────────────────────
    private void ComposeTransportRow(IContainer c)
    {
        c.Border(1).BorderColor("#000000").Padding(3).Row(row =>
        {
            void Cell(string label, string value)
                => row.RelativeItem().Text(t =>
                {
                    t.Span(label).Bold();
                    t.Span(value);
                });

            Cell("Reverse Charge: ", _inv.ReverseCharge);
            Cell("Transport : ",     _inv.Transport);
            Cell("Date of Supply: ", _inv.DateOfSupply);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Place of Supply: ").Bold();
                t.Span(_inv.PlaceOfSupply);
            });
        });
    }

    // ── 5. Product Table ──────────────────────────────────────────────────
    private void ComposeProductTable(IContainer c)
    {
        c.Border(1).BorderColor("#000000").Table(table =>
        {
            table.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(28);   // S.No  5%
                cd.RelativeColumn(4);    // Desc  27%
                cd.RelativeColumn(1.8f); // FIT   12%
                cd.RelativeColumn(1.3f); // SIZE   9%
                cd.RelativeColumn(1);    // HSN    7%
                cd.RelativeColumn(0.9f); // QTY    6%
                cd.RelativeColumn(0.9f); // UOM    6%
                cd.RelativeColumn(1.5f); // Rate  10%
                cd.RelativeColumn(2.7f); // AMT   18%
            });

            // Header row
            table.Header(h =>
            {
                IContainer HCell() => h.Cell().Border(1).BorderColor("#000000")
                    .Padding(3).AlignCenter();

                HCell().Text("S.\nNo.").Bold().FontSize(9);
                HCell().Text("Product Discription").Bold().FontSize(9);
                HCell().Text("FIT").Bold().FontSize(9);
                HCell().Text("SIZE").Bold().FontSize(9);
                HCell().Text("HSN").Bold().FontSize(9);
                HCell().Text("QTY").Bold().FontSize(9);
                HCell().Text("UOM").Bold().FontSize(9);
                HCell().Text("Rate").Bold().FontSize(9);
                HCell().Text("AMOUNT RS.").Bold().FontSize(9);
            });

            // Data rows
            foreach (var item in _inv.Items)
            {
                IContainer DCell(bool alignRight = false)
                {
                    var cell = table.Cell().BorderLeft(1).BorderRight(1).BorderColor("#000000").Padding(2);
                    return alignRight ? cell.AlignRight() : cell.AlignCenter();
                }

                DCell().Text(item.SNo.ToString());
                table.Cell().BorderLeft(1).BorderRight(1).BorderColor("#000000").Padding(2).AlignLeft().Text(item.Description);
                DCell().Text(item.Fit);
                DCell().Text(item.Size);
                DCell().Text(item.HSN);
                DCell().Text(item.Qty.ToString("0.##"));
                DCell().Text(item.UOM);
                DCell().Text(item.Rate.ToString("F0"));
                DCell(true).Text(item.Amount.ToString("F2"));
            }

            // Spacer row — minimum height filler
            for (int col = 0; col < 9; col++)
                table.Cell().BorderLeft(1).BorderRight(1).BorderColor("#000000").MinHeight(150).Text("");

            // Total row
            table.Cell().ColumnSpan(5).Border(1).BorderColor("#000000")
                .Padding(3).AlignCenter().Text("Total").Bold().FontSize(12);
            table.Cell().Border(1).BorderColor("#000000")
                .Padding(3).AlignCenter().Text(_inv.Totals.TotalQty.ToString("0.##")).Bold();
            table.Cell().ColumnSpan(2).Border(1).BorderColor("#000000").Text("");
            table.Cell().Border(1).BorderColor("#000000")
                .Padding(3).AlignRight().Text(_inv.Totals.GrossAmount.ToString("F2")).Bold();
        });
    }

    // ── 6. Calculations Section (63% | 37%) ──────────────────────────────
    // NOTE: QuestPDF table headers do not support RowSpan. We use two header rows instead.
    private void ComposeCalculationsSection(IContainer c)
    {
        var t = _inv.Totals;
        c.Border(1).BorderColor("#000000").Row(row =>
        {
            // Left 63% — Tax breakup table
            row.RelativeItem(63).BorderRight(1).BorderColor("#000000").Padding(4).Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3); // TAX HEAD
                        cd.RelativeColumn(2); // CGST %
                        cd.RelativeColumn(2); // CGST Amt
                        cd.RelativeColumn(2); // SGST %
                        cd.RelativeColumn(2); // SGST Amt
                        cd.RelativeColumn(2); // IGST %
                        cd.RelativeColumn(2); // IGST Amt
                    });

                    // QuestPDF header rows: row 1 = group labels, row 2 = sub-labels
                    table.Header(h =>
                    {
                        // Row 1 — group labels
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("TAX HEAD").Bold().FontSize(8);
                        h.Cell().ColumnSpan(2).Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("CGST").Bold().FontSize(8);
                        h.Cell().ColumnSpan(2).Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("SGST").Bold().FontSize(8);
                        h.Cell().ColumnSpan(2).Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("IGST").Bold().FontSize(8);
                        // Row 2 — sub-labels
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("").FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("%").Bold().FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("%").Bold().FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("%").Bold().FontSize(8);
                        h.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter().Text("Amount").Bold().FontSize(8);
                    });

                    // GST 5% row
                    IContainer TC() => table.Cell().Border(1).BorderColor("#000000").Padding(2).AlignCenter();
                    TC().AlignLeft().Text("G.S.T 5%").Bold().FontSize(8);
                    TC().Text(t.CgstRate > 0 ? t.CgstRate.ToString("0.#") : "");
                    TC().Text(t.CgstAmount > 0 ? t.CgstAmount.ToString("F2") : "");
                    TC().Text(t.SgstRate > 0 ? t.SgstRate.ToString("0.#") : "");
                    TC().Text(t.SgstAmount > 0 ? t.SgstAmount.ToString("F2") : "");
                    TC().Text(t.IgstRate > 0 ? t.IgstRate.ToString("0.#") : "");
                    TC().Text(t.IgstAmount > 0 ? t.IgstAmount.ToString("F2") : "");
                });
            });

            // Right 37% — Net amounts
            row.RelativeItem(37).Table(table =>
            {
                table.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });

                void AmtRow(string label, string value, bool bold = false, bool noTopBorder = false)
                {
                    var leftBorder  = noTopBorder ? 0 : 1;
                    var rightBorder = noTopBorder ? 0 : 1;
                    table.Cell().BorderBottom(1).BorderColor("#000000").Padding(2)
                        .Text(label).FontSize(9).If(bold, s => s.Bold());
                    table.Cell().BorderBottom(1).BorderColor("#000000").Padding(2).AlignRight()
                        .Text(value).FontSize(9).If(bold, s => s.Bold());
                }

                AmtRow("Discount",        t.Discount.ToString("F2"),      noTopBorder: true);
                AmtRow("Taxable Amount",  t.TaxableAmount.ToString("F2"));
                AmtRow($"GST {(t.CgstRate + t.SgstRate + t.IgstRate):0.#}%",
                       (t.CgstAmount + t.SgstAmount + t.IgstAmount).ToString("F2"));
                AmtRow("Round Off",       t.RoundOff.ToString("F2"));
                AmtRow("NET AMOUNT (Rs.)", t.NetAmount.ToString("F2"), bold: true);
            });
        });
    }

    // ── 7. Footer: Words + Bank + Signatory + Bottom bar ────────────────
    private void ComposeFooterBar(IContainer c)
    {
        // Amount in words + bank + signatory combined
        var cp = _inv.Company;
        var t  = _inv.Totals;

        c.Border(1).BorderColor("#000000").Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor("#000000").Padding(4).Text(tx =>
            {
                tx.Span("Rupees in words : ").Bold();
                tx.Span(t.AmountInWords);
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().BorderRight(1).BorderColor("#000000").Padding(4).Column(bc =>
                {
                    bc.Item().Text("Bank Details").Bold().Underline();
                    bc.Item().PaddingTop(2).Table(bt =>
                    {
                        bt.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(65);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(3);
                        });

                        bt.Cell().Text("Bank Name :").Bold().FontSize(8);
                        bt.Cell().Text(cp.BankName).Bold().FontSize(8);
                        bt.Cell().Text($"Branch : {cp.BankBranch}").Bold().FontSize(8);
                        bt.Cell().Text("Bank A/C:").Bold().FontSize(8);
                        bt.Cell().ColumnSpan(2).Padding(0).Text(cp.BankAccountNo).Bold().FontSize(8);
                        bt.Cell().Text("Bank IFSC:").Bold().FontSize(8);
                        bt.Cell().ColumnSpan(2).Padding(0).Text(cp.BankIFSC).Bold().FontSize(8);
                    });

                    bc.Item().PaddingTop(6).Text("Terms & conditions :").Bold().Underline().FontSize(9);
                    bc.Item().PaddingTop(2).Text(
                        "* Payment of will be accepted by Cross Cheque or Order Draft only.\n" +
                        "* Goods once sold will not be taken back.* Payment strictly within 30 days."
                    ).FontSize(9);
                });

                row.RelativeItem().Padding(4).Column(sc =>
                {
                    sc.Item().AlignCenter().Text("Ceritified that the particulars given").FontSize(8).FontColor("#333333");
                    sc.Item().AlignCenter().Text("above are true and correct").FontSize(8).FontColor("#333333");
                    sc.Item().PaddingTop(4).AlignCenter().Text($"For {cp.CompanyName}").Bold().FontSize(11);
                    sc.Item().MinHeight(45);
                    sc.Item().AlignCenter().Text("Authorised Signatory").Bold();
                });
            });

            // Bottom footer bar
            col.Item().BorderTop(1).BorderColor("#000000").PaddingHorizontal(6).PaddingVertical(3).Row(r =>
            {
                r.RelativeItem().AlignCenter().Text("Subject to Mumbai Juridiction").Bold().FontSize(9);
                r.ConstantItem(60).AlignRight().Text("E. & O.E.").Bold().FontSize(9);
            });
        });
    }
}

// Extension to conditionally apply text style
internal static class TextSpanExtensions
{
    public static TextSpanDescriptor If(this TextSpanDescriptor span, bool condition,
        System.Func<TextSpanDescriptor, TextSpanDescriptor> apply)
        => condition ? apply(span) : span;
}
