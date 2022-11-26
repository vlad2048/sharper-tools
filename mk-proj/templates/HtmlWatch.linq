<Query Kind="Program">
  <NuGetReference>CliFx</NuGetReference>
  <Namespace>CliFx</Namespace>
  <Namespace>CliFx.Attributes</Namespace>
  <Namespace>CliFx.Infrastructure</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

#load "..\_libs\base"

void Main()
{
	TemplateUtils.GetTemplateFolder("HtmlWatch").Dump();
	return;

	var projNfo = new ProjNfo(
		@"C:\Dev\sharper-tools\_infos\design",
		"dlg"
	);
	FileUtils.DeleteFolder(projNfo.ProjFolder);
	
	HtmlWatchCommand.Run(projNfo);
}

[Command("HtmlWatch")]
public class HtmlWatchCommand : ICommand
{
	[CommandParameter(0)]
	public string Name { get; init; }
	
	[CommandOption("linqpad", 'l', Description = "")]
	public bool LinqpadTemplate { get; init; }
	
    public ValueTask ExecuteAsync(IConsole con)
	{
		var nfo = ProjUtils.Init(Name);
		Run(nfo, LinqpadTemplate);
        return default;
    }
	
	public static void Run(ProjNfo nfo, bool linqpad)
	{
		Console.WriteLine($"Name   : {nfo.Name}");
		Console.WriteLine($"Linqpad: {linqpad}");
		Directory.CreateDirectory(nfo.ProjFolder);
		Directory.SetCurrentDirectory(nfo.ProjFolder);
		
		var templateFolder = TemplateUtils.GetTemplateFolder("HtmlWatch");
		var commonFolder = Path.Combine(templateFolder, "common");
		var basicFolder = Path.Combine(templateFolder, "basic");
		var linqpadFolder = Path.Combine(templateFolder, "linqpad");
		
		var specificFolder = linqpad switch
		{
			false => basicFolder,
			true => linqpadFolder
		};
		
		TemplateUtils.Apply(commonFolder, nfo.ProjFolder);
		TemplateUtils.Apply(specificFolder, nfo.ProjFolder);
		
		Util.Cmd("npm", "init --yes");
		Util.Cmd("npm", "install --save-dev gulp browser-sync");
		
		Json.Mod(nfo.PackageFile, mod =>
		{
			mod.scripts.RemoveAll();
			mod.scripts.Add("start", JToken.Parse("\"gulp serve\""));
		});
		
		File.WriteAllText(nfo.RunFile, "npm run start");

		$"""
		\nInstructions:
		  cd {nfo.Name}
		  run.bat
		""".Dump();
	}
}
