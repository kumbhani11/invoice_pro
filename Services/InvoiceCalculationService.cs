using System;
using System.Collections.Generic;
using System.Linq;

namespace InvoicePro.Services;

public sealed class InvoiceLineItemInput
{
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
}

public sealed class InvoiceCalculationResult
{
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal RoundOffAmount { get; set; }
}

public static class InvoiceCalculationService
{
    public static InvoiceCalculationResult Calculate(
        IEnumerable<InvoiceLineItemInput> items,
        decimal discount,
        decimal gstPercentage,
        bool isInterState)
    {
        var list = items.ToList();
        var subTotal = list.Sum(i => i.Quantity * i.Rate);
        var discountAmount = Math.Max(discount, 0m);
        var taxableAmount = Math.Max(subTotal - discountAmount, 0m);

        var cgst = 0m;
        var sgst = 0m;
        var igst = 0m;

        if (isInterState)
        {
            igst = RoundToPaise(taxableAmount * gstPercentage / 100m);
        }
        else
        {
            var halfRate = gstPercentage / 2m;
            cgst = RoundToPaise(taxableAmount * halfRate / 100m);
            sgst = cgst;
        }

        var taxTotal = RoundToPaise(cgst + sgst + igst);
        var total = RoundToPaise(taxableAmount + taxTotal);
        var grandTotal = Math.Round(total, 0, MidpointRounding.AwayFromZero);
        var roundOffAmount = grandTotal - total;

        return new InvoiceCalculationResult
        {
            SubTotal = RoundToPaise(subTotal),
            Discount = RoundToPaise(discountAmount),
            TaxableAmount = RoundToPaise(taxableAmount),
            CgstAmount = RoundToPaise(cgst),
            SgstAmount = RoundToPaise(sgst),
            IgstAmount = RoundToPaise(igst),
            TaxTotal = RoundToPaise(taxTotal),
            GrandTotal = grandTotal,
            RoundOffAmount = RoundToPaise(roundOffAmount)
        };
    }

    private static decimal RoundToPaise(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
