using PixPorter.Common.Core;
using PixPorter.Common.Models;

namespace PixPorter.Common.Helpers;

public static class CommandHelper
{
    public static CommandResult ProcessCommand(string input)
    {
        try
        {
            Command command = ParseCommand(input);
            return ExecuteCommand(command);
        }
        catch (Exception ex)
        {
            return new CommandResultError(ex.Message);
        }
    }

    private static CommandResult ExecuteCommand(Command command)
    {
        switch (command.Name)
        {
            case Constants.Quit:
                return new CommandResultQuit();

            case Constants.Help:
                return new CommandResultHelp();

            case Constants.ChangeDirectory:
                if (!Directory.Exists(command.Path))
                    return new CommandResultError($"Directory not found: {command.Path}");

                Directory.SetCurrentDirectory(command.Path);
                return new CommandResultDirectoryChanged(command.Path);

            case Constants.ConvertFile:
                return ExecuteCommandConvertFile(command);

            case Constants.ConvertAll:
                return ExecuteCommandConvertAll(command);

            default:
                return new CommandResultError("Unknown command.");
        }
    }

    private static Command ParseCommand(string input)
    {
        input = input.Replace("\"", "").Trim();
        string inputLower = input.ToLowerInvariant();

        if (IsCommandQuit(inputLower))
            return new(Constants.Quit, string.Empty, null);

        if (IsCommandHelp(inputLower))
            return new(Constants.Help, string.Empty, null);

        if (inputLower.StartsWith(Constants.ChangeDirectory))
        {
            string newPath = input[Constants.ChangeDirectory.Length..].Trim();
            return new(Constants.ChangeDirectory, newPath, null);
        }

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string? targetExtension = ExtractTargetExtension(parts);
        int? quality = ExtractQuality(parts);
        bool stripMetadata = ExtractStripMetadata(parts);

        if (parts.Any(p => p.Equals(Constants.ConvertAll, StringComparison.OrdinalIgnoreCase)))
        {
            List<string> allPaths = ExtractAllPaths(parts);
            string path = allPaths.FirstOrDefault() ?? Directory.GetCurrentDirectory();
            return new(Constants.ConvertAll, path, targetExtension, quality, stripMetadata, null);
        }

        List<string> nonFlagParts = [.. parts
            .Where(p => !Constants.FormatFlags.ContainsKey(p))
            .Where(p => !p.Equals(Constants.ConvertAll, StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("--quality=", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Equals("--stripmeta", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.StartsWith("--", StringComparison.OrdinalIgnoreCase))];

        for (int len = nonFlagParts.Count; len >= 1; len--)
        {
            string candidate = string.Join(" ", nonFlagParts.Take(len));
            string fullCandidate = Path.GetFullPath(candidate);

            if (File.Exists(candidate) || File.Exists(fullCandidate))
            {
                string primaryPath = File.Exists(candidate) ? candidate : fullCandidate;
                List<string>? additionalPaths = null;

                if (len < nonFlagParts.Count)
                {
                    additionalPaths = ExtractAllPaths([.. nonFlagParts.Skip(len)]);
                    if (additionalPaths.Count == 0) additionalPaths = null;
                }

                return new(Constants.ConvertFile, primaryPath, targetExtension, quality, stripMetadata, additionalPaths);
            }

            if (Directory.Exists(candidate) || Directory.Exists(fullCandidate))
            {
                string dirPath = Directory.Exists(candidate) ? candidate : fullCandidate;
                return new(Constants.ConvertAll, dirPath, targetExtension, quality, stripMetadata, null);
            }
        }

        throw new CommandException("Invalid command.");
    }

    private static CommandResult ExecuteCommandConvertFile(Command command)
    {
        List<string> filesToConvert = [command.Path];
        if (command.AdditionalPaths != null)
            filesToConvert.AddRange(command.AdditionalPaths);

        if (filesToConvert.Count == 1)
        {
            if (!File.Exists(command.Path))
                return new CommandResultError($"File not found: {command.Path}");

            string sourceExtension = Path.GetExtension(command.Path);
            string effectiveTarget = command.TargetExtension ?? Constants.GetDefaultTarget(sourceExtension);
            string outputPath = Path.ChangeExtension(command.Path, effectiveTarget);

            ConversionHelper.ConvertFile(command.Path, effectiveTarget, command.Quality, command.StripMetadata);

            string qualityNote = command.Quality.HasValue ? $" [quality: {command.Quality}]" : string.Empty;
            string metadataNote = command.StripMetadata ? " [metadata stripped]" : string.Empty;

            return new CommandResultSuccess($"Converted: {command.Path} → {outputPath}{qualityNote}{metadataNote}");
        }

        return new CommandResultMultiConvert(filesToConvert, command.TargetExtension, command.Quality, command.StripMetadata);
    }

    private static CommandResult ExecuteCommandConvertAll(Command command)
    {
        if (!Directory.Exists(command.Path))
            return new CommandResultError($"Directory not found: {command.Path}");

        List<string> files = ConversionHelper.GetConvertibleFiles(command.Path).ToList();
        return files.Count == 0
            ? new CommandResultError("No supported images found.")
            : new CommandResultMultiConvert(files, command.TargetExtension, command.Quality, command.StripMetadata);
    }

    private static bool IsCommandQuit(string input) =>
        input is Constants.Q or Constants.Quit or Constants.Exit;

    private static bool IsCommandHelp(string input) =>
        input == Constants.Help;

    private static string? ExtractTargetExtension(string[] parts) =>
        parts.Select(p => Constants.FormatFlags.TryGetValue(p, out string? ext) ? ext : null)
             .FirstOrDefault(e => e != null);

    private static int? ExtractQuality(string[] parts)
    {
        foreach (string part in parts)
        {
            if (part.StartsWith("--quality=", StringComparison.OrdinalIgnoreCase))
            {
                string raw = part["--quality=".Length..];
                if (int.TryParse(raw, out int q) && q >= 1 && q <= 100)
                    return q;
            }
        }

        return null;
    }

    private static bool ExtractStripMetadata(string[] parts) =>
        parts.Any(p => p.Equals("--stripmeta", StringComparison.OrdinalIgnoreCase));

    private static List<string> ExtractAllPaths(string[] parts)
    {
        List<string> paths = [];
        foreach (string part in parts)
        {
            if (Constants.FormatFlags.ContainsKey(part)) continue;
            if (part.Equals(Constants.ConvertAll, StringComparison.OrdinalIgnoreCase)) continue;
            if (part.StartsWith("--quality=", StringComparison.OrdinalIgnoreCase)) continue;
            if (part.Equals("--stripmeta", StringComparison.OrdinalIgnoreCase)) continue;
            if (part.StartsWith("--", StringComparison.OrdinalIgnoreCase)) continue;

            string full = Path.GetFullPath(part);
            if (File.Exists(part)) paths.Add(part);
            else if (File.Exists(full)) paths.Add(full);
            else if (Directory.Exists(part) || Directory.Exists(full))
                paths.Add(Directory.Exists(part) ? part : full);
        }

        return paths;
    }
}