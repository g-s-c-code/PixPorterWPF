using PixPorter.WPF.ViewModels;
using System.Windows;

namespace PixPorter.WPF;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.HandleDrop(paths);
        }
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            ViewModel.IsDragOver = true;
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsDragOver = false;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effects = DragDropEffects.Copy;
        else
            e.Effects = DragDropEffects.None;

        e.Handled = true;
    }

    private void InfoToggle_Click(object sender, RoutedEventArgs e)
    {
        bool isVisible = InfoPanel.Visibility == Visibility.Visible;
        InfoPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        InfoToggleChevron.Text = isVisible ? "▾" : "▴";
    }
}