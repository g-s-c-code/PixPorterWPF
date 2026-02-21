using PixPorter.Common.Core;
using PixPorter.Common.Helpers;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Text;

namespace PixPorter.Console;

public class UI : IUserInterface
{
    private const int LayoutWidth = 150;

    public void HandleResult(CommandResult result)
    {
        switch (result)
        {
            case CommandResultQuit:
                Environment.Exit(0);
                break;

            case CommandResultHelp:
                RenderUI(
                    DirectoryHelper.GetDirectories(),
                    DirectoryHelper.GetImageFiles(),
                    displayHelp: true);
                WriteAndWait("Press any key to return...");
                break;

            case CommandResultDirectoryChanged:
                break;

            case CommandResultSuccess r:
                Write(r.Message);
                WriteAndWait("\nPress any key to continue...");
                break;

            case CommandResultMultiConvert r:
                RenderProgress(r.Files, r.TargetExtension, r.Quality, r.StripMetadata);
                WriteAndWait("\nPress any key to continue...");
                break;

            case CommandResultError r:
                DisplayErrorMessage(r.Message);
                break;
        }
    }

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

    private void RenderProgress(IReadOnlyList<string> files, string? targetExtension, int? quality, bool stripMetadata)
    {
        int successCount = 0;
        int failCount = 0;

        AnsiConsole.Progress().Start(ctx =>
        {
            ProgressTask task = ctx.AddTask("[lightskyblue1]Converting Images[/]", maxValue: files.Count);
            foreach (string file in files)
            {
                try
                {
                    string fileTarget = targetExtension ?? Constants.GetDefaultTarget(Path.GetExtension(file));
                    ConversionHelper.ConvertFile(file, fileTarget, quality, stripMetadata);
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
        AnsiConsole.MarkupLine($"[white]{Markup.Escape(message)}[/]");

    public void DisplayErrorMessage(string message)
    {
        AnsiConsole.Write(new Markup(Markup.Escape(message), Color.RosyBrown));
        _ = System.Console.ReadKey();
    }

    public void DisplayTitle(string title) =>
        System.Console.Title = title;

    private static void WriteAndWait(string message)
    {
        AnsiConsole.MarkupLine($"[white]{Markup.Escape(message)}[/]");
        _ = System.Console.ReadKey();
    }

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
            _ = table.AddRow(section);

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
            _ = table.AddRow(section);

        return table;
    }

    private static Tree BuildTree(string title, IEnumerable<string> items)
    {
        Tree tree = new(new Markup(title.ToUpper(), Color.White))
        {
            Style = new Style(foreground: Color.RosyBrown)
        };

        foreach (string? item in items.DefaultIfEmpty("[dim italic]None[/]"))
            _ = tree.AddNode($"[bold white]{item}[/]");

        return tree;
    }

    private static IEnumerable<IRenderable> GetInformationSections()
    {
        yield return BuildSection("[lightskyblue1 underline bold]Usage Quick Guide[/]",
        [
            ("[indianred bold]DRAG & DROP:[/]",
            "Drag and drop an image or folder into the window and press '[steelblue][[ENTER]][/]' to convert it."),
            ("[indianred bold]NAVIGATION:[/]",
            "Use '[steelblue]cd [[path]][/]' to navigate folders. Convert all images with '[steelblue]--ca[/]'."),
            ("[indianred bold]FLAGS:[/]",
            "Add a format flag (e.g. '[steelblue]--jpg[/]') to override the default output format. Use '[steelblue]--quality=N[/]' (1-100) to set output quality — omitting it uses maximum quality. Use '[steelblue]--stripmeta[/]' to strip embedded metadata from the output. Formats marked [lightskyblue1]*[/] support the quality flag.")
        ]);

        yield return BuildSection("[lightskyblue1 underline bold]Commands[/]",
        [
            ("[steelblue]cd [[path]][/] ", "   Change directory"),
            ("[steelblue]help[/]      ",   "   Open the detailed instructions menu"),
            ("[steelblue]q[/]         ",   "   Exit application")
        ]);

        yield return BuildSection("[lightskyblue1 underline bold]FLAGS[/]                                      [lightskyblue1 underline bold]DEFAULTS[/]",
        [
            ("[steelblue]--webp[/]        Convert to WebP[lightskyblue1]*[/]",       "            [indianred].webp[/] → [indianred].png[/]"),
            ("[steelblue]--png[/]         Convert to PNG[lightskyblue1]*[/]",        "             [indianred].png[/]  → [indianred].webp[/]"),
            ("[steelblue]--jpg[/]         Convert to JPG[lightskyblue1]*[/]",        "             [indianred].jpg[/]  → [indianred].webp[/]"),
            ("[steelblue]--gif[/]         Convert to GIF",                           "              [indianred].gif[/]  → [indianred].webp[/]"),
            ("[steelblue]--tiff[/]        Convert to TIFF",                          "             [indianred].tiff[/] → [indianred].webp[/]"),
            ("[steelblue]--bmp[/]         Convert to BMP",                           "              [indianred].bmp[/]  → [indianred].webp[/]"),
            ("[steelblue]--tga[/]         Convert to TGA",                           "              [indianred].tga[/]  → [indianred].webp[/]"),
            ("[steelblue]--qoi[/]         Convert to QOI",                           "              [indianred].qoi[/]  → [indianred].webp[/]"),
            ("[steelblue]--pbm[/]         Convert to PBM",                           "              [indianred].pbm[/]  → [indianred].webp[/]"),
            ("[steelblue]--quality=N[/]   Set output quality (1-100)",               "  [indianred]100 (maximum)[/]"),
            ("[steelblue]--stripmeta[/]   Strip embedded metadata from output",      ""),
            ("[steelblue]--ca[/]          Convert all images in the [lightskyblue1 bold]current directory[/]", "")
        ], skipTitleFormatting: true, trimEnd: true);
    }

    private static IEnumerable<IRenderable> GetHelpSections()
    {
        yield return BuildSection("Drag & Drop",
        [
            ("Drag a file or folder into the PixPorter window. Add an optional format flag if desired.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png[/]' + '[steelblue][[ENTER]][/]' → Converts to the default format (e.g., '[steelblue]my_photo.webp[/]').", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png --jpg[/]' + '[steelblue][[ENTER]][/]' → Converts to JPG (e.g., '[steelblue]my_photo.jpg[/]').", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png --jpg --quality=85[/]' + '[steelblue][[ENTER]][/]' → Converts to JPG at quality 85.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png --jpg --stripmeta[/]' + '[steelblue][[ENTER]][/]' → Converts to JPG and strips all embedded metadata.", "")
        ]);

        yield return BuildSection("Direct File/Folder Conversion",
        [
            ("Enter a full file path or folder path + '[steelblue][[ENTER]][/]' for automatic conversion.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]C:\\Users\\Pictures[/]' + '[steelblue][[ENTER]][/]' → Converts all images in the folder using default format mappings.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]C:\\Users\\Pictures --webp --quality=90[/]' + '[steelblue][[ENTER]][/]' → Converts all images to WebP at quality 90.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]C:\\Users\\Pictures --webp --stripmeta[/]' + '[steelblue][[ENTER]][/]' → Converts all images to WebP and strips metadata.", "")
        ]);

        yield return BuildSection("Current Directory Conversion",
        [
            ("Use '[steelblue]cd [[path]][/]' to navigate to a directory, then convert images with '[steelblue]--ca[/]'.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]cd C:\\Users\\Photos[/]' + '[steelblue][[ENTER]][/]' → Navigate to the directory.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]--ca[/]' + '[steelblue][[ENTER]][/]' → Converts all images using default format mappings.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]--ca --jpg --quality=75[/]' + '[steelblue][[ENTER]][/]' → Converts all images to JPG at quality 75.", ""),
            ("[indianred]EXAMPLE:[/] '[steelblue]--ca --stripmeta[/]' + '[steelblue][[ENTER]][/]' → Converts all images and strips metadata.", "")
        ]);

        yield return BuildSection("Quality",
        [
            ("Use '[steelblue]--quality=N[/]' to control output quality, where N is between [white]1[/] (smallest file) and [white]100[/] (best quality). Omitting it defaults to maximum quality.", ""),
            ("[dim]JPG and WebP are lossy — quality directly affects pixel fidelity.[/]", ""),
            ("[dim]PNG is lossless — quality controls compression ratio only, not pixel fidelity.[/]", ""),
            ("[dim]GIF, BMP, TIFF, TGA, QOI, and PBM do not support quality and always use their defaults.[/]", "")
        ]);

        yield return BuildSection("Metadata",
        [
            ("Use '[steelblue]--stripmeta[/]' to remove all embedded metadata from the output file.", ""),
            ("[dim]Strips EXIF (camera info, GPS), IPTC (copyright, keywords), XMP, and ICC colour profiles.[/]", ""),
            ("[dim]Useful for reducing file size or removing sensitive location and device data before sharing.[/]", "")
        ]);

        yield return BuildSection("Default Format Mappings",
        [
            ("[indianred].png[/]  → [indianred].webp[/]", ""),
            ("[indianred].jpg[/]  → [indianred].webp[/]", ""),
            ("[indianred].jpeg[/] → [indianred].webp[/]", ""),
            ("[indianred].webp[/] → [indianred].png[/]",  ""),
            ("[indianred].gif[/]  → [indianred].webp[/]", ""),
            ("[indianred].tiff[/] → [indianred].webp[/]", ""),
            ("[indianred].bmp[/]  → [indianred].webp[/]", ""),
            ("[indianred].tga[/]  → [indianred].webp[/]", ""),
            ("[indianred].qoi[/]  → [indianred].webp[/]", ""),
            ("[indianred].pbm[/]  → [indianred].webp[/]", "")
        ]);

        yield return BuildSection("Flags",
        [
            ("[steelblue]--webp[/]      ", "- Convert to WebP [lightskyblue1]*[/]"),
            ("[steelblue]--png[/]       ", "- Convert to PNG  [lightskyblue1]*[/]"),
            ("[steelblue]--jpg[/]       ", "- Convert to JPG  [lightskyblue1]*[/]"),
            ("[steelblue]--gif[/]       ", "- Convert to GIF"),
            ("[steelblue]--tiff[/]      ", "- Convert to TIFF"),
            ("[steelblue]--bmp[/]       ", "- Convert to BMP"),
            ("[steelblue]--tga[/]       ", "- Convert to TGA"),
            ("[steelblue]--qoi[/]       ", "- Convert to QOI"),
            ("[steelblue]--pbm[/]       ", "- Convert to PBM"),
            ("[steelblue]--quality=N[/] ", "- Set output quality 1–100. [lightskyblue1]*[/] formats only. Defaults to 100."),
            ("[steelblue]--stripmeta[/] ", "- Strip all embedded metadata (EXIF, IPTC, XMP, ICC) from the output."),
            ("[steelblue]--ca[/]        ", "- Convert all images in the current directory")
        ]);
    }

    private static Markup BuildSection(string title, IEnumerable<(string Key, string Value)> items, string? footer = null, bool skipTitleFormatting = false, bool trimEnd = false)
    {
        StringBuilder sb = new();
        _ = skipTitleFormatting
            ? sb.AppendLine(title)
            : sb.AppendLine($"[lightskyblue1 underline bold]{title.ToUpper()}[/]");

        foreach ((string? key, string? value) in items)
            _ = sb.AppendLine($"[white]{key}[/] {value}");

        if (footer != null)
            _ = sb.Append(footer);

        string raw = sb.ToString();
        return new Markup(trimEnd ? raw.TrimEnd() : raw);
    }
}