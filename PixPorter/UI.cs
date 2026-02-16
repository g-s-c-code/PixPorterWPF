using System.Text;
using PixPorter.Common.Core;
using PixPorter.Common.Interfaces;
using PixPorter.Common.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

public class UI : IUserInterace
{
	private const int LayoutWidth = 150;

	#region Public Methods

	public void RenderUI(IEnumerable<string> directories, IEnumerable<string> files, bool displayHelp = false)
	{
		var leftPanel = InformationContentUI();
		var rightPanel = DirectoryContentUI(CurrentDirectoryPathUI(), CurrentDirectoryContentUI(directories, files));
		var pixPorterUI = displayHelp ? HelpContentUI() : PixPorterUI(leftPanel, rightPanel);

		AnsiConsole.Clear();
		AnsiConsole.Write(pixPorterUI);
	}

	public void RenderProgress(List<string> files, string targetFormat, Action<string, string> convertFileMethod)
	{
		AnsiConsole.Progress()
			.Start(ctx =>
			{
				var conversionTask = ctx.AddTask("Converting Images", maxValue: files.Count);

				foreach (var file in files)
				{
					try
					{
						conversionTask.Increment(1);
						convertFileMethod(file, targetFormat);
					}
					catch (Exception ex)
					{
						DisplayErrorMessage($"Conversion failed for {file}: {ex.Message}");
					}
				}
			});
	}

	public string Read(string prompt) => AnsiConsole.Ask<string>(prompt);

	public void Write(string message)
	{
		AnsiConsole.Write(new Markup(message, Color.White));
	}

	public void WriteAndWait(string message)
	{
		AnsiConsole.Write(new Markup(message, Color.White));
		Console.ReadKey();
	}

	public void DisplayErrorMessage(string message)
	{
		AnsiConsole.Write(new Markup(message, Color.RosyBrown));
		Console.ReadKey();
	}

	public void DisplayTitle(string title)
	{
		Console.Title = title;
	}

	public List<IRenderable> GetHelpDetails()
	{
		return new List<IRenderable>
		{
			BuildHelpSection("Drag & Drop"),
			BuildHelpSection("Direct File/Folder Conversion"),
			BuildHelpSection("Current Directory Conversion"),
			BuildHelpSection("How to Use"),
			BuildHelpSection("Flags")
		};
	}

	#endregion

	#region Private UI Component Methods

	private Table PixPorterUI(Table leftPanel, Panel rightPanel)
	{
		var layoutTable = new Table
		{
			Border = TableBorder.Horizontal,
			Title = new TableTitle("PixPorter - Image Format Converter")
		};

		layoutTable.AddColumn(new TableColumn(leftPanel).Padding(0, 0));
		layoutTable.AddColumn(new TableColumn(rightPanel).Padding(0, 0));

		return layoutTable;
	}

	private Panel DirectoryContentUI(Panel currentDirectoryPathUI, Table currentDirectoryContentUI)
	{
		return new Panel(new Rows([currentDirectoryPathUI, currentDirectoryContentUI]))
		{
			BorderStyle = Color.LightSkyBlue1,
			Header = new PanelHeader("[[ Current Directory ]]".ToUpper()),
			Padding = new Padding(0, 1, 0, 0),
		};
	}

	private Table InformationContentUI()
	{
		var table = new Table
		{
			Border = TableBorder.None,
			Width = LayoutWidth
		};

		table.AddColumn(new TableColumn(string.Empty)).HideHeaders();
		table.Columns[0].Padding(0, 0, 0, 0);

		AddInformationSections(table);

		return table;
	}

	private Panel CurrentDirectoryPathUI()
	{
		var currentDirectory = new TextPath(Directory.GetCurrentDirectory().ToUpper())
			.RootColor(Color.White)
			.SeparatorColor(Color.RosyBrown)
			.StemColor(Color.White)
			.LeafColor(Color.White);

		return new Panel(currentDirectory)
		{
			Border = BoxBorder.None,
			Width = LayoutWidth
		};
	}

	private Table CurrentDirectoryContentUI(IEnumerable<string> directories, IEnumerable<string> files)
	{
		var table = new Table
		{
			Border = TableBorder.Simple,
			Width = LayoutWidth
		};

		table.AddColumn(new TableColumn(BuildTree("Folders:".ToUpper(), directories)));
		table.AddColumn(new TableColumn(BuildTree("Image Files:".ToUpper(), files)));
		table.Columns[0].Padding(0, 0);
		table.Columns[1].Padding(0, 0);

		return table;
	}

	private Table HelpContentUI()
	{
		var table = new Table
		{
			Border = TableBorder.Horizontal,
			Width = LayoutWidth,
			Title = new TableTitle("PixPorter - Image Format Converter"),
		};

		table.AddColumn(new TableColumn(string.Empty)).HideHeaders();

		foreach (var section in GetHelpDetails())
		{
			table.AddRow(section);
		}

		return table;
	}

	#endregion

	#region Helper Methods

	private void AddInformationSections(Table table)
	{
		var sections = new[]
		{
			BuildSection("[lightskyblue1 underline bold]Usage Quick Guide[/]", GetQuickGuideItems()),
			BuildSection("[lightskyblue1 underline bold]Commands[/]", GetCommandItems()),
			BuildSection("[lightskyblue1 underline bold]Conversion Format Flags[/]          [lightskyblue1 underline bold]Default Conversion Formats[/]", GetFormatItems())
		};

		foreach (var section in sections)
		{
			table.AddRow(section);
		}
	}

	private IRenderable BuildTree(string header, IEnumerable<string> items)
	{
		var tree = new Tree(new Markup(header, Color.White))
		{
			Style = new Style(foreground: Color.RosyBrown)
		};

		foreach (var node in items.DefaultIfEmpty("[dim italic]None[/]").Select(item => $"[bold white]{item}[/]"))
		{
			tree.AddNode(node);
		}

		return tree;
	}

	private IRenderable BuildSection(string title, (string Key, string Value)[] items)
	{
		var sectionContent = new StringBuilder().AppendLine(title.ToUpper());

		foreach (var (key, value) in items)
		{
			sectionContent.AppendLine($"[white]{key}[/] {value}");
		}

		return new Markup(sectionContent.ToString());
	}

	private (string Key, string Value)[] GetQuickGuideItems()
	{
		return new[]
		{
			("[indianred bold]DRAG & DROP[/]", "\n- Drag and drop an image to this window and press '[steelblue][[ENTER]][/]' to convert it. Add a format flag to specify format.\n"),
			("[indianred bold]NAVIGATION[/]", "\n- Use '[steelblue]cd [[path]][/]' to navigate to a folder containing images. Convert all viable images with '[steelblue]--ca[/]'.")
		};
	}

	private (string Key, string Value)[] GetCommandItems()
	{
		return new[]
		{
			("[steelblue]--ca[/]     ", "- Convert all images in the [lightskyblue1 bold]current directory[/]"),
			("[steelblue]cd [[path]][/]", "- Change directory"),
			("[steelblue]help[/]     ", "- Open the detailed instructions menu"),
			("[steelblue]q[/]        ", "- Exit application")
		};
	}

	private (string Key, string Value)[] GetFormatItems()
	{
		return new[]
		{
			("[steelblue]--png[/]     - Convert to PNG", "      [indianred].png[/] -> [indianred].webp[/]"),
			("[steelblue]--jpg[/]     - Convert to JPG", "      [indianred].jpg[/] -> [indianred].webp[/]"),
			("[steelblue]--webp[/]    - Convert to WebP", "     [indianred].webp[/] -> [indianred].png[/]"),
			("[steelblue]--gif[/]     - Convert to GIF", "      [indianred].gif[/] -> [indianred].png[/]"),
			("[steelblue]--tiff[/]    - Convert to TIFF", "     [indianred].tiff[/] -> [indianred].png[/]"),
			("[steelblue]--bmp[/]     - Convert to BMP", "      [indianred].bmp[/] -> [indianred].png[/]")
		};
	}

	public List<T> GetHelpDetails<T>()
	{
		if (typeof(T) != typeof(IRenderable))
		{
			throw new InvalidOperationException($"Help details of type {typeof(T)} are not supported.");
		}

		var helpDetails = new List<IRenderable>
		{
			BuildHelpSection("Drag & Drop"),
			BuildHelpSection("Direct File/Folder Conversion"),
			BuildHelpSection("Current Directory Conversion"),
			BuildHelpSection("How to Use"),
			BuildHelpSection("Flags")
		};

		return helpDetails as List<T> ?? throw new CommandException("Error fetching help section");
	}

	private IRenderable BuildHelpSection(string sectionType)
	{
		Dictionary<string, string> defaultConversions = new()
		{
			{ Constants.PngFileFormat, Constants.WebpFileFormat },
			{ Constants.JpgFileFormat, Constants.WebpFileFormat },
			{ Constants.JpegFileFormat, Constants.WebpFileFormat },
			{ Constants.WebpFileFormat, Constants.PngFileFormat },
			{ Constants.GifFileFormat, Constants.PngFileFormat },
			{ Constants.TiffFileFormat, Constants.PngFileFormat },
			{ Constants.BmpFileFormat, Constants.PngFileFormat }
		};

		switch (sectionType)
		{
			case "Drag & Drop":
				return BuildSection("Drag & Drop", new[]
				{
			("Drag a file or folder into the PixPorter window. Add an optional format flag if desired.", ""),
			("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png[/]' + '[steelblue][[ENTER]][/]' -> Converts to the default format (e.g., '[steelblue]my_photo.webp[/]').", ""),
			("[indianred]EXAMPLE:[/] '[steelblue]my_photo.png --jpg[/]' + '[steelblue][[ENTER]][/]' -> Converts to JPG (e.g., '[steelblue]my_photo.jpg[/]').", "")
		});
			case "Direct File/Folder Conversion":
				return BuildSection("Direct File/Folder Conversion", new[]
				{
			("Enter a full file path or a folder path (and an optional format flag) + '[steelblue][[ENTER]][/]' for automatic conversion.", ""),
			("[indianred]EXAMPLE:[/] '[steelblue]C:\\Users\\Pictures --webp[/]' + '[steelblue][[ENTER]][/]' -> Converts all images in the folder to WebP.", "")
		});
			case "Current Directory Conversion":
				return BuildSection("Current Directory Conversion", new[]
				{
			("Use the command line to navigate to a directory and perform conversions.", ""),
			("[steelblue]cd [[path]][/]   - Navigate to the desired directory.", ""),
			("[steelblue]--ca[/]         - Converts all images in the current directory.", ""),
			("[indianred]EXAMPLE:[/] '[steelblue]cd C:\\Users\\Photos[/]' + '[steelblue][[ENTER]][/]' -> Navigate to the directory.", ""),
			("[indianred]EXAMPLE:[/] '[steelblue]--ca --jpg[/]' + '[steelblue][[ENTER]][/]' -> Converts all images in the current directory to JPG.", "")
		});
			case "How to Use":
				return BuildSection("How to Use", new[]
				{
			("Add format flags [italic]if[/] you need a specific output format. These are optional since default mappings are pre-set.", ""),
			($"Default mappings: {string.Join(" | ", defaultConversions.Select(c => $"[indianred]{c.Key}[/] -> [indianred]{c.Value}[/]"))}", "")
		});
			case "Flags":
				return BuildSection("Flags", new[]
				{
			("[steelblue]--png[/]   ", "- Convert to PNG"),
			("[steelblue]--jpg[/]   ", "- Convert to JPG"),
			("[steelblue]--webp[/]  ", "- Convert to WebP"),
			("[steelblue]--gif[/]   ", "- Convert to GIF"),
			("[steelblue]--tiff[/]  ", "- Convert to TIFF"),
			("[steelblue]--bmp[/]   ", "- Convert to BMP"),
			("[steelblue]--ca[/]    ", "- Convert all image files in the current directory"),
		});
			default:
				return new Markup("Section not found");
		}
	}

	#endregion
}