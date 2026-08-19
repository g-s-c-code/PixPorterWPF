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

public readonly record struct ConversionSettings(
    string? Format,
    int? Quality,
    bool StripMetadata,
    string? OutputDirectory,
    IReadOnlySet<string> SourcesToPreserve);

public readonly record struct ConversionOutcome(
    string OutputPath,
    string TargetExtension,
    int? Quality,
    string OutputSize);

public partial class MainViewModel : ObservableObject
{
    public const string DefaultFormat = "Default";

    public IReadOnlyList<string> FormatOptions { get; } =
        [DefaultFormat, "WebP", "PNG", "JPG", "GIF", "BMP", "TIFF", "TGA", "QOI", "PBM"];

    [ObservableProperty]
    private ObservableCollection<ConversionItemViewModel> _queuedFiles = [];

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = [];

    private ObservableCollection<ConversionItemViewModel>? _observedQueue;
    private CancellationTokenSource? _conversionCancellation;

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
    [ObservableProperty] private bool _isDark = DetectSystemDarkMode();
    [ObservableProperty] private bool _stripMetadata = false;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasStatusMessage = false;
    [ObservableProperty] private bool _statusIsError = false;
    [ObservableProperty] private string? _customOutputFolder = null;

    public bool HasCustomOutputFolder => !string.IsNullOrEmpty(CustomOutputFolder);

    partial void OnCustomOutputFolderChanged(string? value)
    {
        OnPropertyChanged(nameof(HasCustomOutputFolder));
        OnPropertyChanged(nameof(OutputFolderDisplay));
    }

    public string OutputFolderDisplay =>
        string.IsNullOrEmpty(CustomOutputFolder) ? "Same as source" : CustomOutputFolder;

    public bool QualitySupported =>
        SelectedFormat is null
            ? QueuedFiles.Count == 0 || QueuedFiles.All(f => IsQualitySupported(Path.GetExtension(f.FilePath)))
            : IsQualitySupported($".{SelectedFormat.ToLowerInvariant()}");

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

    private static string Pluralise(int count) => count == 1 ? string.Empty : "s";

    partial void OnIsDarkChanged(bool value) => ThemeService.Instance.Apply(value);

    partial void OnQueuedFilesChanged(ObservableCollection<ConversionItemViewModel> value)
        => ObserveQueue(value);

    public MainViewModel()
    {
        ObserveQueue(QueuedFiles);
        ThemeService.Instance.Apply(_isDark);
    }

    private void ObserveQueue(ObservableCollection<ConversionItemViewModel> collection)
    {
        if (ReferenceEquals(_observedQueue, collection))
            return;

        if (_observedQueue is not null)
        {
            _observedQueue.CollectionChanged -= OnQueueCollectionChanged;
            foreach (var item in _observedQueue)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

        _observedQueue = collection;
        collection.CollectionChanged += OnQueueCollectionChanged;

        foreach (var item in collection)
            item.PropertyChanged += OnItemPropertyChanged;

        RefreshDerivedProperties();
    }

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

    private static bool DetectSystemDarkMode()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);
            return value is int i && i == 0;
        }
        catch
        {
            return false;
        }
    }

    private Task OnUiThreadAsync(Action action) =>
        Application.Current.Dispatcher.InvokeAsync(action).Task;

    private ConversionSettings CaptureSettings(IEnumerable<ConversionItemViewModel> queue) => new(
        SelectedFormat,
        QualitySupported ? Quality : null,
        StripMetadata,
        CustomOutputFolder,
        queue.Select(f => Path.GetFullPath(f.FilePath)).ToHashSet(StringComparer.OrdinalIgnoreCase));

    private static ConversionOutcome ConvertSingle(ConversionItemViewModel item, ConversionSettings settings)
    {
        string targetExtension = settings.Format is not null
            ? $".{settings.Format.ToLowerInvariant()}"
            : Constants.GetDefaultTarget(Path.GetExtension(item.FilePath));

        string outputPath = ConversionHelper.ConvertFile(
            item.FilePath,
            targetExtension,
            settings.Quality,
            settings.StripMetadata,
            settings.OutputDirectory,
            settings.SourcesToPreserve);

        return new ConversionOutcome(
            outputPath,
            targetExtension,
            settings.Quality,
            FileSizeFormatter.DescribeFile(outputPath));
    }

    private static string DescribeSuccess(
        ConversionItemViewModel item,
        ConversionOutcome outcome,
        bool strippedMetadata)
    {
        string sizeNote = !string.IsNullOrEmpty(item.FileSize) && !string.IsNullOrEmpty(outcome.OutputSize)
            ? $"  {item.FileSize} -> {outcome.OutputSize}"
            : string.Empty;
        string qualityNote = outcome.Quality.HasValue ? $"  quality={outcome.Quality}" : string.Empty;
        string metadataNote = strippedMetadata ? "  stripmeta=true" : string.Empty;

        return $"[OK]  {item.FileName}  ->  {Path.GetFileName(outcome.OutputPath)}{sizeNote}{qualityNote}{metadataNote}\n" +
               $"      {outcome.OutputPath}";
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (QueuedFiles.Count == 0 || IsConverting)
            return;

        List<ConversionItemViewModel> queue = (HasSelection
            ? QueuedFiles.Where(f => f.IsSelected)
            : QueuedFiles).ToList();

        ConversionSettings settings = CaptureSettings(queue);

        using CancellationTokenSource cancellation = new();
        _conversionCancellation = cancellation;

        IsConverting = true;
        ClearStatus();
        AddLog($"Starting conversion of {queue.Count} file{Pluralise(queue.Count)}.");

        int succeeded = 0;
        int failed = 0;

        try
        {
            await Parallel.ForEachAsync(
                queue,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellation.Token
                },
                async (item, token) =>
                {
                    await OnUiThreadAsync(() =>
                    {
                        item.Status = ConversionStatus.Converting;
                        item.StatusText = "...";
                    });

                    try
                    {
                        token.ThrowIfCancellationRequested();
                        ConversionOutcome outcome = ConvertSingle(item, settings);
                        Interlocked.Increment(ref succeeded);

                        await OnUiThreadAsync(() =>
                        {
                            item.Status = ConversionStatus.Done;
                            item.StatusText = outcome.TargetExtension.TrimStart('.').ToUpperInvariant();
                            item.OutputFileSize = outcome.OutputSize;
                            AddLog(DescribeSuccess(item, outcome, settings.StripMetadata), LogLevel.Success);
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        await OnUiThreadAsync(() =>
                        {
                            item.Status = ConversionStatus.Pending;
                            item.StatusText = "Cancelled";
                        });
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Interlocked.Increment(ref failed);

                        await OnUiThreadAsync(() =>
                        {
                            item.Status = ConversionStatus.Failed;
                            item.StatusText = "Failed";
                            item.ErrorMessage = exception.Message;
                            AddLog($"[ERR] {item.FileName}\n      {exception.Message}", LogLevel.Error);
                        });
                    }
                });
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _conversionCancellation = null;
            IsConverting = false;
        }

        ReportCompletion(succeeded, failed, cancellation.IsCancellationRequested);
    }

    private void ReportCompletion(int succeeded, int failed, bool cancelled)
    {
        if (cancelled)
        {
            AddLog($"Cancelled. {succeeded} file{Pluralise(succeeded)} converted before stopping.", LogLevel.Error);
            ShowStatus($"Cancelled after {succeeded} file{Pluralise(succeeded)}.", isError: true);
            return;
        }

        if (failed == 0)
        {
            AddLog($"Done. {succeeded} file{Pluralise(succeeded)} converted successfully.", LogLevel.Success);
            ShowStatus($"{succeeded} file{Pluralise(succeeded)} converted successfully.", isError: false);
            return;
        }

        AddLog($"Done. {succeeded} succeeded, {failed} failed.", LogLevel.Error);
        ShowStatus($"{succeeded} converted, {failed} failed.", isError: true);
    }

    [RelayCommand]
    private void CancelConversion() => _conversionCancellation?.Cancel();

    [RelayCommand]
    private void SelectOutputFolder()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Choose an output folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
            CustomOutputFolder = dialog.FolderName;
    }

    [RelayCommand]
    private void UseSourceFolder() => CustomOutputFolder = null;

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
