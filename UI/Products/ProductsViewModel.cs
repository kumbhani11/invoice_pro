using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoicePro.Data.SQLite;
using InvoicePro.Models;
using InvoicePro.ViewModels;

namespace InvoicePro.UI.Products;

public partial class ProductsViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<Product> _productsList = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private Product _currentProduct = new();

    public ProductsViewModel()
    {
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        using var db = new BillingDbContext();
        var query = db.Products.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(c => c.ProductCode.Contains(SearchText) || c.Description.Contains(SearchText));
        }
            
        ProductsList = new ObservableCollection<Product>(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query));
    }

    async partial void OnSearchTextChanged(string value)
    {
        await LoadProductsAsync();
    }

    [RelayCommand]
    private void AddNew()
    {
        CurrentProduct = new Product();
        IsEditing = true;
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedProduct == null) return;
        
        CurrentProduct = new Product
        {
            Id = SelectedProduct.Id,
            ProductCode = SelectedProduct.ProductCode,
            Description = SelectedProduct.Description,
            Fit = SelectedProduct.Fit,
            Size = SelectedProduct.Size,
            HSN = SelectedProduct.HSN,
            UOM = SelectedProduct.UOM,
            Rate = SelectedProduct.Rate
        };
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        using var db = new BillingDbContext();
        if (CurrentProduct.Id == 0)
        {
            db.Products.Add(CurrentProduct);
        }
        else
        {
            db.Products.Update(CurrentProduct);
        }
        await db.SaveChangesAsync();
        
        IsEditing = false;
        await LoadProductsAsync();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedProduct == null) return;
        using var db = new BillingDbContext();
        db.Products.Remove(SelectedProduct);
        db.SaveChanges();
        _ = LoadProductsAsync();
    }
}
