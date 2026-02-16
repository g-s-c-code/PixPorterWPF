using PixPorter.Common.Core;
using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Models;

namespace PixPorter.Common.Services;

public class CommandService(IUserInterface ui)
{
    public void Execute(Command command)
    {
        switch (command.Name)
        {
            case Constants.Quit:
                Environment.Exit(0);
                break;

            case Constants.Help:
                ui.RenderUI(DirectoryHelper.GetDirectories(), DirectoryHelper.GetImageFiles(), true);
                ui.WriteAndWait("Press any key to return...");
                break;

            case Constants.ChangeDirectory:
                ChangeDirectory(command.Path);
                break;

            case Constants.ConvertFile:
                ConvertSingle(command.Path, command.TargetExtension);
                break;

            case Constants.ConvertAll:
                ConvertDirectory(command.Path, command.TargetExtension);
                break;

            default:
                ui.DisplayErrorMessage("Unknown command.");
                break;
        }
    }

    private void ChangeDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            ui.DisplayErrorMessage($"Directory not found: {path}");
            return;
        }

        Directory.SetCurrentDirectory(path);
    }

    private void ConvertSingle(string path, string? targetExtension)
    {
        if (!File.Exists(path))
        {
            ui.DisplayErrorMessage($"File not found: {path}");
            return;
        }

        ConversionHelper.ConvertFile(path, targetExtension);
        ui.Write($"Converted: {path}");
    }

    private void ConvertDirectory(string path, string? targetExtension)
    {
        if (!Directory.Exists(path))
        {
            ui.DisplayErrorMessage($"Directory not found: {path}");
            return;
        }

        List<string> files = ConversionHelper.GetConvertibleFiles(path).ToList();

        if (!files.Any())
        {
            ui.DisplayErrorMessage("No supported images found.");
            return;
        }

        string effectiveTarget = targetExtension ?? Constants.GetDefaultTarget(Path.GetExtension(files[0]));

        ui.RenderProgress(files, effectiveTarget, (file, format) =>
        {
            ConversionHelper.ConvertFile(file, format);
            ui.Write($"Converted: {file}");
        });

        ui.WriteAndWait("Conversion complete.");
    }

    public void Process(string input)
    {
        try
        {
            Command command = CommandHelper.Parse(input.ToLower());
            Execute(command);
        }
        catch (Exception ex)
        {
            ui.DisplayErrorMessage(ex.Message);
        }
    }
}
