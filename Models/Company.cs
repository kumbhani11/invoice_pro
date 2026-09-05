namespace InvoicePro.Models;
public class Company {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegisteredOffice { get; set; } = string.Empty;
    public string SalesOffice { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string IFSC { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;
    public string AuthorizedSignatoryText { get; set; } = string.Empty;
}
