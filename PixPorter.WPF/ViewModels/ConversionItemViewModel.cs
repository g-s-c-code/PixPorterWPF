using CommunityToolkit.Mvvm.ComponentModel;
using PixPorter.Common.Helpers;
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
    [ObservableProperty] private string? _outputFileSize;
    [ObservableProperty] private bool _isSelected = false;

    public ConversionItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        FileSize = FileSizeFormatter.DescribeFile(filePath);
    }
}