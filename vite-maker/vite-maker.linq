<Query Kind="Program">
  <NuGetReference>RestSharp.Serializers.NewtonsoftJson</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#load "..\libs-lowlevel\basic"
#load "..\libs-lowlevel\css"
#load "..\libs-lowlevel\console"
#load "..\libs\files"

public static readonly string TemplateFolder = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "templates");

void Main()
{
	Con.UI.Dump();
	var nfo = new MakeNfo(@"C:\Dev", "MovieSurfer", 6230, 6240);
	ViteUtils.Make(nfo);
}


public static class ViteUtils
{
	private static readonly string[] libs =
	{
		"axios",
		"mobx",
		"mobx-react-lite",
		"react-toastify",
		"classnames",
		"@fortawesome/fontawesome-free",
		//"halfmoon",
		//"ion-rangeslider", "jquery",
	};
	private static readonly string[] devLibs =
	{
		"sass",
		"vite-tsconfig-paths",
		//"@types/halfmoon",
		//"@types/ion-rangeslider", "@types/jquery",
	};
	public static void Make(MakeNfo nfo)
	{
		FolderUtils.CleanFolders(nfo);
		Dotnet.CreateSln(nfo.SlnFolder, nfo.Name);
		MakeBackend(nfo);
		MakeFrontend(nfo);
		ApplyTemplates(nfo);
	}

	private static void MakeBackend(MakeNfo nfo)
	{
		Dotnet.CreateWebapiProj(nfo.WebsiteFolder, "backend");
		Dotnet.AddProjToSln(nfo.SlnFolder, nfo.WebsiteBackendProjFile, "Website");
		nfo.WebsiteBackendLaunchSettingsFile.ChangeJson(e =>
		{
			e["profiles"]["backend"]["launchBrowser"] = false;
			e["profiles"]["backend"]["launchUrl"] = "";
			e["profiles"]["backend"]["applicationUrl"] = $"http://localhost:{nfo.BackendPort}";
		});
	}

	private static void MakeFrontend(MakeNfo nfo)
	{
		Npm.CreateProject(nfo.WebsiteFolder, "frontend");
		var folder = nfo.WebsiteFrontendFolder;
		Npm.InstallInit(folder);
		Npm.Install(folder, false, libs);
		Npm.Install(folder, true, devLibs);
		
		nfo.WebsiteFrontendPackageJsonFile.ChangeJson(e =>
		{
			e["scripts"]["dev"] = "vite --host";
		});

		nfo.WebsiteFrontendTsConfigFile.ChangeJson(e =>
		{
			e["compilerOptions"]["baseUrl"] = "src";
			e["compilerOptions"]["experimentalDecorators"] = true;
		});
		
		File.WriteAllText(nfo.WebsiteFrontendRunBatFile, "npm run dev");
	}
	
	private static void ApplyTemplates(MakeNfo nfo)
	{
		var root = TemplateFolder;
		void Recurse(string folder)
		{
			var (files, filesDel, dirs, dirsDel) = GetFilesFilesDelDirsDirsDel(folder);
			foreach (var fileDel in filesDel)
			{
				var fileDelDst = fileDel.TransposeTo(root, nfo.SlnFolder).RemoveDel();
				if (File.Exists(fileDelDst))
					File.Delete(fileDelDst);
			}
			foreach (var dirDel in dirsDel)
			{
				var dirDelDst = dirDel.TransposeTo(root, nfo.SlnFolder).RemoveDel();
				if (Directory.Exists(dirDelDst))
					Directory.Delete(dirDelDst, true);
			}
			
			foreach (var file in files)
			{
				var fileDst = file.TransposeTo(root, nfo.SlnFolder);
				var str = File.ReadAllText(file);
				void Repl<T>(string varName, T varVal) => str = str.Replace($"%{varName}%", $"{varVal}");
				Repl("FrontendPort", nfo.FrontendPort);
				Repl("BackendPort", nfo.BackendPort);
				File.WriteAllText(fileDst, str);
			}

			foreach (var dir in dirs)
			{
				var dirDst = dir.TransposeTo(root, nfo.SlnFolder);
				if (!Directory.Exists(dirDst))
					Directory.CreateDirectory(dirDst);
				Recurse(dir);
			}
		}
		Recurse(root);
	}
	
	private static string TransposeTo(this string file, string rootPrev, string rootNext)
	{
		if (!file.StartsWith(rootPrev)) throw new ArgumentException();
		var s = file[rootPrev.Length..];
		return $"{rootNext}{s}";
	}
	
	private static (string[], string[], string[], string[]) GetFilesFilesDelDirsDirsDel(string folder)
	{
		return (
			Directory.GetFiles(folder).Where(e => !IsDel(e)).ToArray(),
			Directory.GetFiles(folder).Where(e => IsDel(e)).ToArray(),
			Directory.GetDirectories(folder).Where(e => !IsDel(e)).ToArray(),
			Directory.GetDirectories(folder).Where(e => IsDel(e)).ToArray()
		);
	}

	private static string RemoveDel(this string s)
	{
		if (!IsDel(s)) throw new ArgumentException();
		return Path.Combine(Path.GetDirectoryName(s)!, $"{Path.GetFileNameWithoutExtension(s)[..^4]}{Path.GetExtension(s)}");
	}
	private static bool IsDel(string s) => Path.GetFileNameWithoutExtension(s).EndsWith("-DEL");
}





public record MakeNfo(
	string ParentFolder,
	string Name,
	int BackendPort,
	int FrontendPort
)
{
	public string SlnFolder => Path.Combine(ParentFolder, Name);
	public string SlnFile => Path.Combine(SlnFolder, $"{Name}.sln");

	public string WebsiteFolder => Path.Combine(SlnFolder, "Website");
	public string WebsiteBackendFolder => Path.Combine(WebsiteFolder, "backend");
	public string WebsiteFrontendFolder => Path.Combine(WebsiteFolder, "frontend");
	public string WebsiteFrontendSrcFolder => Path.Combine(WebsiteFrontendFolder, "src");
	public string WebsiteFrontendSrcAssetsFolder => Path.Combine(WebsiteFrontendSrcFolder, "assets");

	public string WebsiteBackendProjFile => Path.Combine(WebsiteBackendFolder, "backend.csproj");
	public string WebsiteBackendLaunchSettingsFile => Path.Combine(WebsiteBackendFolder, "Properties", "launchSettings.json");

	public string WebsiteFrontendPackageJsonFile => Path.Combine(WebsiteFrontendFolder, "package.json");
	public string WebsiteFrontendViteConfigTsFile => Path.Combine(WebsiteFrontendFolder, "vite.config.ts");
	public string WebsiteFrontendTsConfigFile => Path.Combine(WebsiteFrontendFolder, "tsconfig.json");
	public string WebsiteFrontendRunBatFile => Path.Combine(WebsiteFrontendFolder, "run.bat");

	public void CreateFolders()
	{
		SlnFolder.CreateFolderIFN();
		WebsiteFolder.CreateFolderIFN();
		WebsiteBackendFolder.CreateFolderIFN();
		WebsiteFrontendFolder.CreateFolderIFN();
	}
}




static class FolderUtils
{
	public static void CleanFolders(MakeNfo nfo)
	{
		Files.EmptyFolder(nfo.SlnFolder.CreateFolderIFN());
		nfo.CreateFolders();
	}

	private static void CheckPrevMatches(string fileDst, string fileSrcPrev)
	{
		if (File.Exists(fileSrcPrev))
		{
			if (!File.Exists(fileDst)) throw new ArgumentException("File doesn't match prev template (1)");
			var strSrcPrev = File.ReadAllText(fileSrcPrev);
			var strDst = File.ReadAllText(fileDst);
			if (strSrcPrev != strDst) throw new ArgumentException("File doesn't match prev template (2)");
		}
		else
		{
			if (File.Exists(fileDst)) throw new ArgumentException("File doesn't match prev template (3)");
		}
	}

	private static string AddSuffix(this string file, string suffix) =>
		Path.Combine(Path.GetDirectoryName(file)!, $"{Path.GetFileNameWithoutExtension(file)}{suffix}{Path.GetExtension(file)}");
}

public static class Dotnet
{
	public static void CreateSln(string folder, string name) =>
		Con.Run("dotnet", folder,
			"new",
			"sln",
			"--name", name
		);

	public static void CreateWebapiProj(string websiteFolder, string projName) =>
		Con.Run("dotnet", websiteFolder,
			"new",
			"webapi",
			"--no-https",
			"--use-minimal-apis",
			"--no-openapi",
			"--name", projName
		);
	
	public static void AddProjToSln(string slnFolder, string projFile, string solutionFolder) =>
		Con.Run("dotnet", slnFolder,
			"sln",
			"add",
			"--solution-folder", solutionFolder,
			projFile
		);
}

public static class Npm
{
	public static void CreateProject(string websiteFolder, string name) =>
		Con.Run("npm", websiteFolder,
			"create",
			"vite@latest",
			name, "--", "--template", "react-ts"
		);

	public static void InstallInit(string frontendFolder) =>
		Con.Run("npm", frontendFolder,
			"install"
		);

	public static void Install(string frontendFolder, bool dev, params string[] libs) =>
		Con.Run("npm", frontendFolder,
			A(
				"install"
			)
				.AddIf(dev, "--save-dev")
				.AddArgs(libs)
		);

	private static string[] A(params string[] arr) => arr;
	private static string[] AddIf(this string[] arr, bool condition, string val) => condition switch
	{
		true => arr.Concat(new[] { val }).ToArray(),
		false => arr,
	};
	private static string[] AddArgs(this string[] arr, params string[] args) =>
		arr.Concat(args).ToArray();
}



public static class FilesUtils
{
	public static string MakeRel(this string file, MakeNfo nfo)
	{
		var prefix = $@"{nfo.ParentFolder}\";
		return file.StartsWith(prefix) switch
		{
			true => file[prefix.Length..],
			false => file
		};
	}
	
	public static void ChangeJson(this string file, Action<dynamic> action)
	{
		dynamic obj = LoadJson(file);
		action(obj);
		SaveJson(file, obj);
	}

	private static dynamic LoadJson(string file) => JsonConvert.DeserializeObject(File.ReadAllText(file));
	private static void SaveJson(string file, dynamic obj) => File.WriteAllText(file, JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
	
	public static string CreateFolderIFN(this string folder)
	{
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		return folder;
	}
}
