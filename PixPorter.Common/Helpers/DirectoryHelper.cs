using static PixPorter.Common.Core.Constants;

namespace PixPorter.Common.Helpers;

public static class DirectoryHelper
{
    public static IEnumerable<string> GetEntries(Func<string, bool> filter, string errorType)
    {
        var dir = Directory.GetCurrentDirectory();
        try
        {
            var entries = errorType == ChangeDirectory
                ? Directory.EnumerateDirectories(dir)
                : Directory.EnumerateFiles(dir);

            return entries
                .Where(entry => (File.GetAttributes(entry) & FileAttributes.Hidden) == 0 && filter(entry))
                .Select(Path.GetFileName)
                .Where(name => name != null)
                .ToList()!;
        }
        catch (UnauthorizedAccessException)
        {
            return [$"Error: Access to the path '{dir}' is denied."];
        }
        catch (Exception ex)
        {
            return [$"Error: {ex.Message}"];
        }
    }

    public static IEnumerable<string> GetDirectories() =>
        GetEntries(_ => true, ChangeDirectory);

    public static IEnumerable<string> GetImageFiles() =>
        GetEntries(file => SupportedFileFormats.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase), "File");
}