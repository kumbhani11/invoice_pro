using System;
using System.IO;
using System.Linq;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoicePro;

public static class TestRunner
{
    public static int RunAll()
    {
        try
        {
            var tmpDir = Path.Combine(Path.GetTempPath(), "invoicepro_test_harness");
            Directory.CreateDirectory(tmpDir);

            var db1 = Path.Combine(tmpDir, "test_company_a.db");
            var db2 = Path.Combine(tmpDir, "test_company_b.db");

            Console.WriteLine($"Using temp folder: {tmpDir}");

            // Test against company A
            BillingDbContext.CurrentDatabasePath = db1;
            using (var db = new BillingDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.Migrate();
                db.Customers.RemoveRange(db.Customers);
                db.SaveChanges();
            }

            Console.WriteLine("DB A initialized");

            // Create
            var customer = new Customer
            {
                Name = "Test Cust",
                GSTIN = "27TEST",
                Phone = "12345",
                Address = "Addr",
                State = "MAHARASHTRA",
                StateCode = "27"
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
                Console.WriteLine($"Create OK (Id={c.Id})");

                // Update
                c.Phone = "54321";
                db.Customers.Update(c);
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "Test Cust");
                if (c == null || c.Phone != "54321") throw new Exception("Update failed");
                Console.WriteLine("Update OK");

                // Delete
                db.Customers.Remove(c);
                db.SaveChanges();
            }

            using (var db = new BillingDbContext())
            {
                var c = db.Customers.FirstOrDefault(x => x.Name == "Test Cust");
                if (c != null) throw new Exception("Delete failed");
                Console.WriteLine("Delete OK");
            }

            // Company isolation
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

            Console.WriteLine("Company isolation OK");

            Console.WriteLine("All tests passed");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
            Console.WriteLine(ex);
            return 1;
        }
    }
}
