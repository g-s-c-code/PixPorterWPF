using PixPorter.Common.Core;
using PixPorter.Common.Models;

namespace PixPorter.Common.Helpers;

public static class CommandHelper
{
    public static Command ParseInput(string input)
    {
        input = input.Replace("\"", string.Empty).Trim();
        if (IsSpecialCommand(input, out Command? specialCommand))
        {
            return specialCommand!;
        }

        if (input.StartsWith(Constants.ChangeDirectory))
        {
            string path = input[Constants.ChangeDirectory.Length..].Trim();
            return new Command(Constants.ChangeDirectory, [path], null, null);
        }

        return ParseConversionCommand(input);
    }

    private static bool IsSpecialCommand(string input, out Command? command)
    {
        command = input switch
        {
            Constants.Q or Constants.Quit or Constants.Exit => new Command(Constants.Quit, [], null, null),
            Constants.Help => new Command(Constants.Help, [], null, null),
            _ => null
        };

        return command != null;
    }

    private static Command ParseConversionCommand(string input)
    {
        string? filePath = ExtractFilePath(input, [.. Constants.SupportedFileFormats]);
        string remainingInput;

        if (filePath != null)
        {
            remainingInput = input[(input.IndexOf(filePath) + filePath.Length)..].Trim();
            var parts = remainingInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? formatFlag = ExtractFormatFlag(parts);

            bool convertAll = parts.Contains(Constants.ConvertAll);
            string commandType = convertAll ? Constants.ConvertAll : Constants.ConvertFile;
            return new Command(commandType, [filePath], MapFormatFlag(formatFlag), null);
        }

        var allParts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (allParts.Contains(Constants.ConvertAll))
        {
            string? formatFlag = ExtractFormatFlag(allParts);
            return new Command(Constants.ConvertAll, [Directory.GetCurrentDirectory()], MapFormatFlag(formatFlag), null);
        }

        string? formatFlag2 = ExtractFormatFlag(allParts);
        return ParseDirectoryConversion(allParts, formatFlag2);
    }

    private static Command ParseDirectoryConversion(string[] parts, string? formatFlag)
    {
        var potentialPaths = parts.Where(p => !p.StartsWith("-")).ToList();

        if (!potentialPaths.Any() && formatFlag != null)
        {
            return new Command(Constants.ConvertAll,
                [Directory.GetCurrentDirectory()],
                MapFormatFlag(formatFlag),
                null);
        }

        foreach (var path in potentialPaths)
        {
            string relativePath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (Directory.Exists(path))
            {
                return new Command(Constants.ConvertAll, [path], MapFormatFlag(formatFlag), null);
            }
            if (Directory.Exists(relativePath))
            {
                return new Command(Constants.ConvertAll, [relativePath], MapFormatFlag(formatFlag), null);
            }
            if (File.Exists(path))
            {
                return new Command(Constants.ConvertFile, [path], MapFormatFlag(formatFlag), null);
            }
            if (File.Exists(relativePath))
            {
                return new Command(Constants.ConvertFile, [relativePath], MapFormatFlag(formatFlag), null);
            }
        }

        throw new CommandException("Invalid command or path.");
    }

    private static string? ExtractFormatFlag(string[] parts) =>
        parts.FirstOrDefault(IsFormatFlag);

    private static bool IsFormatFlag(string flag) =>
        flag is Constants.PngFlag
            or Constants.JpgFlag
            or Constants.JpegFlag
            or Constants.WebpFlag
            or Constants.GifFlag
            or Constants.TiffFlag
            or Constants.BmpFlag;

    private static string? ExtractFilePath(string input, string[] validExtensions) => validExtensions
        .Select(ext => new
        {
            Extension = ext,
            Index = input.IndexOf(ext, StringComparison.OrdinalIgnoreCase)
        })
        .Where(x => x.Index > 0)
        .Select(x => input[..(x.Index + x.Extension.Length)].Trim())
        .FirstOrDefault();

    private static string? MapFormatFlag(string? flag) => flag switch
    {
        Constants.PngFlag => Constants.PngFileFormat,
        Constants.JpgFlag => Constants.JpgFileFormat,
        Constants.JpegFlag => Constants.JpegFileFormat,
        Constants.WebpFlag => Constants.WebpFileFormat,
        Constants.GifFlag => Constants.GifFileFormat,
        Constants.TiffFlag => Constants.TiffFileFormat,
        Constants.BmpFlag => Constants.BmpFileFormat,
        _ => null
    };
}