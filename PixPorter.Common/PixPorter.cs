using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Services;

namespace PixPorter.Common;

public class PixPorter(IUserInterface ui)
{
    private readonly CommandService _service = new(ui);

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
                Console.ReadKey(true);
            }

            string input = ui.Read("Enter command:");
            if (!string.IsNullOrWhiteSpace(input))
            {
                _service.Process(input);
            }
        }
    }
}