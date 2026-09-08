using Avalonia.Controls;
using InvoicePro.UI;
using InvoicePro.UI.CompanySelection;

namespace InvoicePro.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Provide a way for viewmodels to open the company selection dialog.
        NavigationService.OpenCompanySelection = async () =>
        {
            var dlg = new CompanySelectionView(createMainWindowOnSelect: false);
            await dlg.ShowDialog(this);
        };
    }
}