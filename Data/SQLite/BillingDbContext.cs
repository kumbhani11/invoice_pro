using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using InvoicePro.Models;

namespace InvoicePro.Data.SQLite;

public class BillingDbContext : DbContext
{
    public static string CurrentDatabasePath { get; set; } = string.Empty;

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<InvoiceItem> InvoiceItems { get; set; } = null!;
    public DbSet<TaxConfiguration> TaxConfigurations { get; set; } = null!;
    public DbSet<ApplicationSettings> ApplicationSettings { get; set; } = null!;

    public BillingDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = string.IsNullOrEmpty(CurrentDatabasePath) ? "fallback.db" : CurrentDatabasePath;
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
        
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define foreign key constraints and decimal precisions if needed
        modelBuilder.Entity<Invoice>()
            .HasMany(e => e.Items)
            .WithOne(e => e.Invoice)
            .HasForeignKey(e => e.InvoiceId)
            .IsRequired();
            
        // SQLite doesn't natively support decimal precision limits like SQL Server,
        // but it's good practice for when migrations are generated.
    }
}
