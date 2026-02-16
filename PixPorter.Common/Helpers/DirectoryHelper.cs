using PixPorter.Common.Core;

namespace PixPorter.Common.Helpers;

public static class DirectoryHelper
{
    public static IEnumerable<string> GetDirectories()
    {
        return Directory.EnumerateDirectories(Directory.GetCurrentDirectory())
            .Select(Path.GetFileName)
            .Where(n => n != null)!;
    }

    public static IEnumerable<string> GetImageFiles()
    {
        return Directory.EnumerateFiles(Directory.GetCurrentDirectory())
            .Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)))
            .Select(Path.GetFileName)
            .Where(n => n != null)!;
    }
}
