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

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ConversionItemViewModel> _queuedFiles = [];

    public const string DefaultFormat = "Default";

    public IReadOnlyList<string> FormatOptions { get; } = [DefaultFormat, "WebP", "PNG", "JPG", "GIF", "BMP", "TIFF"];

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

    [ObservableProperty]
    private int _quality = 100;

    [ObservableProperty]
    private bool _isConverting = false;

    [ObservableProperty]
    private bool _isDragOver = false;

    [ObservableProperty]
    private bool _isDark = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasStatusMessage = false;

    [ObservableProperty]
    private bool _statusIsError = false;

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
            foreach (ConversionItemViewModel item in QueuedFiles)
                item.IsSelected = value;
            OnPropertyChanged(nameof(AllSelected));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ConvertButtonText));
        }
    }

    private static bool IsQualitySupported(string? ext) =>
        ext is ".webp" or ".png" or ".jpg" or ".jpeg";

    partial void OnIsDarkChanged(bool value)
    {
        ThemeService.Instance.Apply(value);
    }

    partial void OnQueuedFilesChanged(ObservableCollection<ConversionItemViewModel> value)
    {
        SubscribeToQueueChanges(value);
    }

    private void SubscribeToQueueChanges(ObservableCollection<ConversionItemViewModel> collection)
    {
        collection.CollectionChanged += OnQueueCollectionChanged;
    }

    private void OnQueueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ConversionItemViewModel item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        if (e.OldItems != null)
        {
            foreach (ConversionItemViewModel item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

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

    public MainViewModel()
    {
        SubscribeToQueueChanges(QueuedFiles);
    }

    [RelayCommand]
    private async Task ConvertAsync()
    {
        if (QueuedFiles.Count == 0)
            return;

        IEnumerable<ConversionItemViewModel> targets = HasSelection
            ? QueuedFiles.Where(f => f.IsSelected)
            : QueuedFiles;

        List<ConversionItemViewModel> toConvert = targets.ToList();

        IsConverting = true;
        ClearStatus();

        int success = 0;
        int failed = 0;

        foreach (ConversionItemViewModel item in toConvert)
        {
            item.Status = ConversionStatus.Converting;
            item.StatusText = "Converting…";

            await Task.Run(() =>
            {
                try
                {
                    string sourceExt = Path.GetExtension(item.FilePath);
                    string targetExt = SelectedFormat ?? Constants.GetDefaultTarget(sourceExt);
                    int? quality = QualitySupported ? Quality : null;

                    ConversionHelper.ConvertFile(item.FilePath, targetExt, quality);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        item.Status = ConversionStatus.Done;
                        item.StatusText = $"→ {targetExt.TrimStart('.').ToUpperInvariant()}";
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
                    });

                    failed++;
                }
            });
        }

        IsConverting = false;

        if (failed == 0)
            ShowStatus($"✓  {success} file{(success == 1 ? "" : "s")} converted successfully.", isError: false);
        else
            ShowStatus($"{success} converted, {failed} failed.", isError: true);
    }

    [RelayCommand]
    private void ClearQueue()
    {
        foreach (ConversionItemViewModel item in QueuedFiles)
            item.PropertyChanged -= OnItemPropertyChanged;

        QueuedFiles.Clear();
        ClearStatus();
        RefreshDerivedProperties();
    }

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
        OpenFileDialog dialog = new()
        {
            Multiselect = true,
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp;*.tiff|All Files|*.*",
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
        HashSet<string> existing = QueuedFiles.Select(f => f.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            if (!existing.Contains(path))
            {
                QueuedFiles.Add(new ConversionItemViewModel(path));
                existing.Add(path);
            }
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