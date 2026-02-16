namespace PixPorter.Common.Interfaces;

public interface IUserInterace
{
    void RenderUI(IEnumerable<string> directories, IEnumerable<string> files, bool displayHelp = false);

    void RenderProgress(List<string> files, string targetFormat, Action<string, string> convertFileMethod);

    string Read(string prompt);

    void Write(string message);

    void WriteAndWait(string message);

    void DisplayErrorMessage(string message);

    void DisplayTitle(string title);

    List<T> GetHelpDetails<T>();
}