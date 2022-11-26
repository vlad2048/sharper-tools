<Query Kind="Program">
  <NuGetReference>CliFx</NuGetReference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>CliFx</Namespace>
  <Namespace>CliFx.Attributes</Namespace>
  <Namespace>CliFx.Infrastructure</Namespace>
</Query>

#load ".\_libs\base"
#load ".\templates\HtmlWatch"


async Task<int> Main(string[] args)
{
	return await new CliApplicationBuilder()
		.AddCommandsFromThisAssembly()
		.Build()
		.RunAsync(args);
}


/*
record Args(ITemplate Template, ProjNfo ProjNfo);

bool RetrieveArgs(string[] args, out Args? data)
{
	var templates = TemplateUtils.LoadTemplates();
	bool ShowUsage()
	{
		"Usage: MkProj.exe [TemplateName] [ProjectName]".Dump();
		$"Templates: {string.Join(",", templates.Select(e => e.Name))}".Dump();
		return false;
	}
	data = null;
	if (args.Length != 2) return ShowUsage();
	var templateName = args[0];
	var projName = args[1];
	var template = templates.FirstOrDefault(e => string.Compare(e.Name, templateName, StringComparison.InvariantCultureIgnoreCase) == 0);
	if (template == null) return ShowUsage();
	var folder = Directory.GetCurrentDirectory();
	var projNfo = new ProjNfo(folder, projName);
	if (Directory.Exists(projNfo.ProjFolder))
	{
		$"Folder {projNfo.ProjFolder} already exists".Dump();
		return false;
	}
	data = new Args(template, projNfo);
	$"Template: {data.Template.Name}".Dump();
	$"Folder  : {data.ProjNfo.RootFolder}".Dump();
	$"Name    : {data.ProjNfo.Name}".Dump();
	return true;
}
*/
