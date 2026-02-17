using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Models;

namespace PixPorter.Common;

public class PixPorter(IUserInterface ui)
{
    public void Run()
    {
        ui.DisplayTitle("PixPorter");
        while (true)
        {
            ui.RenderUI(
                DirectoryHelper.GetDirectories(),
                DirectoryHelper.GetImageFiles());

            while (Console.KeyAvailable)
            {
                _ = Console.ReadKey(true);
            }

            string input = ui.Read("Enter command:");
            if (!string.IsNullOrWhiteSpace(input))
            {
                CommandResult result = CommandHelper.Process(input);
                ui.HandleResult(result);
            }
        }
    }
}