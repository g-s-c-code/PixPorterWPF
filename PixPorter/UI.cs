using PixPorter.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace PixPorter.Console;

public class UI : IUserInterface
{
    private const int LayoutWidth = 150;

    public void RenderUI(IEnumerable<string> directories, IEnumerable<string> files, bool displayHelp = false)
    {
        Table leftPanel = InformationContentUI();
        Panel rightPanel = DirectoryContentUI(CurrentDirectoryPathUI(), CurrentDirectoryContentUI(directories, files));
        Table content = displayHelp
            ? HelpContentUI()
            : PixPorterUI(leftPanel, rightPanel);

        AnsiConsole.Clear();
        AnsiConsole.Write(content);
    }

    public void RenderProgress(IEnumerable<string> files, string? targetExtension, Action<string, string?> convert)
    {
        List<string> list = [.. files];
        int successCount = 0;
        int failCount = 0;

        AnsiConsole.Progress().Start(ctx =>
        {
            ProgressTask task = ctx.AddTask("[lightskyblue1]Converting Images[/]", maxValue: list.Count);
            foreach (string file in list)
            {
                try
                {
                    convert(file, targetExtension);
                    task.Increment(1);
                    successCount++;
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage($"Conversion failed for {file}: {ex.Message}");
                    failCount++;
                }
            }
        });

        AnsiConsole.MarkupLine($"[lightskyblue1]Conversion complete:[/] [white]{successCount} successful[/]" +
                              (failCount > 0 ? $", [rosybrown]{failCount} failed[/]" : ""));
    }

    public string Read(string prompt) =>
        AnsiConsole.Ask<string>(prompt);

    public void Write(string message) =>
        AnsiConsole.MarkupLine($"[white]{message}[/]");

    public void WriteAndWait(string message)
    {
        Write(message);
        _ = System.Console.ReadKey();
    }

    public void DisplayErrorMessage(string message)
    {
        AnsiConsole.Write(new Markup(message, Color.RosyBrown));
        _ = System.Console.ReadKey();
    }

    public void DisplayTitle(string title) =>
        System.Console.Title = title;

    private static Table PixPorterUI(Table left, Panel right)
    {
        Table table = new()
        {
            Border = TableBorder.Horizontal,
            Title = new TableTitle("[lightskyblue1 bold]PixPorter - Image Format Converter[/]")
        };

        _ = table.AddColumn(new TableColumn(left).Padding(0, 0));
        _ = table.AddColumn(new TableColumn(right).Padding(0, 0));

        return table;
    }

    private static Panel DirectoryContentUI(Panel path, Table content) => new(new Rows([path, content]))
    {
        BorderStyle = Color.LightSkyBlue1,
        Header = new PanelHeader("[[ CURRENT DIRECTORY ]]"),
        Padding = new Padding(0, 1, 0, 0)
    };

    private static Table InformationContentUI()
    {
        Table table = new()
        {
            Border = TableBorder.None,
            Width = LayoutWidth
        };

        _ = table.AddColumn(new TableColumn(string.Empty)).HideHeaders();
        _ = table.Columns[0].Padding(0, 0);

        foreach (IRenderable section in GetInformationSections())
        {
            _ = table.AddRow(section);
        }

        return table;
    }

    private static Panel CurrentDirectoryPathUI()
    {
        TextPath path = new TextPath(Directory.GetCurrentDirectory().ToUpper())
            .RootColor(Color.White)
            .SeparatorColor(Color.RosyBrown)
            .StemColor(Color.White)
            .LeafColor(Color.White);

        return new Panel(path)
        {
            Border = BoxBorder.None,
            Width = LayoutWidth
        };
    }

    private static Table CurrentDirectoryContentUI(IEnumerable<string> directories, IEnumerable<string> files)
    {
        Table table = new()
        {
            Border = TableBorder.Simple,
            Width = LayoutWidth
        };

        _ = table.AddColumn(new TableColumn(BuildTree("Folders:".ToUpper(), directories)));
        _ = table.AddColumn(new TableColumn(BuildTree("Image Files:".ToUpper(), files)));
        _ = table.Columns[0].Padding(0, 0);
        _ = table.Columns[1].Padding(0, 0);

        return table;
    }

    private static Table HelpContentUI()
    {
        Table table = new()
        {
            Border = TableBorder.Horizontal,
            Width = LayoutWidth,
            Title = new TableTitle("[lightskyblue1 bold]PixPorter – Help & Usage Guide[/]")
        };

        _ = table.AddColumn(new TableColumn(string.Empty)).HideHeaders();
        foreach (IRenderable section in GetHelpSections())
        {
            _ = table.AddRow(section);
        }

        return table;
    }

    private static Tree BuildTree(string title, IEnumerable<string> items)
    {
        Tree tree = new(new Markup(title.ToUpper(), Color.White))
        {
            Style = new Style(foreground: Color.RosyBrown)
        };

        foreach (string? item in items.DefaultIfEmpty("[dim italic]None[/]"))
        {
            _ = tree.AddNode($"[bold white]{item}[/]");
        }

        return tree;
    }

    private static IEnumerable<IRenderable> GetInformationSections()
    {
        yield return BuildSection("[lightskyblue1 underline bold]Usage Quick Guide[/]",
        [
            ("[indianred bold]DRAG & DROP[/]",
            "\n- Drag and drop an image or folder into the window and press '[steelblue][[ENTER]][/]' to convert it. Add a format flag to override defaults.\n"),
            ("[indianred bold]NAVIGATION[/]",
            "\n- Use '[steelblue]cd [[path]][/]' to navigate folders. Convert all images with '[steelblue]--ca[/]'.")
        ]);

        yield return BuildSection("[lightskyblue1 underline bold]Commands[/]",
        [
            ("[steelblue]--ca[/]     ", "- Convert all images in the [lightskyblue1 bold]current directory[/]"),
            ("[steelblue]cd [[path]][/]", "- Change directory"),
            ("[steelblue]help[/]     ", "- Open the detailed instructions menu"),
            ("[steelblue]q[/]        ", "- Exit application")
        ]);

        yield return BuildSection("[lightskyblue1 underline bold]CONVERSION FORMAT FLAGS[/]          [lightskyblue1 underline bold]DEFAULT CONVERSION FORMATS[/]",
        [
            ("[steelblue]--png[/]     - Convert to PNG", "      [indianred].png[/]  → [indianred].webp[/]"),
            ("[steelblue]--jpg[/]     - Convert to JPG", "      [indianred].jpg[/]  → [indianred].webp[/]"),
            ("[steelblue]--webp[/]    - Convert to WebP", "     [indianred].webp[/] → [indianred].png[/]"),
            ("[steelblue]--gif[/]     - Convert to GIF", "      [indianred].gif[/]  → [indianred].png[/]"),
            ("[steelblue]--tiff[/]    - Convert to TIFF", "     [indianred].tiff[/] → [indianred].png[/]"),
            ("[steelblue]--bmp[/]     - Convert to BMP", "      [indianred].bmp[/]  → [indianred].png[/]")
        ], skipTitleFormatting: true);
    }

    private static IEnumerable<IRenderable> GetHelpSections()
    {
        yield return BuildSection("Drag & Drop",
        [
            ("Drag a file or folder into the PixPorter window. Add an optional format flag if desired.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png[/]' + '[steelblue][[ENTER]][/]' → Converts to the default format (e.g., '[steelblue]my_photo.webp[/]').", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png --jpg[/]' + '[steelblue][[ENTER]][/]' → Converts to JPG (e.g., '[steelblue]my_photo.jpg[/]').", "")
        ]);

        yield return BuildSection("Direct File/Folder Conversion",
        [
            ("Enter a full file path or folder path + '[steelblue][[ENTER]][/]' for automatic conversion.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]C:\\Users\\Pictures --webp[/]' + '[steelblue][[ENTER]][/]' → Converts all images in the folder to WebP.", "")
        ]);

        yield return BuildSection("Current Directory Conversion",
        [
            ("Use the command line to navigate to a directory and perform conversions.", ""),
            ("[steelblue]cd [[path]][/]   - Navigate to the desired directory.", ""),
            ("[steelblue]--ca[/]         - Converts all images in the current directory.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]cd C:\\Users\\Photos[/]' + '[steelblue][[ENTER]][/]' → Navigate to the directory.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]--ca --jpg[/]' + '[steelblue][[ENTER]][/]' → Converts all images in the current directory to JPG.", "")
        ]);

        yield return BuildSection("How to Use",
        [
            ("Add format flags [italic]if[/] you need a specific output format. These are optional since default mappings are pre-set.", ""),
            ("Default mappings:",
             "[indianred].png[/] → [indianred].webp[/] | [indianred].jpg[/] → [indianred].webp[/] | [indianred].jpeg[/] → [indianred].webp[/] | [indianred].webp[/] → [indianred].png[/] | [indianred].gif[/] → [indianred].png[/] | [indianred].tiff[/] → [indianred].png[/] | [indianred].bmp[/] → [indianred].png[/]")
        ]);

        yield return BuildSection("Flags",
        [
            ("[steelblue]--png[/]   ", "- Convert to PNG"),
            ("[steelblue]--jpg[/]   ", "- Convert to JPG"),
            ("[steelblue]--webp[/]  ", "- Convert to WebP"),
            ("[steelblue]--gif[/]   ", "- Convert to GIF"),
            ("[steelblue]--tiff[/]  ", "- Convert to TIFF"),
            ("[steelblue]--bmp[/]   ", "- Convert to BMP"),
            ("[steelblue]--ca[/]    ", "- Convert all image files in the current directory")
        ]);
    }

    private static Markup BuildSection(string title, IEnumerable<(string Key, string Value)> items, string? footer = null, bool skipTitleFormatting = false)
    {
        StringBuilder sb = new();
        if (skipTitleFormatting)
        {
            _ = sb.AppendLine(title);
        }
        else
        {
            _ = sb.AppendLine($"[lightskyblue1 underline bold]{title.ToUpper()}[/]");
        }

        foreach ((string? key, string? value) in items)
        {
            _ = sb.AppendLine($"[white]{key}[/] {value}");
        }

        if (footer != null)
        {
            _ = sb.AppendLine($"\n{footer}");
        }

        return new Markup(sb.ToString());
    }
}
