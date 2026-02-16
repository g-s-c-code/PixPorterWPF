namespace PixPorter.Common.Interfaces;

public interface IUserInterface
{
    void RenderUI(IEnumerable<string> directories, IEnumerable<string> files, bool displayHelp = false);
    void RenderProgress(IEnumerable<string> files, string targetExtension, Action<string, string> convert);
    string Read(string prompt);
    void Write(string message);
    void WriteAndWait(string message);
    void DisplayErrorMessage(string message);
    void DisplayTitle(string title);
}
