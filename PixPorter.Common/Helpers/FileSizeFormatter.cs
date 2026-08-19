namespace PixPorter.Common.Helpers;

public static class FileSizeFormatter
{
    private const long BytesPerMegabyte = 1_048_576;
    private const long BytesPerKilobyte = 1024;

    public static string Describe(long bytes) => bytes switch
    {
        >= BytesPerMegabyte => $"{bytes / (double)BytesPerMegabyte:F1} MB",
        >= BytesPerKilobyte => $"{bytes / (double)BytesPerKilobyte:F0} KB",
        _ => $"{bytes} B"
    };

    public static string DescribeFile(string path)
    {
        try
        {
            return Describe(new FileInfo(path).Length);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
