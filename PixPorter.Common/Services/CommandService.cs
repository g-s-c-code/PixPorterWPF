using PixPorter.Common.Core;
using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Models;

namespace PixPorter.Common.Services;

public class CommandService(IUserInterace ui)
{
    private readonly IUserInterace _ui = ui;

    public void ExecuteCommand(Command command)
    {
        switch (command.Name.ToLower())
        {
            case Constants.Quit:
                Environment.Exit(0);
                break;

            case Constants.Help:
                _ui.RenderUI(DirectoryHelper.GetDirectories(), DirectoryHelper.GetImageFiles(), true);
                _ui.WriteAndWait("Press any key to return...");
                break;

            case Constants.ChangeDirectory:
                ChangeDirectory(command.Arguments.FirstOrDefault() ?? "");
                break;

            case Constants.ConvertFile:
                ConvertFile(command.Arguments.FirstOrDefault() ?? "", command.TargetFormat);
                Console.ReadKey();
                break;

            case Constants.ConvertAll:
                ConvertDirectory(
                    command.Arguments.FirstOrDefault() ?? Directory.GetCurrentDirectory(),
                    command.TargetFormat);
                break;

            default:
                _ui.DisplayErrorMessage("Unknown command.");
                break;
        }
    }

    private void ChangeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _ui.DisplayErrorMessage("Path cannot be empty. Usage: cd [[path]]");
            return;
        }

        string newPath = Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        if (Directory.Exists(newPath))
        {
            Directory.SetCurrentDirectory(newPath);
        }
        else
        {
            _ui.DisplayErrorMessage($"Directory not found: {newPath}");
        }
    }

    private void ConvertFile(string path, string? targetFormat)
    {
        if (!File.Exists(path))
        {
            _ui.DisplayErrorMessage($"File not found: {path}");
            return;
        }

        try
        {
            ConversionHelper.ConvertFile(path, targetFormat);
            _ui.Write($"Converted: {path} -> {Path.ChangeExtension(path, targetFormat ?? ConversionHelper.GetDefaultFormat())}");
        }
        catch (Exception ex)
        {
            _ui.WriteAndWait($"Conversion failed: {ex.Message}");
        }
    }

    private void ConvertDirectory(string path, string? targetFormat)
    {
        if (!Directory.Exists(path))
        {
            _ui.DisplayErrorMessage($"Directory not found: {path}");
            return;
        }

        try
        {
            var files = ConversionHelper.GetSupportedFiles(path, targetFormat);

            if (files.Count == 0)
            {
                _ui.DisplayErrorMessage("No supported image files found in the directory.");
                return;
            }

            var effectiveTargetFormat = targetFormat ?? ConversionHelper.GetDefaultFormat();

            _ui.RenderProgress(files, effectiveTargetFormat, (file, format) =>
            {
                try
                {
                    ConversionHelper.ConvertFile(file, format);
                    _ui.Write($"Converted: {file} -> {Path.ChangeExtension(file, format)}");
                }
                catch (Exception ex)
                {
                    _ui.WriteAndWait($"Conversion failed for {file}: {ex.Message}");
                }
            });

            _ui.WriteAndWait("All supported files have been successfully processed.");
        }
        catch (Exception ex)
        {
            _ui.DisplayErrorMessage($"Failed to convert directory: {ex.Message}");
        }
    }

    public void ProcessCommand(string input)
    {
        try
        {
            var command = CommandHelper.ParseInput(input.ToLower());
            ExecuteCommand(command);
        }
        catch (CommandException ex)
        {
            _ui.DisplayErrorMessage($"Command Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _ui.DisplayErrorMessage($"Unexpected Error: {ex.Message}");
        }
    }
}