using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoicePro.UI.NewBill;

public class InvoiceItemModel : INotifyPropertyChanged
{
    private string _productDescription = string.Empty;
    private string _fit  = string.Empty;
    private string _size = string.Empty;
    private string _hsn  = string.Empty;
    private decimal _qty = 1;
    private string _uom  = "Pcs";
    private decimal _rate;

    // Row index — set by ViewModel after collection changes
    public int SNo { get; set; }

    public string ProductDescription { get => _productDescription; set => Set(ref _productDescription, value); }
    public string Fit  { get => _fit;  set => Set(ref _fit,  value); }
    public string Size { get => _size; set => Set(ref _size, value); }
    public string Hsn  { get => _hsn;  set => Set(ref _hsn,  value); }
    public string Uom  { get => _uom;  set => Set(ref _uom,  value); }

    public decimal Qty
    {
        get => _qty;
        set { Set(ref _qty, value); Recalculate(); }
    }

    public decimal Rate
    {
        get => _rate;
        set { Set(ref _rate, value); Recalculate(); }
    }

    // Read-only — Qty * Rate
    public decimal Amount { get; private set; }

    private void Recalculate()
    {
        Amount = System.Math.Round(_qty * _rate, 2);
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
