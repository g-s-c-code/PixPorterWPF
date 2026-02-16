namespace PixPorter.Common.Models;

public record Command(
    string Name,
    string Path,
    string? TargetExtension,
    List<string>? AdditionalPaths = null);