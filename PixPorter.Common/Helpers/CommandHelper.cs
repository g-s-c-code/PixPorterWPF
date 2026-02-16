using PixPorter.Common.Core;
using PixPorter.Common.Models;

namespace PixPorter.Common.Helpers;

public static class CommandHelper
{
    public static Command Parse(string input)
    {
        input = input.Replace("\"", "").Trim();

        if (IsQuit(input))
        {
            return new(Constants.Quit, string.Empty, null);
        }

        if (IsHelp(input))
        {
            return new(Constants.Help, string.Empty, null);
        }

        if (input.StartsWith(Constants.ChangeDirectory))
        {
            string newPath = input[Constants.ChangeDirectory.Length..].Trim();
            return new(Constants.ChangeDirectory, newPath, null);
        }

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string? targetExtension = ExtractTargetExtension(parts);
        List<string> allPaths = ExtractAllPaths(parts);  // Changed to get ALL paths

        if (parts.Contains(Constants.ConvertAll))
        {
            string path = allPaths.FirstOrDefault() ?? Directory.GetCurrentDirectory();
            return new(Constants.ConvertAll, path, targetExtension);
        }

        if (allPaths.Any())
        {
            // If multiple files, use first as primary and rest as additional
            string primaryPath = allPaths[0];
            List<string>? additionalPaths = allPaths.Count > 1 ? allPaths.Skip(1).ToList() : null;
            return new(Constants.ConvertFile, primaryPath, targetExtension, additionalPaths);
        }

        throw new CommandException("Invalid command.");
    }

    private static bool IsQuit(string input)
    {
        return input is Constants.Q or Constants.Quit or Constants.Exit;
    }

    private static bool IsHelp(string input)
    {
        return input == Constants.Help;
    }

    private static string? ExtractTargetExtension(string[] parts)
    {
        return parts.Select(p => Constants.FormatFlags.TryGetValue(p, out string? ext) ? ext : null)
             .FirstOrDefault(e => e != null);
    }

    private static List<string> ExtractAllPaths(string[] parts)  // New method
    {
        List<string> paths = new();

        foreach (string part in parts)
        {
            // Skip format flags
            if (Constants.FormatFlags.ContainsKey(part) || part == Constants.ConvertAll)
                continue;

            string full = Path.GetFullPath(part);

            if (File.Exists(part))
            {
                paths.Add(part);
            }
            else if (File.Exists(full))
            {
                paths.Add(full);
            }
            else if (Directory.Exists(part) || Directory.Exists(full))
            {
                paths.Add(Directory.Exists(part) ? part : full);
            }
        }

        return paths;
    }
}