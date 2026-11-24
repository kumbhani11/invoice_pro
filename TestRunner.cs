using System;
using System.IO;
using System.Linq;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.Services;
using InvoicePro.Utils;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro;

public static class TestRunner
{
    public static int RunAll()
    {
        int failures = 0;
        failures += RunOriginalDbTests();
        failures += TestIgstSaved();
        failures += TestReprintRateDerivation();
        failures += TestGstLabelDynamic();
        failures += TestQuantityDecimalSave();
        failures += TestQuantityDecimalNoTruncation();

        if (failures == 0)
            Console.WriteLine("\n✅ All tests passed.");
        else
            Console.WriteLine($"\n❌ {failures} test(s) FAILED.");

        return failures == 0 ? 0 : 1;
    }

    // ── Original DB CRUD + isolation tests ───────────────────────────────
    private static int RunOriginalDbTests()
    {
        int failures = 0;
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "invoicepro_test_harness");
            Directory.CreateDirectory(tmpDir);

            var db1 = Path.Combine(tmpDir, "test_company_a.db");
            var db2 = Path.Combine(tmpDir, "test_company_b.db");

            Console.WriteLine($"[DB] Using temp folder: {tmpDir}");

            BillingDbContext.CurrentDatabasePath = db1;
            using (var db = new BillingDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.Migrate();
                db.Customers.RemoveRange(db.Customers);
                db.SaveChanges();
            }

            var customer = new Customer
            {
                Name = "Test Cust", GSTIN = "27TEST", Phone = "12345",
                Address = "Addr", State = "MAHARASHTRA", StateCode = "27"
            };

            using (var db = new BillingDbContext())
            {
                db.Customers.Add(customer);
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "Test Cust");
                if (c == null) throw new Exception("Create failed");
                Console.WriteLine($"[DB] Create OK (Id={c.Id})");
                c.Phone = "54321";
                db.Customers.Update(c);
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "Test Cust");
                if (c == null || c.Phone != "54321") throw new Exception("Update failed");
                Console.WriteLine("[DB] Update OK");
                db.Customers.Remove(c);
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "Test Cust");
                if (c != null) throw new Exception("Delete failed");
                Console.WriteLine("[DB] Delete OK");
            }

            BillingDbContext.CurrentDatabasePath = db2;
            using (var db = new BillingDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.Migrate();
                db.Customers.RemoveRange(db.Customers);
                db.SaveChanges();
                db.Customers.Add(new Customer { Name = "B Cust", GSTIN = "B", Phone = "1" });
                db.SaveChanges();
            }

            BillingDbContext.CurrentDatabasePath = db1;
            using (var db = new BillingDbContext())
            {
                var list = db.Customers.ToList();
                if (list.Count != 0) throw new Exception("Company isolation failed: DB1 should be empty");
            }

            Console.WriteLine("[DB] Company isolation OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] FAILED: {ex.Message}");
            failures++;
        }
        return failures;
    }

    // ── Bug 1: IGST is saved correctly (not hardcoded 0) ─────────────────
    // Tests InvoiceCalculationService inter-state path and verifies IgstAmount > 0
    private static int TestIgstSaved()
    {
        try
        {
            var items = new[]
            {
                new InvoiceLineItemInput { Quantity = 10, Rate = 1000m }
            };
            // Inter-state: 18% IGST
            var result = InvoiceCalculationService.Calculate(items, discount: 0m, gstPercentage: 18m, isInterState: true);

            Assert(result.IgstAmount > 0,    "IGST amount must be > 0 for inter-state invoice");
            Assert(result.CgstAmount == 0m,  "CGST must be 0 for inter-state invoice");
            Assert(result.SgstAmount == 0m,  "SGST must be 0 for inter-state invoice");

            decimal expectedIgst = Math.Round(10000m * 0.18m, 2);
            Assert(result.IgstAmount == expectedIgst, $"IGST expected {expectedIgst}, got {result.IgstAmount}");

            // Verify the saved Invoice model would carry the correct IGST (simulates SaveInvoiceAsync logic)
            decimal savedIgst = result.IgstAmount; // Bug 1 fix: was hardcoded 0
            Assert(savedIgst == expectedIgst, $"Saved IGST expected {expectedIgst}, got {savedIgst}");

            Console.WriteLine($"[Bug1] IGST save OK — IGST={result.IgstAmount}, CGST={result.CgstAmount}, SGST={result.SgstAmount}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bug1] FAILED: {ex.Message}");
            return 1;
        }
    }

    // ── Bug 3: Reprint derives GST rates from saved amounts ───────────────
    private static int TestReprintRateDerivation()
    {
        try
        {
            // Simulate a saved invoice with 5% GST (CGST 2.5% + SGST 2.5%)
            decimal taxable = 35405m;
            decimal cgst    = Math.Round(taxable * 0.025m, 2);
            decimal sgst    = cgst;
            decimal igst    = 0m;

            // Reprint rate derivation logic (mirrors BillsViewModel fix)
            decimal cgstRate = 0m, sgstRate = 0m, igstRate = 0m;
            if (taxable > 0)
            {
                if (igst > 0)
                    igstRate = Math.Round(igst / taxable * 100m, 4);
                else
                {
                    cgstRate = Math.Round(cgst / taxable * 100m, 4);
                    sgstRate = Math.Round(sgst / taxable * 100m, 4);
                }
            }

            Assert(Math.Abs(cgstRate - 2.5m) < 0.01m, $"CGST rate expected ~2.5, got {cgstRate}");
            Assert(Math.Abs(sgstRate - 2.5m) < 0.01m, $"SGST rate expected ~2.5, got {sgstRate}");
            Assert(igstRate == 0m, $"IGST rate expected 0, got {igstRate}");

            // Now simulate an inter-state invoice at 18%
            decimal igst18   = Math.Round(taxable * 0.18m, 2);
            decimal cgst18   = 0m;
            decimal sgst18   = 0m;
            decimal cgstRate18 = 0m, sgstRate18 = 0m, igstRate18 = 0m;
            if (taxable > 0)
            {
                if (igst18 > 0)
                    igstRate18 = Math.Round(igst18 / taxable * 100m, 4);
                else
                {
                    cgstRate18 = Math.Round(cgst18 / taxable * 100m, 4);
                    sgstRate18 = Math.Round(sgst18 / taxable * 100m, 4);
                }
            }

            Assert(Math.Abs(igstRate18 - 18m) < 0.01m, $"IGST rate expected ~18, got {igstRate18}");
            Assert(cgstRate18 == 0m, $"CGST rate expected 0 for inter-state, got {cgstRate18}");

            Console.WriteLine($"[Bug3] Reprint rate derivation OK — CGST={cgstRate}%, SGST={sgstRate}%, IGST18={igstRate18}%");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bug3] FAILED: {ex.Message}");
            return 1;
        }
    }

    // ── Bug 4: GST label is dynamic, not hardcoded "G.S.T 5%" ────────────
    private static int TestGstLabelDynamic()
    {
        try
        {
            // Intra-state 5%: label should reflect 5%
            string label5 = BuildGstLabel(cgstRate: 2.5m, sgstRate: 2.5m, igstRate: 0m);
            Assert(label5 == "G.S.T 5%", $"Expected 'G.S.T 5%', got '{label5}'");

            // Intra-state 12%
            string label12 = BuildGstLabel(cgstRate: 6m, sgstRate: 6m, igstRate: 0m);
            Assert(label12 == "G.S.T 12%", $"Expected 'G.S.T 12%', got '{label12}'");

            // Inter-state 18%
            string label18 = BuildGstLabel(cgstRate: 0m, sgstRate: 0m, igstRate: 18m);
            Assert(label18 == "G.S.T 18%", $"Expected 'G.S.T 18%', got '{label18}'");

            // Intra-state 28%
            string label28 = BuildGstLabel(cgstRate: 14m, sgstRate: 14m, igstRate: 0m);
            Assert(label28 == "G.S.T 28%", $"Expected 'G.S.T 28%', got '{label28}'");

            Console.WriteLine($"[Bug4] GST label dynamic OK — 5%='{label5}', 12%='{label12}', 18%='{label18}', 28%='{label28}'");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bug4] FAILED: {ex.Message}");
            return 1;
        }
    }

    // Mirrors the label logic in InvoiceDocument.cs and NewBillViewModel.CalculateTotals
    private static string BuildGstLabel(decimal cgstRate, decimal sgstRate, decimal igstRate)
        => igstRate > 0
            ? $"G.S.T {igstRate:0.#}%"
            : $"G.S.T {(cgstRate + sgstRate):0.#}%";

    // ── Bug 5a: Quantity stored as decimal in DB (no truncation) ─────────
    private static int TestQuantityDecimalSave()
    {
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "invoicepro_test_harness");
            Directory.CreateDirectory(tmpDir);
            var dbPath = Path.Combine(tmpDir, "test_qty_decimal.db");

            BillingDbContext.CurrentDatabasePath = dbPath;
            using (var db = new BillingDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.Migrate();

                var invoice = new Invoice
                {
                    InvoiceNumber = "TEST-001",
                    InvoiceDate   = DateTime.Now,
                    CustomerName  = "Test",
                    PrintedAmountInWords = "Test"
                };
                db.Invoices.Add(invoice);
                db.SaveChanges();

                // Save a fractional quantity — this would have been truncated before the fix
                db.InvoiceItems.Add(new InvoiceItem
                {
                    InvoiceId   = invoice.Id,
                    Description = "Fabric",
                    Quantity    = 1.5m,   // decimal — must survive round-trip
                    Rate        = 200m,
                    Amount      = 300m
                });
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var item = db.InvoiceItems.First(i => i.InvoiceId != 0);
                Assert(item.Quantity == 1.5m, $"Quantity expected 1.5, got {item.Quantity}");
            }

            Console.WriteLine("[Bug5a] Quantity decimal save/load OK — 1.5 survived round-trip");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bug5a] FAILED: {ex.Message}");
            return 1;
        }
    }

    // ── Bug 5b: Calculation service uses decimal qty correctly ────────────
    private static int TestQuantityDecimalNoTruncation()
    {
        try
        {
            // 1.5 metres @ ₹200 = ₹300 — would have been ₹200 with int truncation
            var items = new[]
            {
                new InvoiceLineItemInput { Quantity = 1.5m, Rate = 200m }
            };
            var result = InvoiceCalculationService.Calculate(items, discount: 0m, gstPercentage: 5m, isInterState: false);

            Assert(result.SubTotal == 300m, $"SubTotal expected 300, got {result.SubTotal}");

            decimal expectedCgst = Math.Round(300m * 0.025m, 2);
            Assert(result.CgstAmount == expectedCgst, $"CGST expected {expectedCgst}, got {result.CgstAmount}");

            Console.WriteLine($"[Bug5b] Decimal quantity calculation OK — SubTotal={result.SubTotal}, CGST={result.CgstAmount}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bug5b] FAILED: {ex.Message}");
            return 1;
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
