using System;
using System.Collections.Generic;
namespace InvoicePro.Models;
public class Invoice {
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    
    // Customer snapshot
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerGSTIN { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerState { get; set; } = string.Empty;
    public string CustomerStateCode { get; set; } = string.Empty;
    
    // Totals
    public int TotalQuantity { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    public decimal RoundOff { get; set; }
    public decimal NetAmount { get; set; }
    
    // State/Status
    public bool IsCancelled { get; set; }
    public string PrintedAmountInWords { get; set; } = string.Empty;
    
    public List<InvoiceItem> Items { get; set; } = new();
}
