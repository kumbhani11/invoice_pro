using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace InvoicePro.UI.Backup;

public partial class BackupView : UserControl
{
    public BackupView()
    {
        InitializeComponent();
    }

    private string GetDbPath()
    {
        return InvoicePro.Data.SQLite.BillingDbContext.CurrentDatabasePath;
    }

    private async void OnBackupClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Database Backup",
            DefaultExtension = ".db",
            SuggestedFileName = $"HardikaBackup_{DateTime.Now:yyyyMMdd}.db"
        });

        if (file != null)
        {
            try
            {
                var sourcePath = GetDbPath();
                await using var sourceStream = File.OpenRead(sourcePath);
                await using var destinationStream = await file.OpenWriteAsync();
                await sourceStream.CopyToAsync(destinationStream);
                
                StatusText.Text = "Backup completed successfully!";
                StatusText.Foreground = Avalonia.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
            }
        }
    }

    private async void OnRestoreClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Database Backup to Restore",
            AllowMultiple = false
        });

        if (files != null && files.Count > 0)
        {
            try
            {
                var destPath = GetDbPath();
                await using var sourceStream = await files[0].OpenReadAsync();
                await using var destinationStream = File.Create(destPath);
                await sourceStream.CopyToAsync(destinationStream);
                
                StatusText.Text = "Restore completed! Please restart the application immediately.";
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error: " + ex.Message;
                StatusText.Foreground = Avalonia.Media.Brushes.Red;
            }
        }
    }
}