namespace PixPorter.Common.Models;

public record Command(
    string Name,
    string Path,
    string? TargetExtension,
    int? Quality = null,
    List<string>? AdditionalPaths = null);