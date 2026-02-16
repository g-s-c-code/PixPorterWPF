namespace PixPorter.Common.Models;

public record Command(
    string Name,
    List<string> Arguments,
    string? TargetFormat,
    int? Quality);