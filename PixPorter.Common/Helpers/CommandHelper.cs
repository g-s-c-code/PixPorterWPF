using PixPorter.Common.Models;
using static PixPorter.Common.Core.Constants;

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

        if (input.StartsWith(ChangeDirectory))
        {
            string path = input[ChangeDirectory.Length..].Trim();
            return new Command(ChangeDirectory, [path], null, null);
        }

        return ParseConversionCommand(input);
    }

    private static bool IsSpecialCommand(string input, out Command? command)
    {
        command = input switch
        {
            Q or Quit or Exit => new Command(Quit, [], null, null),
            Help => new Command(Help, [], null, null),
            _ => null
        };

        return command != null;
    }

    private static Command ParseConversionCommand(string input)
    {
        string? filePath = ExtractFilePath(input, [.. SupportedFileFormats]);
        string remainingInput;

        if (filePath != null)
        {
            remainingInput = input[(input.IndexOf(filePath) + filePath.Length)..].Trim();
            var parts = remainingInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? formatFlag = ExtractFormatFlag(parts);

            bool convertAll = parts.Contains(ConvertAll);
            string commandType = convertAll ? ConvertAll : ConvertFile;
            return new Command(commandType, [filePath], MapFormatFlag(formatFlag), null);
        }

        var allParts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (allParts.Contains(ConvertAll))
        {
            string? formatFlag = ExtractFormatFlag(allParts);
            return new Command(ConvertAll, [Directory.GetCurrentDirectory()], MapFormatFlag(formatFlag), null);
        }

        string? formatFlag2 = ExtractFormatFlag(allParts);
        return ParseDirectoryConversion(allParts, formatFlag2);
    }

    private static Command ParseDirectoryConversion(string[] parts, string? formatFlag)
    {
        var potentialPaths = parts.Where(p => !p.StartsWith("-")).ToList();

        if (!potentialPaths.Any() && formatFlag != null)
        {
            return new Command(ConvertAll,
                [Directory.GetCurrentDirectory()],
                MapFormatFlag(formatFlag),
                null);
        }

        foreach (var path in potentialPaths)
        {
            string relativePath = Path.Combine(Directory.GetCurrentDirectory(), path);

            if (Directory.Exists(path))
            {
                return new Command(ConvertAll, [path], MapFormatFlag(formatFlag), null);
            }
            if (Directory.Exists(relativePath))
            {
                return new Command(ConvertAll, [relativePath], MapFormatFlag(formatFlag), null);
            }
            if (File.Exists(path))
            {
                return new Command(ConvertFile, [path], MapFormatFlag(formatFlag), null);
            }
            if (File.Exists(relativePath))
            {
                return new Command(ConvertFile, [relativePath], MapFormatFlag(formatFlag), null);
            }
        }

        throw new CommandException("Invalid command or path.");
    }

    private static string? ExtractFormatFlag(string[] parts) =>
        parts.FirstOrDefault(IsFormatFlag);

    private static bool IsFormatFlag(string flag) =>
        flag is PngFlag
            or JpgFlag
            or JpegFlag
            or WebpFlag
            or GifFlag
            or TiffFlag
            or BmpFlag;

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
        PngFlag => PngFileFormat,
        JpgFlag => JpgFileFormat,
        JpegFlag => JpegFileFormat,
        WebpFlag => WebpFileFormat,
        GifFlag => GifFileFormat,
        TiffFlag => TiffFileFormat,
        BmpFlag => BmpFileFormat,
        _ => null
    };
}