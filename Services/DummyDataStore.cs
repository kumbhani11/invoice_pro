using System;
using System.Collections.Generic;
using InvoicePro.Models;
using InvoicePro.Utils;

namespace InvoicePro.Services;

public static class DummyDataStore
{
    public static CompanyProfile GetCompanyProfile(string companyName) => companyName switch
    {
        "HARDIKA CREATION" => new CompanyProfile
        {
            CompanyName   = "HARDIKA CREATION",
            AddressLine1  = "Regd.Off: Shop No.5, Laxmi Niwas, Near Bus Stand, Kandivali (E), Mumbai-400101",
            AddressLine2  = "Sales Office: 12B, Textile Market, Ring Road, Surat-395002",
            Contact       = "Ph. +91 9876543210",
            GSTIN         = "24BCDPK1234C1Z5",
            BankName      = "STATE BANK OF INDIA",
            BankBranch    = "Kandivali East",
            BankAccountNo = "12345678901234",
            BankIFSC      = "SBIN0001234"
        },
        _ => new CompanyProfile  // Default: AVANI ENTERPRISE
        {
            CompanyName   = "AVANI ENTERPRISE",
            AddressLine1  = "Regd.Off: B-701, Royal Accord CHS, Nr.Association hall, Yoginagar, Borivali (W), Mum-91",
            AddressLine2  = "Sales Office: 38A/19 Saraswati Niwas, Gr.Floor, Near Jain Hostel, Elphinstone Road (W) Mumbai-400013.",
            Contact       = "Ph. (022) 24361252 / +91 9819811409",
            GSTIN         = "27AMCPK9770B1Z1",
            BankName      = "BANK OF BARODA",
            BankBranch    = "Yoginagar (Borivali)",
            BankAccountNo = "99680 2000 00732",
            BankIFSC      = "BARB0DBYOGI"
        }
    };

    public static InvoiceModel GetDummyInvoice(string companyName)
    {
        var company = GetCompanyProfile(companyName);

        var party = new PartyModel
        {
            Name      = "YASH COLLECTION",
            Address   = "8/A SADASHIV BHUVAN R.R.T ROAD BHAJI MARKET MULUND WEST",
            GSTIN     = "27AAEPG5504F1Z4",
            State     = "MAHARASTRA",
            StateCode = "27"
        };

        var items = new List<LineItem>
        {
            new() { SNo=1, Description="R-091",  Fit="POLO", Size="30/40", HSN="6203", Qty=57, UOM="Pcs", Rate=485, Amount=27645.00m },
            new() { SNo=2, Description="R-T-55", Fit="POLO", Size="32/38", HSN="6203", Qty=16, UOM="Pcs", Rate=485, Amount=7760.00m  }
        };

        decimal gross    = 35405.00m;
        decimal discount = 0m;
        decimal taxable  = gross - discount;
        decimal cgstAmt  = Math.Round(taxable * 0.025m, 2);
        decimal sgstAmt  = cgstAmt;
        decimal net      = Math.Round(taxable + cgstAmt + sgstAmt, 0);
        decimal roundOff = net - (taxable + cgstAmt + sgstAmt);

        var totals = new InvoiceTotals
        {
            TotalQty      = 73,
            GrossAmount   = gross,
            Discount      = discount,
            TaxableAmount = taxable,
            CgstRate      = 2.5m,
            CgstAmount    = cgstAmt,
            SgstRate      = 2.5m,
            SgstAmount    = sgstAmt,
            IgstRate      = 0,
            IgstAmount    = 0,
            RoundOff      = roundOff,
            NetAmount     = net,
            AmountInWords = NumberToWordsConverter.ConvertAmount(net)
        };

        return new InvoiceModel
        {
            InvoiceNo     = companyName == "HARDIKA CREATION" ? "H-101" : "A-289",
            Date          = "15/08/2026",
            State         = "Maharashtra",
            StateCode     = "27",
            ReverseCharge = "No",
            Transport     = "MAHENDRA",
            DateOfSupply  = "15/08/2026",
            PlaceOfSupply = "MUMBAI",
            Company       = company,
            BillingParty  = party,
            ShippingParty = party,
            Items         = items,
            Totals        = totals
        };
    }
}
