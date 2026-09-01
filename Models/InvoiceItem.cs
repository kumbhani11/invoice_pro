namespace InvoicePro.Models;
public class InvoiceItem {
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    // Product snapshot
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string HSN { get; set; } = string.Empty;
    public string UOM { get; set; } = string.Empty;
    
    // Calculation
    public int Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
