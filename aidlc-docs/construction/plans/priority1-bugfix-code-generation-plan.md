# Code Generation Plan — Priority 1 Critical Bug Fixes

## Unit: priority1-bugfix
## Date: 2025-07-16

---

## Unit Context
Fix 5 verified critical correctness bugs found during the GST invoice audit.
No new features. No schema additions beyond the Quantity decimal fix.

## Bug List (from audit)
- Bug 1: IGST never saved to DB (NewBillViewModel.SaveInvoiceAsync)
- Bug 2: Supply To preview binds to wrong source (InvoicePreviewView.axaml)
- Bug 3: Reprint hardcodes GST rates 9m (BillsViewModel.PrintSelectedAsync)
- Bug 4: Tax row label hardcoded "G.S.T 5%" (InvoiceDocument.cs + InvoicePreviewView.axaml)
- Bug 5: Quantity stored as int, calculated as decimal — truncation (InvoiceItem.cs + migration + SaveInvoiceAsync)

---

## Steps

- [ ] Step 1: Fix Bug 1 — Save IGST in NewBillViewModel.SaveInvoiceAsync
  - File: Services/NewBillViewModel.cs (UI/NewBill/NewBillViewModel.cs)
  - Change: Replace `IGST = 0` with `IGST = result.IgstAmount` (need to capture IgstAmount from CalculateTotals result)
  - Also add `_igst` backing field alongside `_cgst` and `_sgst`

- [ ] Step 2: Fix Bug 2 — Supply To preview binding in InvoicePreviewView.axaml
  - File: UI/NewBill/InvoicePreviewView.axaml
  - Change: Replace all `SelectedCustomer.*` bindings in the Supply To section with SupplyToName, SupplyToAddress, SupplyToGstin, SupplyToState, SupplyToStateCode

- [ ] Step 3: Fix Bug 3 — Reprint derives GST rates from saved amounts (BillsViewModel)
  - File: UI/Bills/BillsViewModel.cs
  - Change: Remove hardcoded CgstRate=9m/SgstRate=9m; derive rates from saved CGST/SGST/IGST amounts and TaxableAmount; also restore Fit, Size, UOM, ProductAttribute from saved InvoiceItem rows

- [ ] Step 4: Fix Bug 4 — Dynamic tax row label in InvoiceDocument.cs
  - File: Services/InvoiceDocument.cs
  - Change: Replace hardcoded "G.S.T 5%" with computed label from actual CgstRate+SgstRate or IgstRate

- [ ] Step 5: Fix Bug 4 (UI side) — Dynamic tax row label in InvoicePreviewView.axaml
  - File: UI/NewBill/InvoicePreviewView.axaml
  - Change: Replace hardcoded "G.S.T 5%" TextBlock with binding to PreviewGstLabel

- [ ] Step 6: Fix Bug 5 — Change InvoiceItem.Quantity from int to decimal
  - File: Models/InvoiceItem.cs
  - Change: `public int Quantity` → `public decimal Quantity`

- [ ] Step 7: Fix Bug 5 — Add EF Core migration for Quantity decimal
  - File: Data/Migrations/20260903000000_QuantityDecimal.cs (new)
  - Change: AlterColumn Quantity from INTEGER to TEXT (SQLite decimal)

- [ ] Step 8: Fix Bug 5 — Remove int cast in SaveInvoiceAsync
  - File: UI/NewBill/NewBillViewModel.cs
  - Change: `Quantity = (int)row.Qty` → `Quantity = row.Qty`
  - Also fix `TotalQuantity = (int)InvoiceItems.Sum(r => r.Qty)` cast

- [ ] Step 9: Update BillingDbContextModelSnapshot for Quantity decimal
  - File: Data/Migrations/BillingDbContextModelSnapshot.cs
  - Change: Update Quantity column type to TEXT/decimal

- [ ] Step 10: Write unit tests for all 5 bugs in TestRunner.cs
  - File: TestRunner.cs
  - Add: TestIgstSaved, TestSupplyToBinding (logic only), TestReprintRateDerivation, TestGstLabelDynamic, TestQuantityDecimal

- [ ] Step 11: Build and run tests
  - Command: dotnet build + TestRunner.RunAll()

- [ ] Step 12: Update plan checkboxes and aidlc-state.md

---

## Files Modified
1. UI/NewBill/NewBillViewModel.cs
2. UI/NewBill/InvoicePreviewView.axaml
3. UI/Bills/BillsViewModel.cs
4. Services/InvoiceDocument.cs
5. Models/InvoiceItem.cs
6. Data/Migrations/20260903000000_QuantityDecimal.cs (new)
7. Data/Migrations/BillingDbContextModelSnapshot.cs
8. TestRunner.cs
