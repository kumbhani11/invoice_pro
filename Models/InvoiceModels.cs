using System;
using System.Collections.Generic;

namespace InvoicePro.Models;

public class CompanyProfile
{
    public string CompanyName   { get; set; } = string.Empty;
    public string AddressLine1  { get; set; } = string.Empty;
    public string AddressLine2  { get; set; } = string.Empty;
    public string Contact       { get; set; } = string.Empty;
    public string GSTIN         { get; set; } = string.Empty;
    public string BankName      { get; set; } = string.Empty;
    public string BankBranch    { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string BankIFSC      { get; set; } = string.Empty;
}

public class PartyModel
{
    public string Name      { get; set; } = string.Empty;
    public string Address   { get; set; } = string.Empty;
    public string GSTIN     { get; set; } = string.Empty;
    public string State     { get; set; } = string.Empty;
    public string StateCode { get; set; } = string.Empty;
}

public class LineItem
{
    public int     SNo        { get; set; }
    public string  Description{ get; set; } = string.Empty;
    public string  Fit        { get; set; } = string.Empty;
    public string  Size       { get; set; } = string.Empty;
    public string  HSN        { get; set; } = string.Empty;
    public decimal Qty        { get; set; }
    public string  UOM        { get; set; } = "Pcs";
    public decimal Rate       { get; set; }
    public decimal Amount     { get; set; }
}

public class InvoiceTotals
{
    public decimal TotalQty      { get; set; }
    public decimal GrossAmount   { get; set; }
    public decimal Discount      { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal CgstRate      { get; set; }
    public decimal CgstAmount    { get; set; }
    public decimal SgstRate      { get; set; }
    public decimal SgstAmount    { get; set; }
    public decimal IgstRate      { get; set; }
    public decimal IgstAmount    { get; set; }
    public decimal RoundOff      { get; set; }
    public decimal NetAmount     { get; set; }
    public string  AmountInWords { get; set; } = string.Empty;
}

public class InvoiceModel
{
    public string         InvoiceNo      { get; set; } = string.Empty;
    public string         Date           { get; set; } = string.Empty;
    public string         State          { get; set; } = string.Empty;
    public string         StateCode      { get; set; } = string.Empty;
    public string         ReverseCharge  { get; set; } = "No";
    public string         Transport      { get; set; } = string.Empty;
    public string         DateOfSupply   { get; set; } = string.Empty;
    public string         PlaceOfSupply  { get; set; } = string.Empty;
    public CompanyProfile Company        { get; set; } = new();
    public PartyModel     BillingParty   { get; set; } = new();
    public PartyModel     ShippingParty  { get; set; } = new();
    public List<LineItem> Items          { get; set; } = new();
    public InvoiceTotals  Totals         { get; set; } = new();
}
