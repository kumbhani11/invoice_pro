using Avalonia.Controls;

namespace InvoicePro.UI.CompanySelection;

public partial class CompanySelectionView : Window
{
    public CompanySelectionView()
    {
        InitializeComponent();
        DataContext = new CompanySelectionViewModel(this);
    }

    public CompanySelectionView(bool createMainWindowOnSelect)
    {
        InitializeComponent();
        DataContext = new CompanySelectionViewModel(this, createMainWindowOnSelect);
    }
}
