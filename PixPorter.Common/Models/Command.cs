namespace PixPorter.Common.Models;

public record Command(
    string Name,
    string Path,
    string? TargetExtension,
    int? Quality = null,
    bool StripMetadata = false,
    List<string>? AdditionalPaths = null);