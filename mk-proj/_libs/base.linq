<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
</Query>

void Main()
{
	var fileIn = @"C:\Dev\sharper-tools\_infos\design\dlg\package.json";
	var fileOut = @"C:\Dev\sharper-tools\_infos\design\dlg\package_out.json";
	
	Json.Mod(fileIn, mod =>
	{
		mod.scripts.RemoveAll();
		mod.scripts.Add("run", JToken.Parse("\"here\""));
	}, fileOut);
}


public static class TemplateUtils
{
	private const string TemplateFilesFolderName = "template-files";
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

	public static string GetTemplateFolder(string name)
	{
		var folder = Path.Combine(TemplateRoot, name);
		if (!Directory.Exists(folder)) throw new ArgumentException($"Cannot find folder {folder}");
		return folder;
	}
	
	public static void Apply(string templateFolder, string targetFolder)
	{
		void Recurse(string templateDir)
		{
			var files = Directory.GetFiles(templateDir);
			foreach (var file in files)
			{
				var fileDst = file.Replace(templateFolder, targetFolder);
				var folderDst = Path.GetDirectoryName(fileDst)!;
				if (!Directory.Exists(folderDst))
					Directory.CreateDirectory(folderDst);
				File.Copy(file, fileDst);
			}
			var subDirs = Directory.GetDirectories(templateDir);
			foreach (var subDir in subDirs)
				Recurse(subDir);
		}
		Recurse(templateFolder);
	}
}

public static class Json
{
	public static void Mod(string fileIn, Action<dynamic> action, string? fileOut = null)
	{
		fileOut ??= fileIn;
		var cfg = Load(fileIn);
		action(cfg);
		Save(fileIn, fileOut, cfg);
	}
	
	public static T ModGet<T>(string file, Func<dynamic, T> fun)
	{
		var cfg = Load(file);
		var res = fun(cfg);
		return res;
	}
	
	private static dynamic Load(string file) => JsonConvert.DeserializeObject(File.ReadAllText(file), jsonOpt)!;
	private static void Save(string fileIn, string fileOut, dynamic cfg)
	{
		var strPrev = File.ReadAllText(fileIn);
		var strNext = JsonConvert.SerializeObject(cfg, jsonOpt);
		if (strNext != strPrev) File.WriteAllText(fileOut, strNext);
	}
	private static readonly JsonSerializerSettings jsonOpt = new()
	{
		Converters = new[] { new ExpandoObjectConverter() },
		Formatting = Newtonsoft.Json.Formatting.Indented,
	};
}

public static class FileUtils
{
	public static void DeleteFolder(string folder)
	{
		if (!Directory.Exists(folder))
			return;
		Directory.Delete(folder, true);
	}
	
	public static void EmptyFolder(string folder)
	{
		if (!Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
			return;
		}
		var files = Directory.GetFiles(folder);
		var dirs = Directory.GetDirectories(folder);
		foreach (var file in files) File.Delete(file);
		foreach (var dir in dirs) Directory.Delete(dir, true);
	}
	
	public static string CreateFolderIFN(this string folder)
	{
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		return folder;
	}
}

/*
public interface ITemplate
{
	string Name { get; }
	void Make(ProjNfo nfo);
}

public static class TemplateUtils
{
	public static ITemplate[] LoadTemplates() => (
		from type in Assembly.GetCallingAssembly().GetTypes()
		where typeof(ITemplate).IsAssignableFrom(type) && !type.IsInterface
		select Activator.CreateInstance(type) as ITemplate
	).ToArray();
}
*/
