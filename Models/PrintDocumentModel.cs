using System.Collections.Generic;

namespace InvoicePro.Models;

public class PrintDocumentModel
{
    public string DocumentType { get; set; } = "TAX_INVOICE"; // "TAX_INVOICE" or "CREDIT_NOTE"
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentDate { get; set; } = string.Empty;
    public string OriginalInvoiceNumber { get; set; } = string.Empty;
    
    public Company Company { get; set; } = new();
    public PrintCustomerModel Customer { get; set; } = new();
    public PrintCustomerModel Supply { get; set; } = new();
    
    public string Transport { get; set; } = string.Empty;
    public bool ReverseCharge { get; set; }
    public string DateOfSupply { get; set; } = string.Empty;
    public string PlaceOfSupply { get; set; } = string.Empty;
    
    public string ProductAttributeLabel { get; set; } = "FIT";
    
    public List<PrintItemModel> Items { get; set; } = new();
    
    public decimal GrossAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxableAmount { get; set; }
    
    public decimal CGST { get; set; }
    public decimal SGST { get; set; }
    public decimal IGST { get; set; }
    
    public decimal RoundOff { get; set; }
    public decimal NetAmount { get; set; }
    
    // For GST table
    public decimal CgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal IgstRate { get; set; }
}

public class PrintItemModel
{
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductAttribute { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string HSN { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UOM { get; set; } = "Pcs";
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PrintCustomerModel
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
    public string GSTIN { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
