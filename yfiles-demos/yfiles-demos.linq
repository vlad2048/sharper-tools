<Query Kind="Program">
  <NuGetReference>CliWrap</NuGetReference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>CliWrap</Namespace>
</Query>

const string folder = @"C:\Dev_ExternalLibs\yFiles\samples-netcore\demos";

/*

Layout			->		LayoutStyles					best to explore layout algorithms
						MixedLayout						cool
						IncrementalHierarchicLayout		cool
						Multipage						cool
						NodeLabeling					nice placement of labels
						InteractiveOrganic				nice dynamic
						PortCandidate					great auto selection of ports
						Tree							multiple layouts for levels in a hierarchical tree
						LayerConstraints / SequenceConstraints		cool constraint system

DataBinding		->		GraphBuilder					nice grouped tree generation from code

View			->		Graph Viewer					graph minimap and node info pane

yEd.NET			->		yEd.NET							full graph editor + layout

*/

void Main()
{
	FileUtils.FindRecursively(folder, "*.exe")
		.Where(e => Path.GetFileName(e) != "apphost.exe")
		.Select(Exe.FromFile)
		.OrderBy(e => e.Name)
		.Select(exe =>
		{
			var runBtn = new Button("Run", _ =>
			{
				Cli.Wrap(exe.File)
					.WithWorkingDirectory(exe.Folder)
					.ExecuteAsync()
					.Task
					.Wait();
			});

			return new
			{
				Name = exe.Name,
				Cat = exe.Cat,
				Cmd = runBtn
			};
		})
		.GroupBy(e => e.Cat)
		.Dump();
}

record Exe(string File, string Cat)
{
	public string Folder => Path.GetDirectoryName(File)!;
	public string Name => Path.GetFileName(File).Replace("Demo.exe", "");
	
	public static Exe FromFile(string file) => new(
		file,
		GetFolderUp(file, 5)
	);
	
	private static string GetFolderUp(string file, int level)
	{
		for (var i = 0; i < level; i++)
			file = Path.GetDirectoryName(file)!;
		return Path.GetFileName(file);
	}
}

static class FileUtils
{
	public static string[] FindRecursively(string folder, string pattern)
	{
		var list = new List<string>();

		void Recurse(string curFolder)
		{
			list.AddRange(Directory.GetFiles(curFolder, pattern));
			var subFolders = Directory.GetDirectories(curFolder);
			foreach (var subFolder in subFolders)
				Recurse(subFolder);
		}

		Recurse(folder);
		return list.ToArray();
	}
}