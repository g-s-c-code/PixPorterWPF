using PixPorter.Common.Core;
using SixLabors.ImageSharp;

namespace PixPorter.Common.Helpers;

public static class ConversionHelper
{
    public static void ConvertFile(string inputPath, string? targetExtension)
    {
        string sourceExtension = Path.GetExtension(inputPath);
        string outputExtension = targetExtension ?? Constants.GetDefaultTarget(sourceExtension);
        string outputPath = Path.ChangeExtension(inputPath, outputExtension);

        using Image image = Image.Load(inputPath);
        image.Save(outputPath);
    }

    public static IEnumerable<string> GetConvertibleFiles(string directory)
    {
        return Directory.GetFiles(directory)
            .Where(f => Constants.SupportedExtensions.Contains(Path.GetExtension(f)));
    }
}
