using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BreezeLink.UI.ViewModels;

namespace BreezeLink.UI.Controls;

public sealed partial class NodeManagementDialog : ContentDialog
{
    public NodeManagementViewModel ViewModel { get; set; } = null!;

    public NodeManagementDialog()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载数据
        _ = ViewModel.LoadDataAsync();
    }

}
