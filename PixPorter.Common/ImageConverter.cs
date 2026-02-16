using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Services;

namespace PixPorter.Common;

public class ImageConverter
{
    private readonly IUserInterace _ui;
    private readonly CommandService _commandService;

    public ImageConverter(IUserInterace ui)
    {
        _ui = ui;
        _commandService = new CommandService(_ui);
    }

    public void Run()
    {
        _ui.DisplayTitle("PixPorter - Image Format Converter");

        while (true)
        {
            _ui.RenderUI(DirectoryHelper.GetDirectories(), DirectoryHelper.GetImageFiles());
            var input = _ui.Read("Enter command:");

            if (string.IsNullOrWhiteSpace(input))
                continue;

            _commandService.ProcessCommand(input);
        }
    }
}