using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PixPorter.Common.Core;
using PixPorter.Common.Helpers;
using PixPorter.WPF.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace PixPorter.WPF.ViewModels;

public enum LogLevel { Info, Success, Error }

public class LogEntry
{
    public string Timestamp { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public LogLevel Level { get; init; } = LogLevel.Info;
}

public partial class MainViewModel : ObservableObject
{
    public const string DefaultFormat = "Default";

    public IReadOnlyList<string> FormatOptions { get; } =
        [DefaultFormat, "WebP", "PNG", "JPG", "GIF", "BMP", "TIFF", "TGA", "QOI", "PBM"];

    [ObservableProperty]
    private ObservableCollection<ConversionItemViewModel> _queuedFiles = [];

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = [];

    private string _selectedFormatOption = DefaultFormat;
    public string SelectedFormatOption
    {
        get => _selectedFormatOption;
        set
        {
            if (SetProperty(ref _selectedFormatOption, value))
            {
                OnPropertyChanged(nameof(QualitySupported));
                OnPropertyChanged(nameof(QualityHint));
                if (!QualitySupported)
                    Quality = 100;
            }
        }
    }

    private string? SelectedFormat => SelectedFormatOption == DefaultFormat ? null : SelectedFormatOption;

    [ObservableProperty] private int _quality = 100;
    [ObservableProperty] private bool _isConverting = false;
    [ObservableProperty] private bool _isDragOver = false;
    [ObservableProperty] private bool _isDark = true;
    [ObservableProperty] private bool _stripMetadata = false;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage = false;
    [ObservableProperty] private bool _statusIsError = false;

    public bool QualitySupported =>
        SelectedFormat is null
            ? QueuedFiles.Count == 0 || QueuedFiles.All(f => IsQualitySupported(Path.GetExtension(f.FilePath)))
            : IsQualitySupported(SelectedFormat);

    public string QualityHint =>
        !QualitySupported && SelectedFormat is null && QueuedFiles.Count > 0
            ? "Some queued files do not support quality adjustment."
            : QualitySupported
                ? "Applies to JPG, PNG, and WebP only."
                : "Quality not supported for this format.";

    public bool HasSelection => QueuedFiles.Any(f => f.IsSelected);
    public string ConvertButtonText => HasSelection ? "Convert Selected" : "Convert All";

    public bool AllSelected
    {
        get => QueuedFiles.Count > 0 && QueuedFiles.All(f => f.IsSelected);
        set
        {
            foreach (var item in QueuedFiles)
                item.IsSelected = value;
            OnPropertyChanged(nameof(AllSelected));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ConvertButtonText));
        }
    }

    private static bool IsQualitySupported(string? ext) =>
        ext is ".webp" or ".png" or ".jpg" or ".jpeg";

    private static string FormatOutputBytes(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024.0:F0} KB";
        return $"{bytes} B";
    }

    partial void OnIsDarkChanged(bool value) => ThemeService.Instance.Apply(value);

    partial void OnQueuedFilesChanged(ObservableCollection<ConversionItemViewModel> value)
        => SubscribeToQueueChanges(value);

    public MainViewModel()
    {
        SubscribeToQueueChanges(QueuedFiles);
    }

    private void SubscribeToQueueChanges(ObservableCollection<ConversionItemViewModel> collection)
        => collection.CollectionChanged += OnQueueCollectionChanged;

    private void OnQueueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ConversionItemViewModel item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;

        if (e.OldItems != null)
            foreach (ConversionItemViewModel item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;

        RefreshDerivedProperties();
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConversionItemViewModel.IsSelected))
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(AllSelected));
            OnPropertyChanged(nameof(ConvertButtonText));
        }
    }

    private void AddLog(string message, LogLevel level = LogLevel.Info)
    {
        LogEntries.Add(new LogEntry
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss"),
            Message = message,
            Level = level
        });
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (QueuedFiles.Count == 0)
            return;

        List<ConversionItemViewModel> toConvert = (HasSelection
            ? QueuedFiles.Where(f => f.IsSelected)
            : QueuedFiles).ToList();

        IsConverting = true;
        ClearStatus();

        AddLog($"Starting conversion of {toConvert.Count} file{(toConvert.Count == 1 ? "" : "s")}.");

        int success = 0;
        int failed = 0;

        foreach (var item in toConvert)
        {
            item.Status = ConversionStatus.Converting;
            item.StatusText = "—";

            await Task.Run(() =>
            {
                try
                {
                    string sourceExt = Path.GetExtension(item.FilePath);
                    string targetExt = SelectedFormat is not null
                        ? $".{SelectedFormat.ToLowerInvariant()}"
                        : Constants.GetDefaultTarget(sourceExt);
                    int? quality = QualitySupported ? Quality : null;

                    ConversionHelper.ConvertFile(item.FilePath, targetExt, quality, StripMetadata);

                    string outputPath = Path.ChangeExtension(item.FilePath, targetExt);
                    string outputSize = string.Empty;

                    try
                    {
                        long bytes = new FileInfo(outputPath).Length;
                        outputSize = FormatOutputBytes(bytes);
                    }
                    catch { }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        item.Status = ConversionStatus.Done;
                        item.StatusText = targetExt.TrimStart('.').ToUpperInvariant();
                        item.OutputFileSize = outputSize;

                        string qualityNote = quality.HasValue ? $"  quality={quality}" : string.Empty;
                        string metaNote = StripMetadata ? "  stripmeta=true" : string.Empty;
                        string sizeNote = !string.IsNullOrEmpty(item.FileSize) && !string.IsNullOrEmpty(outputSize)
                            ? $"  {item.FileSize} → {outputSize}"
                            : string.Empty;

                        AddLog(
                            $"[OK]  {item.FileName}  →  {Path.GetFileName(outputPath)}{sizeNote}{qualityNote}{metaNote}\n" +
                            $"      {outputPath}",
                            LogLevel.Success);
                    });

                    success++;
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        item.Status = ConversionStatus.Failed;
                        item.StatusText = "Failed";
                        item.ErrorMessage = ex.Message;

                        AddLog($"[ERR] {item.FileName}\n      {ex.Message}", LogLevel.Error);
                    });

                    failed++;
                }
            });
        }

        IsConverting = false;

        if (failed == 0)
        {
            AddLog($"Done. {success} file{(success == 1 ? "" : "s")} converted successfully.", LogLevel.Success);
            ShowStatus($"✓  {success} file{(success == 1 ? "" : "s")} converted successfully.", isError: false);
        }
        else
        {
            AddLog($"Done. {success} succeeded, {failed} failed.", LogLevel.Error);
            ShowStatus($"{success} converted, {failed} failed.", isError: true);
        }
    }

    [RelayCommand]
    private void ClearQueue()
    {
        foreach (var item in QueuedFiles)
            item.PropertyChanged -= OnItemPropertyChanged;

        QueuedFiles.Clear();
        ClearStatus();
        RefreshDerivedProperties();
    }

    [RelayCommand]
    private void ClearLog() => LogEntries.Clear();

    [RelayCommand]
    private void RemoveFile(ConversionItemViewModel item)
    {
        item.PropertyChanged -= OnItemPropertyChanged;
        QueuedFiles.Remove(item);
        if (QueuedFiles.Count == 0)
            ClearStatus();
        RefreshDerivedProperties();
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp;*.tiff;*.tga;*.qoi;*.pbm|All Files|*.*",
            Title = "Select images to convert"
        };

        if (dialog.ShowDialog() == true)
            AddFiles(dialog.FileNames);
    }

    public void HandleDrop(string[] paths)
    {
        IsDragOver = false;
        List<string> files = [];

        foreach (string path in paths)
        {
            if (File.Exists(path) && Constants.SupportedExtensions.Contains(Path.GetExtension(path)))
                files.Add(path);
            else if (Directory.Exists(path))
                files.AddRange(ConversionHelper.GetConvertibleFiles(path));
        }

        AddFiles([.. files]);
    }

    private void AddFiles(string[] paths)
    {
        var existing = QueuedFiles.Select(f => f.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (existing.Add(path))
                QueuedFiles.Add(new ConversionItemViewModel(path));
        }

        RefreshDerivedProperties();
    }

    private void RefreshDerivedProperties()
    {
        OnPropertyChanged(nameof(QualitySupported));
        OnPropertyChanged(nameof(QualityHint));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(AllSelected));
        OnPropertyChanged(nameof(ConvertButtonText));
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
        HasStatusMessage = true;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        HasStatusMessage = false;
        StatusIsError = false;
    }
}