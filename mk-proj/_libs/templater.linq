<Query Kind="Program">
  <Namespace>System.Text.Json</Namespace>
</Query>

void Main()
{
	Templater.Apply(@"C:\tmp\MkViter\chump_work", null, "ViteCore");
}


public static class Templater
{
	public static void Apply(string targetFolder, Dictionary<string, string>? vars, params string[] src)
	{
		vars ??= new Dictionary<string, string>();
		var sourceFolder = GetTemplateFolder(src);
		var deletes = ReadDeletes(sourceFolder);
		ApplyDeletes(deletes, targetFolder);
		
		var deletesFile = Path.Combine(sourceFolder, DeletesFilename);
		void Recurse(string srcDir)
		{
			var srcFiles = Directory.GetFiles(srcDir).Where(e => e != deletesFile).ToArray();
			var srcSubDirs = Directory.GetDirectories(srcDir);
			foreach (var srcSubDir in srcSubDirs) Recurse(srcSubDir);
			foreach (var srcFile in srcFiles)
			{
				var dstFile = srcFile.TransposeTo(sourceFolder!, targetFolder).CreateFolderForFileIFN();
				CopyWithVars(srcFile, dstFile, vars);
			}
		}
		Recurse(sourceFolder);
	}
	
	private static void CopyWithVars(string srcFile, string dstFile, Dictionary<string, string> vars)
	{
		var str = File.ReadAllText(srcFile);
		foreach (var kv in vars)
			str = str.Replace($"%{kv.Key}%", kv.Value);
		File.WriteAllText(dstFile, str);
	}
	
	
	private static Deletes ReadDeletes(string sourceFolder)
	{
		var deletesFile = Path.Combine(sourceFolder, DeletesFilename);
		if (!File.Exists(deletesFile)) return Deletes.Empty;
		return JsonSerializer.Deserialize<Deletes>(File.ReadAllText(deletesFile))!;
	}
	private static void ApplyDeletes(Deletes deletes, string targetFolder)
	{
		var files = deletes.GetFiles(targetFolder);
		var folders = deletes.GetFolders(targetFolder);
		foreach (var file in files) File.Delete(file);
		foreach (var folder in folders) Directory.Delete(folder, true);
	}
	
	private record Deletes(string[] Files, string[] Folders)
	{
		public string[] GetFiles(string targetFolder) => Files.Select(e => Path.Combine(targetFolder, e.Replace("/", @"\"))).ToArray();
		public string[] GetFolders(string targetFolder) => Folders.Select(e => Path.Combine(targetFolder, e.Replace("/", @"\"))).ToArray();
		public static readonly Deletes Empty = new(Array.Empty<string>(), Array.Empty<string>());
	}
	
	private static string CreateFolderForFileIFN(this string file)
	{
		var folder = Path.GetDirectoryName(file)!;
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		return file;
	}
	
	private static string TransposeTo(this string file, string rootPrev, string rootNext)
	{
		if (!file.StartsWith(rootPrev)) throw new ArgumentException();
		var s = file[rootPrev.Length..];
		return $"{rootNext}{s}";
	}
	
	private const string TemplateFilesFolderName = "template-files";
	private const string DeletesFilename = "_deletes.json";
	private static readonly Lazy<string> templateRoot = new(() =>
	{
		var scriptFolder = Path.GetDirectoryName(Util.CurrentQueryPath)!;
		var root = Path.Combine(scriptFolder, TemplateFilesFolderName);
		if (Directory.Exists(root)) return root;
		root = Path.GetFullPath(Path.Combine(scriptFolder, "..", TemplateFilesFolderName));
		if (!Directory.Exists(root)) throw new ArgumentException($"Cannot find the {TemplateFilesFolderName} folder");
		return root;
	});
	private static string TemplateRoot => templateRoot.Value;
	private static string GetTemplateFolder(params string[] src) => Path.Combine(new[] { TemplateRoot }.Concat(src).ToArray());
}