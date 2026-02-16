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
                // Handle multiple files if present
                List<string> filesToConvert = new() { command.Path };
                if (command.AdditionalPaths != null)
                {
                    filesToConvert.AddRange(command.AdditionalPaths);
                }

                if (filesToConvert.Count == 1)
                {
                    // Single file - show result and wait
                    ConvertSingle(command.Path, command.TargetExtension);
                    ui.WriteAndWait("\nPress any key to continue...");
                }
                else
                {
                    string effectiveTarget = command.TargetExtension;

                    ui.Write($"\nConverting {filesToConvert.Count} file(s)...\n");
                    ui.RenderProgress(filesToConvert, effectiveTarget, (file, format) =>
                    {
                        // For each file, determine the appropriate target
                        string fileTarget = format ?? Constants.GetDefaultTarget(Path.GetExtension(file));
                        ConversionHelper.ConvertFile(file, fileTarget);
                    });
                    ui.WriteAndWait("\nPress any key to continue...");
                }
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

        string sourceExtension = Path.GetExtension(path);
        string effectiveTarget = targetExtension ?? Constants.GetDefaultTarget(sourceExtension);
        string outputPath = Path.ChangeExtension(path, effectiveTarget);

        ConversionHelper.ConvertFile(path, effectiveTarget);
        ui.Write($"Converted: {path} → {outputPath}");
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

        // If explicit target provided, use it; otherwise use null to handle per-file defaults
        string displayTarget = targetExtension != null
            ? targetExtension.ToUpper()
            : "default formats";

        ui.Write($"\nConverting {files.Count} file(s) to {displayTarget}...\n");

        ui.RenderProgress(files, targetExtension, (file, format) =>
        {
            string fileTarget = format ?? Constants.GetDefaultTarget(Path.GetExtension(file));
            ConversionHelper.ConvertFile(file, fileTarget);
        });

        ui.WriteAndWait("\nPress any key to continue...");
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
