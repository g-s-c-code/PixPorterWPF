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
        string? path = ExtractPath(parts);

        return parts.Contains(Constants.ConvertAll)
            ? new(Constants.ConvertAll, path ?? Directory.GetCurrentDirectory(), targetExtension)
            : path != null ? new(Constants.ConvertFile, path, targetExtension) : throw new CommandException("Invalid command.");
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

    private static string? ExtractPath(string[] parts)
    {
        foreach (string part in parts)
        {
            string full = Path.GetFullPath(part);

            if (File.Exists(part) || Directory.Exists(part))
            {
                return part;
            }

            if (File.Exists(full) || Directory.Exists(full))
            {
                return full;
            }
        }

        return null;
    }
}
