using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoicePro.UI.NewBill;

/// <summary>
/// Observable row model for the invoice DataGrid.
/// Amount recalculates automatically when Qty, Rate, or TaxPercent changes.
/// </summary>
public class InvoiceItemModel : INotifyPropertyChanged
{
    private string _productCode = string.Empty;
    private string _description = string.Empty;
    private string _hsn = string.Empty;
    private int _quantity = 1;
    private decimal _rate;
    private decimal _taxPercent;

    public string ProductCode  { get => _productCode;  set => Set(ref _productCode,  value); }
    public string Description  { get => _description;  set => Set(ref _description,  value); }
    public string HSN          { get => _hsn;          set => Set(ref _hsn,          value); }

    public int Quantity
    {
        get => _quantity;
        set { Set(ref _quantity, value); Recalculate(); }
    }

    public decimal Rate
    {
        get => _rate;
        set { Set(ref _rate, value); Recalculate(); }
    }

    public decimal TaxPercent
    {
        get => _taxPercent;
        set { Set(ref _taxPercent, value); Recalculate(); }
    }

    // Read-only — updated by Recalculate()
    public decimal TaxAmount { get; private set; }
    public decimal Amount    { get; private set; }

    // Row index shown in the # column — set by the ViewModel after collection changes
    public int ItemIndex { get; set; }

    private void Recalculate()
    {
        decimal baseAmount = Quantity * Rate;
        TaxAmount = System.Math.Round(baseAmount * TaxPercent / 100m, 2);
        Amount    = baseAmount + TaxAmount;
        OnPropertyChanged(nameof(TaxAmount));
        OnPropertyChanged(nameof(Amount));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged(string? name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
