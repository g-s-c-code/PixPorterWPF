using PixPorter.WPF.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PixPorter.WPF;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        ((MainViewModel)DataContext).LogEntries.CollectionChanged += LogEntries_CollectionChanged;
    }

    private void LogEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            LogScrollViewer.ScrollToBottom();
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
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void SmoothScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer sv) return;

        e.Handled = true;

        double target = sv.VerticalOffset - (e.Delta * 0.6);
        target = Math.Max(0, Math.Min(target, sv.ScrollableHeight));

        var animation = new DoubleAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        sv.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
    }
}

public static class ScrollViewerBehavior
{
    public static readonly DependencyProperty VerticalOffsetProperty =
        DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
            new PropertyMetadata(0.0, OnVerticalOffsetChanged));

    private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer sv)
            sv.ScrollToVerticalOffset((double)e.NewValue);
    }
}