using PixPorter.Common.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Pbm;

namespace PixPorter.Common.Helpers;

public static class ConversionHelper
{
    public static string ConvertFile(
        string inputPath,
        string? targetExtension,
        int? quality = null,
        bool stripMetadata = false,
        string? outputDirectory = null,
        IReadOnlySet<string>? sourcesToPreserve = null)
    {
        string sourceExtension = Path.GetExtension(inputPath);
        string outputExtension = targetExtension ?? Constants.GetDefaultTarget(sourceExtension);
        string outputPath = ResolveOutputPath(inputPath, outputExtension, outputDirectory, sourcesToPreserve);

        using Image image = Image.Load(inputPath);

        if (stripMetadata)
        {
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IccProfile = null;
        }

        if (quality.HasValue)
            image.Save(outputPath, BuildEncoder(outputExtension, quality.Value));
        else
            image.Save(outputPath);

        return outputPath;
    }

    private static string ResolveOutputPath(
        string inputPath,
        string outputExtension,
        string? outputDirectory,
        IReadOnlySet<string>? sourcesToPreserve)
    {
        string directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetDirectoryName(inputPath) ?? string.Empty
            : outputDirectory;

        Directory.CreateDirectory(directory);

        string baseName = Path.GetFileNameWithoutExtension(inputPath);
        string candidate = Path.Combine(directory, baseName + outputExtension);

        return WouldDestroyASource(candidate, inputPath, sourcesToPreserve)
            ? Path.Combine(directory, $"{baseName}-converted{outputExtension}")
            : candidate;
    }

    private static bool WouldDestroyASource(
        string candidate,
        string inputPath,
        IReadOnlySet<string>? sourcesToPreserve)
    {
        string resolved = Path.GetFullPath(candidate);

        return string.Equals(resolved, Path.GetFullPath(inputPath), StringComparison.OrdinalIgnoreCase)
            || (sourcesToPreserve?.Contains(resolved) ?? false);
    }

    public static IEnumerable<string> GetConvertibleFiles(string directory) =>
        Directory.GetFiles(directory).Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)));

    private static IImageEncoder BuildEncoder(string extension, int quality) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => new JpegEncoder { Quality = quality },
        ".webp" => new WebpEncoder { Quality = quality },
        ".png" => new PngEncoder { CompressionLevel = MapQualityToPngCompression(quality) },
        _ => throw new NotSupportedException($"Quality is not supported for {extension} files.")
    };

    private static PngCompressionLevel MapQualityToPngCompression(int quality)
    {
        int level = (int)Math.Round(9.0 * (1.0 - ((quality - 1) / 99.0)));
        return level switch
        {
            0 => PngCompressionLevel.NoCompression,
            1 => PngCompressionLevel.BestSpeed,
            9 => PngCompressionLevel.BestCompression,
            _ => (PngCompressionLevel)level
        };
    }
}