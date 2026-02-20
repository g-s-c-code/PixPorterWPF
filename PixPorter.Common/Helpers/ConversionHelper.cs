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
    public static void ConvertFile(string inputPath, string? targetExtension, int? quality = null, bool stripMetadata = false)
    {
        string sourceExtension = Path.GetExtension(inputPath);
        string outputExtension = targetExtension ?? Constants.GetDefaultTarget(sourceExtension);
        string outputPath = Path.ChangeExtension(inputPath, outputExtension);

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