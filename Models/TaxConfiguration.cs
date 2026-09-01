namespace InvoicePro.Models;
public class TaxConfiguration {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CGSTRate { get; set; }
    public decimal SGSTRate { get; set; }
    public decimal IGSTRate { get; set; }
    public bool IsActive { get; set; } = true;
}
