namespace InvoicePro.Models;
public class Product {
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Fit { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string HSN { get; set; } = string.Empty;
    public string UOM { get; set; } = "Pcs";
    public decimal Rate { get; set; }
}
