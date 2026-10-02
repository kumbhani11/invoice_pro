# Code Generation Plan — new-invoice-redesign Fix Pass 2

## Files to Modify
- `UI/NewBill/NewBillView.axaml` — primary target (all 8 issues)
- `App.axaml` — font family fix for ₹ rendering

## Steps

- [ ] Step 1: App.axaml — set FontFamily to "Segoe UI, Nirmala UI, Arial" on Window/TextBlock base styles
- [ ] Step 2: NewBillView.axaml — Root layout: 7-row Grid with correct row defs, ScrollViewer wrapper
- [ ] Step 3: NewBillView.axaml — Compact density styles (sec-hdr 26px, row 24px, input 26px, padding 6px)
- [ ] Step 4: NewBillView.axaml — Replace emoji icons with vector Path geometry (calendar, trash, printer, search removed)
- [ ] Step 5: NewBillView.axaml — Bill To / Supply To: compact 3-row layout, read-only TextBlocks, no search button, ~100px height
- [ ] Step 6: NewBillView.axaml — Company Info: label column 95px bold, colon column, value column
- [ ] Step 7: NewBillView.axaml — Invoice Details: bold right-aligned labels 100px, State as TextBox, State Code 120px wide
- [ ] Step 8: NewBillView.axaml — Typography: TAX INVOICE 22px Bold, company name 20px Bold, NET AMOUNT Bold 18px, section titles 14px SemiBold
- [ ] Step 9: NewBillView.axaml — Product table: thin vertical dividers, alternate rows, delete icon only on filled rows, UOM dropdown in cell
- [ ] Step 10: NewBillView.axaml — Right panel: summary rows 22px, Net Amount row 32px, GST breakdown rows 22px
- [ ] Step 11: NewBillView.axaml — Footer: ~80px, header 24px + four 18px rows
- [ ] Step 12: Build and verify 0 errors

## Constraints
- No changes to: InvoiceCalculationService, MasterDocumentTemplate, InvoiceDocument, any Model, NewBillViewModel business logic
