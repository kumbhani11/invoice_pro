using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace InvoicePro.UI.NewBill;

public partial class NewBillView : UserControl
{
    public NewBillView()
    {
        InitializeComponent();
    }

    private async void PreviewButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var preview = new InvoicePreviewView
            {
                DataContext = this.DataContext,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            // Toolbar with simple zoom controls
            var fitButton = new Button { Content = "Fit Width", Width = 90 };
            var actualButton = new Button { Content = "100%", Width = 60 };
            var toolbar = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, Margin = new Thickness(6) };
            toolbar.Children.Add(fitButton);
            toolbar.Children.Add(actualButton);

            // Wrap preview in a scrollviewer so content is always reachable
            var scroller = new ScrollViewer
            {
                Content = preview
            };

            var grid = new Grid();
            grid.RowDefinitions = new RowDefinitions("Auto, *");
            Grid.SetRow(toolbar, 0);
            Grid.SetRow(scroller, 1);
            grid.Children.Add(toolbar);
            grid.Children.Add(scroller);

            var win = new Window
            {
                Title = "Invoice Preview",
                Width = 900,
                Height = 1100,
                MinWidth = 600,
                MinHeight = 600,
                CanResize = true,
                Content = grid,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var owner = this.VisualRoot as Window;

            // Fit-to-width behavior (enabled by default)
            bool fitMode = true;
            fitButton.Click += (_, __) =>
            {
                fitMode = !fitMode;
                if (fitMode)
                {
                    fitButton.Content = "Fit:On";
                    try
                    {
                        var initialWidth = (owner != null) ? owner.Bounds.Width : win.Width;
                        preview.Width = System.Math.Max(0, initialWidth - 48);
                    }
                    catch { preview.Width = System.Math.Max(0, win.Width - 48); }
                }
                else
                {
                    fitButton.Content = "Fit Width";
                    preview.Width = double.NaN;
                }
            };

            // Set initial fit mode state and preview width before showing
            fitButton.Content = "Fit:On";
            try
            {
                var initialWidth = (owner != null) ? owner.Bounds.Width : win.Width;
                preview.Width = System.Math.Max(0, initialWidth - 48);
            }
            catch { preview.Width = System.Math.Max(0, win.Width - 48); }

            actualButton.Click += (_, __) =>
            {
                fitMode = false;
                fitButton.Content = "Fit Width";
                preview.Width = double.NaN;
            };

            // Update preview width when window resizes while in fit mode
            win.PropertyChanged += (s, ev) =>
            {
                if (ev.Property == Window.BoundsProperty && fitMode)
                {
                    try
                    {
                        var rect = (Avalonia.Rect)ev.NewValue!;
                        preview.Width = System.Math.Max(0, rect.Width - 48);
                    }
                    catch { }
                }
            };

            if (owner != null)
                await win.ShowDialog(owner);
            else
                win.Show();
        }
        catch
        {
            // Swallow errors opening preview window to avoid breaking main UI
        }
    }
}