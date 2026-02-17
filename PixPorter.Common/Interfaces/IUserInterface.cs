using PixPorter.Common.Models;

namespace PixPorter.Common.Interfaces;

public interface IUserInterface
{
    void HandleResult(CommandResult result);
    void RenderUI(IEnumerable<string> directories, IEnumerable<string> files, bool displayHelp = false);
    string Read(string prompt);
    void Write(string message);
    void DisplayErrorMessage(string message);
    void DisplayTitle(string title);
}