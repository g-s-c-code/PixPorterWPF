using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace PixPorter.WPF.ViewModels;

public enum ConversionStatus
{
    Pending,
    Converting,
    Done,
    Failed
}

public partial class ConversionItemViewModel : ObservableObject
{
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _fileSize = string.Empty;
    [ObservableProperty] private ConversionStatus _status = ConversionStatus.Pending;
    [ObservableProperty] private string _statusText = "Pending";
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSelected = false;

    public ConversionItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);

        try
        {
            long bytes = new FileInfo(filePath).Length;
            FileSize = FormatBytes(bytes);
        }
        catch
        {
            FileSize = string.Empty;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F0} KB";
        return $"{bytes} B";
    }
}