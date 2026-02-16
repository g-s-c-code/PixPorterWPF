namespace PixPorter.Common.Core;

public static class Constants
{
    public const string ChangeDirectory = "cd ";
    public const string Exit = "exit";
    public const string Q = "q";
    public const string Quit = "quit";
    public const string Help = "help";
    public const string ConvertFile = "-cf";
    public const string ConvertAll = "--ca";
    public const string PngFlag = "--png";
    public const string JpgFlag = "--jpg";
    public const string JpegFlag = "--jpeg";
    public const string WebpFlag = "--webp";
    public const string GifFlag = "--gif";
    public const string TiffFlag = "--tiff";
    public const string BmpFlag = "--bmp";
    public const string PngFileFormat = ".png";
    public const string JpgFileFormat = ".jpg";
    public const string JpegFileFormat = ".jpeg";
    public const string WebpFileFormat = ".webp";
    public const string GifFileFormat = ".gif";
    public const string TiffFileFormat = ".tiff";
    public const string BmpFileFormat = ".bmp";

    public static readonly HashSet<string> SupportedFileFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        PngFileFormat, JpgFileFormat, JpegFileFormat, WebpFileFormat, GifFileFormat, TiffFileFormat, BmpFileFormat
    };

    public static readonly HashSet<string> SupportedFormatFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        PngFlag, JpgFlag, JpegFlag, WebpFlag, GifFlag, TiffFlag, BmpFlag
    };

    public static Dictionary<string, string> DefaultConversions { get; } = new()
    {
        { PngFileFormat, WebpFileFormat },
        { JpgFileFormat, WebpFileFormat },
        { JpegFileFormat, WebpFileFormat },
        { WebpFileFormat, PngFileFormat },
        { GifFileFormat, PngFileFormat },
        { TiffFileFormat, PngFileFormat },
        { BmpFileFormat, PngFileFormat }
    };
}