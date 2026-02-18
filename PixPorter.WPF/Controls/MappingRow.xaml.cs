using System.Windows;
using System.Windows.Controls;

namespace PixPorter.WPF;

public partial class MappingRow : UserControl
{
    public static readonly DependencyProperty FromProperty =
        DependencyProperty.Register(nameof(From), typeof(string), typeof(MappingRow),
            new PropertyMetadata(string.Empty, OnFromChanged));

    public static readonly DependencyProperty ToProperty =
        DependencyProperty.Register(nameof(To), typeof(string), typeof(MappingRow),
            new PropertyMetadata(string.Empty, OnToChanged));

    public string From
    {
        get => (string)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public string To
    {
        get => (string)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public MappingRow()
    {
        InitializeComponent();
    }

    private static void OnFromChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MappingRow row)
            row.FromText.Text = (string)e.NewValue;
    }

    private static void OnToChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MappingRow row)
            row.ToText.Text = (string)e.NewValue;
    }
}