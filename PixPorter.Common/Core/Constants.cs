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

    public static readonly Dictionary<string, string> FormatFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        { "--png", ".png" },
        { "--jpg", ".jpg" },
        { "--jpeg", ".jpg" },
        { "--webp", ".webp" },
        { "--gif", ".gif" },
        { "--bmp", ".bmp" },
        { "--tiff", ".tiff" }
    };

    public static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".tiff"
    };

    public static string GetDefaultTarget(string sourceExtension)
    {
        return sourceExtension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            ? ".png"
            : ".webp";
    }
}
