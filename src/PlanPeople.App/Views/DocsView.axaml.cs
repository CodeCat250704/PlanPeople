using System;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using PlanPeople.App.ViewModels;

namespace PlanPeople.App.Views;

public partial class DocsView : UserControl
{
    public DocsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DocsViewModel vm)
        {
            vm.RequestImportFile = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择 Markdown 文件",
                    AllowMultiple = false,
                    FileTypeFilter = new[] { new FilePickerFileType("Markdown") { Patterns = new[] { "*.md", "*.markdown" } } }
                });
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            };
        }
    }
}