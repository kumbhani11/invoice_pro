using Avalonia.Controls;

namespace InvoicePro.UI.CompanySelection;

public partial class CompanySelectionView : Window
{
    public CompanySelectionView()
    {
        InitializeComponent();
        DataContext = new CompanySelectionViewModel(this);
    }
}
