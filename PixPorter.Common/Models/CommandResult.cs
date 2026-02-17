namespace PixPorter.Common.Models;

public abstract record CommandResult;
public record CommandResultSuccess(string Message) : CommandResult;
public record CommandResultMultiConvert(IReadOnlyList<string> Files, string? TargetExtension, int? Quality) : CommandResult;
public record CommandResultDirectoryChanged(string NewPath) : CommandResult;
public record CommandResultError(string Message) : CommandResult;
public record CommandResultQuit : CommandResult;
public record CommandResultHelp : CommandResult;