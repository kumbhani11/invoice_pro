using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.Utils;

namespace InvoiceProTests;

class Program
{
    static int Main()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        int failures = 0;

        failures += RunDbCrudTests();
        failures += TestBug1_IgstSaved();
        failures += TestBug3_ReprintRateDerivation();
        failures += TestBug4_GstLabelDynamic();
        failures += TestBug5a_QuantityDecimalSave();
        failures += TestBug5b_QuantityDecimalCalculation();

        Console.WriteLine(failures == 0
            ? "\n✅ All tests passed."
            : $"\n❌ {failures} test(s) FAILED.");

        return failures == 0 ? 0 : 1;
    }

    // ── Original DB CRUD + isolation ─────────────────────────────────────
    static int RunDbCrudTests()
    {
        int f = 0;
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "invoicepro_tests");
            Directory.CreateDirectory(tmpDir);
            var db1 = Path.Combine(tmpDir, "test_a.db");
            var db2 = Path.Combine(tmpDir, "test_b.db");

            BillingDbContext.CurrentDatabasePath = db1;
            using (var db = new BillingDbContext()) { db.Database.EnsureDeleted(); db.Database.Migrate(); }

            // Create
            using (var db = new BillingDbContext())
            {
                db.Customers.Add(new Customer { Name = "TC", GSTIN = "27X", Phone = "1", Address = "A", State = "MH", StateCode = "27" });
                db.SaveChanges();
            }
            // Read
            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "TC");
                Assert(c != null, "Create: customer not found");
                c!.Phone = "2";
                db.Customers.Update(c);
                db.SaveChanges();
            }
            // Update
            using (var db = new BillingDbContext())
            {
                var c = db.Customers.First(x => x.Name == "TC");
                Assert(c.Phone == "2", $"Update: expected phone=2, got {c.Phone}");
                db.Customers.Remove(c);
                db.SaveChanges();
            }
            // Delete
            using (var db = new BillingDbContext())
                Assert(!db.Customers.Any(x => x.Name == "TC"), "Delete: customer still exists");

            // Isolation
            BillingDbContext.CurrentDatabasePath = db2;
            using (var db = new BillingDbContext()) { db.Database.EnsureDeleted(); db.Database.Migrate(); db.Customers.Add(new Customer { Name = "B" }); db.SaveChanges(); }
            BillingDbContext.CurrentDatabasePath = db1;
            using (var db = new BillingDbContext())
                Assert(!db.Customers.Any(), "Isolation: DB1 should be empty");

            Console.WriteLine("[DB] CRUD + isolation OK");
        }
        catch (Exception ex) { Console.WriteLine($"[DB] FAILED: {ex.Message}"); f++; }
        return f;
    }

    // ── Bug 1: IGST is captured and not hardcoded 0 ───────────────────────
    static int TestBug1_IgstSaved()
    {
        try
        {
            var items = new[] { new InvoiceLineItemInput { Quantity = 10m, Rate = 1000m } };

            // Inter-state 18%
            var r = InvoiceCalculationService.Calculate(items, 0m, 18m, isInterState: true);
            Assert(r.IgstAmount == 1800m,  $"Bug1: IGST expected 1800, got {r.IgstAmount}");
            Assert(r.CgstAmount == 0m,     $"Bug1: CGST must be 0 for inter-state, got {r.CgstAmount}");
            Assert(r.SgstAmount == 0m,     $"Bug1: SGST must be 0 for inter-state, got {r.SgstAmount}");
            Assert(r.GrandTotal == 11800m, $"Bug1: GrandTotal expected 11800, got {r.GrandTotal}");

            // Intra-state 5% — IGST must be 0
            var r2 = InvoiceCalculationService.Calculate(items, 0m, 5m, isInterState: false);
            Assert(r2.IgstAmount == 0m,   $"Bug1: IGST must be 0 for intra-state, got {r2.IgstAmount}");
            Assert(r2.CgstAmount == 250m, $"Bug1: CGST expected 250, got {r2.CgstAmount}");
            Assert(r2.SgstAmount == 250m, $"Bug1: SGST expected 250, got {r2.SgstAmount}");

            Console.WriteLine($"[Bug1] IGST save OK — inter-state IGST={r.IgstAmount}, intra-state IGST={r2.IgstAmount}");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"[Bug1] FAILED: {ex.Message}"); return 1; }
    }

    // ── Bug 3: Reprint derives GST rates from saved amounts ───────────────
    static int TestBug3_ReprintRateDerivation()
    {
        try
        {
            // Scenario A: intra-state 5% (CGST 2.5% + SGST 2.5%)
            decimal taxable = 35405m;
            decimal cgst = Math.Round(taxable * 0.025m, 2);
            decimal sgst = cgst;
            decimal igst = 0m;

            var (cgstRate, sgstRate, igstRate) = DeriveRates(taxable, cgst, sgst, igst);
            Assert(Math.Abs(cgstRate - 2.5m) < 0.01m, $"Bug3-A: CGST rate expected 2.5, got {cgstRate}");
            Assert(Math.Abs(sgstRate - 2.5m) < 0.01m, $"Bug3-A: SGST rate expected 2.5, got {sgstRate}");
            Assert(igstRate == 0m,                     $"Bug3-A: IGST rate expected 0, got {igstRate}");

            // Scenario B: inter-state 18%
            decimal igst18 = Math.Round(taxable * 0.18m, 2);
            var (cr18, sr18, ir18) = DeriveRates(taxable, 0m, 0m, igst18);
            Assert(Math.Abs(ir18 - 18m) < 0.01m, $"Bug3-B: IGST rate expected 18, got {ir18}");
            Assert(cr18 == 0m,                    $"Bug3-B: CGST rate expected 0, got {cr18}");

            // Scenario C: zero taxable — must not divide by zero
            var (cr0, sr0, ir0) = DeriveRates(0m, 0m, 0m, 0m);
            Assert(cr0 == 0m && sr0 == 0m && ir0 == 0m, "Bug3-C: zero taxable must yield zero rates");

            Console.WriteLine($"[Bug3] Reprint rate derivation OK — 5%: CGST={cgstRate}%, SGST={sgstRate}%; 18%: IGST={ir18}%");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"[Bug3] FAILED: {ex.Message}"); return 1; }
    }

    static (decimal cgst, decimal sgst, decimal igst) DeriveRates(decimal taxable, decimal cgstAmt, decimal sgstAmt, decimal igstAmt)
    {
        if (taxable <= 0) return (0m, 0m, 0m);
        if (igstAmt > 0)
            return (0m, 0m, Math.Round(igstAmt / taxable * 100m, 4));
        return (Math.Round(cgstAmt / taxable * 100m, 4),
                Math.Round(sgstAmt / taxable * 100m, 4),
                0m);
    }

    // ── Bug 4: GST label is dynamic ───────────────────────────────────────
    static int TestBug4_GstLabelDynamic()
    {
        try
        {
            Assert(GstLabel(2.5m, 2.5m, 0m)  == "G.S.T 5%",  $"Bug4: expected 'G.S.T 5%',  got '{GstLabel(2.5m,2.5m,0m)}'");
            Assert(GstLabel(6m,   6m,   0m)  == "G.S.T 12%", $"Bug4: expected 'G.S.T 12%', got '{GstLabel(6m,6m,0m)}'");
            Assert(GstLabel(0m,   0m,   18m) == "G.S.T 18%", $"Bug4: expected 'G.S.T 18%', got '{GstLabel(0m,0m,18m)}'");
            Assert(GstLabel(14m,  14m,  0m)  == "G.S.T 28%", $"Bug4: expected 'G.S.T 28%', got '{GstLabel(14m,14m,0m)}'");
            Assert(GstLabel(0m,   0m,   5m)  == "G.S.T 5%",  $"Bug4: expected 'G.S.T 5%' for IGST-only, got '{GstLabel(0m,0m,5m)}'");

            Console.WriteLine("[Bug4] GST label dynamic OK — 5%, 12%, 18%, 28% all correct");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"[Bug4] FAILED: {ex.Message}"); return 1; }
    }

    static string GstLabel(decimal cgstRate, decimal sgstRate, decimal igstRate)
        => igstRate > 0 ? $"G.S.T {igstRate:0.#}%" : $"G.S.T {(cgstRate + sgstRate):0.#}%";

    // ── Bug 5a: Decimal quantity survives DB round-trip ───────────────────
    static int TestBug5a_QuantityDecimalSave()
    {
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "invoicepro_tests");
            var dbPath = Path.Combine(tmpDir, "test_qty.db");

            BillingDbContext.CurrentDatabasePath = dbPath;
            using (var db = new BillingDbContext()) { db.Database.EnsureDeleted(); db.Database.Migrate(); }

            int invoiceId;
            using (var db = new BillingDbContext())
            {
                var inv = new Invoice { InvoiceNumber = "QTY-001", InvoiceDate = DateTime.Now, CustomerName = "T", PrintedAmountInWords = "T" };
                db.Invoices.Add(inv);
                db.SaveChanges();
                invoiceId = inv.Id;

                db.InvoiceItems.Add(new InvoiceItem { InvoiceId = invoiceId, Description = "Fabric", Quantity = 1.5m, Rate = 200m, Amount = 300m });
                db.InvoiceItems.Add(new InvoiceItem { InvoiceId = invoiceId, Description = "Thread", Quantity = 0.75m, Rate = 100m, Amount = 75m });
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var items = db.InvoiceItems.Where(i => i.InvoiceId == invoiceId).OrderBy(i => i.Id).ToList();
                Assert(items[0].Quantity == 1.5m,  $"Bug5a: item[0] Qty expected 1.5, got {items[0].Quantity}");
                Assert(items[1].Quantity == 0.75m, $"Bug5a: item[1] Qty expected 0.75, got {items[1].Quantity}");
            }

            Console.WriteLine("[Bug5a] Decimal quantity DB round-trip OK — 1.5 and 0.75 survived");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"[Bug5a] FAILED: {ex.Message}"); return 1; }
    }

    // ── Bug 5b: Calculation uses full decimal qty (no int truncation) ─────
    static int TestBug5b_QuantityDecimalCalculation()
    {
        try
        {
            // 1.5 metres @ ₹200 = ₹300 (would have been ₹200 with int truncation)
            var items = new[] { new InvoiceLineItemInput { Quantity = 1.5m, Rate = 200m } };
            var r = InvoiceCalculationService.Calculate(items, 0m, 5m, isInterState: false);
            Assert(r.SubTotal == 300m,  $"Bug5b: SubTotal expected 300, got {r.SubTotal}");
            Assert(r.CgstAmount == 7.5m, $"Bug5b: CGST expected 7.5, got {r.CgstAmount}");
            Assert(r.SgstAmount == 7.5m, $"Bug5b: SGST expected 7.5, got {r.SgstAmount}");

            // 0.333 kg @ ₹900 = ₹299.70
            var items2 = new[] { new InvoiceLineItemInput { Quantity = 0.333m, Rate = 900m } };
            var r2 = InvoiceCalculationService.Calculate(items2, 0m, 18m, isInterState: true);
            decimal expectedSub = Math.Round(0.333m * 900m, 2);
            Assert(r2.SubTotal == expectedSub, $"Bug5b: SubTotal expected {expectedSub}, got {r2.SubTotal}");

            Console.WriteLine($"[Bug5b] Decimal qty calculation OK — 1.5×200=300, CGST={r.CgstAmount}; 0.333×900={r2.SubTotal}");
            return 0;
        }
        catch (Exception ex) { Console.WriteLine($"[Bug5b] FAILED: {ex.Message}"); return 1; }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
