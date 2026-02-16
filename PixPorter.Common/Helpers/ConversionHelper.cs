using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Tiff;
using PixPorter.Common.Core;

namespace PixPorter.Common.Helpers;

public static class ConversionHelper
{
    private static readonly Dictionary<string, IImageEncoder> Encoders = new()
    {
        { Constants.WebpFileFormat, new WebpEncoder() },
        { Constants.PngFileFormat, new PngEncoder() },
        { Constants.JpegFileFormat, new JpegEncoder() },
        { Constants.JpgFileFormat, new JpegEncoder() },
        { Constants.GifFileFormat, new GifEncoder() },
        { Constants.TiffFileFormat, new TiffEncoder() },
        { Constants.BmpFileFormat, new BmpEncoder() }
    };

    public static void ConvertFile(string filePath, string? targetFormat = null)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var outputFormat = DetermineOutputFormat(extension, targetFormat);
        var outputPath = Path.ChangeExtension(filePath, outputFormat);

        using var image = Image.Load(filePath);
        image.Save(outputPath, GetEncoder(outputFormat));
    }

    public static List<string> GetSupportedFiles(string directoryPath, string? targetFormat) =>
        [.. Directory.GetFiles(directoryPath).Where(file => IsSupported(file, targetFormat))];

    private static string DetermineOutputFormat(string inputExtension, string? targetFormat)
    {
        if (targetFormat != null)
            return targetFormat;

        return Constants.DefaultConversions.TryGetValue(inputExtension, out var defaultFormat)
            ? defaultFormat
            : throw new Exception($"Unsupported file type: {inputExtension}");
    }

    private static IImageEncoder GetEncoder(string format) =>
        Encoders.TryGetValue(format, out var encoder)
            ? encoder
            : throw new Exception($"Unsupported conversion target: {format}");

    private static bool IsSupported(string filePath, string? targetFormat)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return Constants.SupportedFileFormats.Contains(extension) &&
               (targetFormat == null || Constants.DefaultConversions.ContainsKey(extension));
    }

    public static string GetDefaultFormat() => Constants.WebpFileFormat;
}