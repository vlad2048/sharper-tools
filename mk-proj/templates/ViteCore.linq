<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>CliFx</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>CliFx</Namespace>
  <Namespace>CliFx.Attributes</Namespace>
  <Namespace>CliFx.Infrastructure</Namespace>
  <Namespace>LINQPadExtras.Scripting_Batcher</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Newtonsoft.Json</Namespace>
</Query>

#load "..\_libs\base"
#load "..\_libs\templater"

void Main()
{
	//TemplateUtils.GetTemplateFolder("ViteCore").Dump();
	var sln = new MakeNfo(
		@"C:\tmp\MkViter",
		"chump",
		6230,
		6240
	);
	FileUtils.DeleteFolder(sln.SlnFolder);
	ViteCoreCommand.Run(sln);
}


[Command("ViteCore")]
public class ViteCoreCommand : ICommand
{
	[CommandParameter(0)]
	public string Name { get; init; }

	public ValueTask ExecuteAsync(IConsole con)
	{
		var sln = new MakeNfo(Directory.GetCurrentDirectory(), Name, 6230, 6240);
		Run(sln);
		return default;
	}

	public static void Run(MakeNfo sln)
	{
		//Util.ReadLine();
		Console.WriteLine($"Name        : {sln.Name}");
		Console.WriteLine($"BackendPort : {sln.BackendPort}");
		Console.WriteLine($"FrontendPort: {sln.FrontendPort}");

		Batcher.Run(
			$"Making {sln.Name}",
			cmd =>
			{
				sln.CreateFolders();
				Dotnet.CreateSln(cmd, sln.SlnFolder, sln.Name);
				MakeBackend(cmd, sln);
				MakeFrontend(cmd, sln);
				var vars = new Dictionary<string, string>()
				{
					{ "BackendPort", $"{sln.BackendPort}" },
					{ "FrontendPort", $"{sln.FrontendPort}" },
				};
				Templater.Apply(sln.SlnFolder, vars, "ViteCore");
				
				StartBackendAndFrontend(cmd, sln);
			},
			opt =>
			{
				opt.CmdLine = IsCalledFromCmdLine();
			}
		);
	}
	
	private static void StartBackendAndFrontend(ICmd cmd, MakeNfo sln)
	{
		// wt --title Backend -d "C:\tmp\MkViter\chump\Website\backend" dotnet run ; new-tab -V -d "C:\tmp\MkViter\chump\Website\frontend" cmd /k run.bat
		cmd.Run("wt",
			"--title", "Backend",
			"-d", sln.WebsiteBackendFolder,
			"dotnet", "run",
			";",
			"new-tab", "-V",
			"-d", sln.WebsiteFrontendFolder,
			"cmd", "/k", "run.bat"
		);
	}
	
	private static readonly string[] libs =
	{
		"axios",
		"mobx",
		"mobx-react-lite",
		"react-toastify",
		"classnames",
		"@fortawesome/fontawesome-free",
	};
	
	private static readonly string[] devLibs =
	{
		"sass",
		"vite-tsconfig-paths",
	};
	
	
	
	private static void MakeBackend(ICmd cmd, MakeNfo nfo)
	{
		Dotnet.CreateWebapiProj(cmd, nfo.WebsiteFolder, "backend");
		Dotnet.AddProjToSln(cmd, nfo.SlnFolder, nfo.WebsiteBackendProjFile, "Website");
		nfo.WebsiteBackendLaunchSettingsFile.ChangeJson(e =>
		{
			e["profiles"]["http"]["launchBrowser"] = false;
			e["profiles"]["http"]["launchUrl"] = "";
			e["profiles"]["http"]["applicationUrl"] = $"http://localhost:{nfo.BackendPort}";
		});
	}

	private static void MakeFrontend(ICmd cmd, MakeNfo nfo)
	{
		Npm.CreateProject(cmd, nfo.WebsiteFolder, "frontend");
		var folder = nfo.WebsiteFrontendFolder;
		Npm.InstallInit(cmd, folder);
		Npm.Install(cmd, folder, false, libs);
		Npm.Install(cmd, folder, true, devLibs);
		
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
	

	private static bool IsCalledFromCmdLine()
	{
		#if CMD
			return true;
		#else
			return false;
		#endif
	}
}





static class Dotnet
{
	public static void CreateSln(ICmd cmd, string folder, string name)
	{
		cmd.Cd(folder);
		cmd.Run("dotnet",
			"new",
			"sln",
			"--name", name
		);
	}

	public static void CreateWebapiProj(ICmd cmd, string websiteFolder, string projName)
	{
		cmd.Cd(websiteFolder);
		cmd.Run("dotnet",
			"new",
			"webapi",
			"--no-https",
			"--use-minimal-apis",
			"--no-openapi",
			"--name", projName
		);
	}

	public static void AddProjToSln(ICmd cmd, string slnFolder, string projFile, string solutionFolder)
	{
		cmd.Cd(slnFolder);
		cmd.Run("dotnet",
			"sln",
			"add",
			"--solution-folder", solutionFolder,
			projFile
		);
	}
}


static class Npm
{
	public static void CreateProject(ICmd cmd, string websiteFolder, string name)
	{
		cmd.Cd(websiteFolder);
		cmd.Run("npm",
			"create",
			"vite@latest",
			name, "--", "--template", "react-ts"
		);
	}

	public static void InstallInit(ICmd cmd, string frontendFolder)
	{
		cmd.Cd(frontendFolder);
		cmd.Run("npm",
			"install"
		);
	}

	public static void Install(ICmd cmd, string frontendFolder, bool dev, params string[] libs)
	{
		cmd.Cd(frontendFolder);
		cmd.Run("npm",
			A(
				"install"
			)
				.AddIf(dev, "--save-dev")
				.AddArgs(libs)
		);
	}

	private static string[] A(params string[] arr) => arr;
	private static string[] AddIf(this string[] arr, bool condition, string val) => condition switch
	{
		true => arr.Concat(new[] { val }).ToArray(),
		false => arr,
	};
	private static string[] AddArgs(this string[] arr, params string[] args) =>
		arr.Concat(args).ToArray();
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
		FileUtils.EmptyFolder(
			SlnFolder.CreateFolderIFN()
		);
		WebsiteFolder.CreateFolderIFN();
		WebsiteBackendFolder.CreateFolderIFN();
		WebsiteFrontendFolder.CreateFolderIFN();
	}
}



static class FolderUtils
{
	public static void CleanFolders(MakeNfo nfo)
	{
		FileUtils.EmptyFolder(nfo.SlnFolder.CreateFolderIFN());
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


static class JsonUtils
{
	public static void ChangeJson(this string file, Action<dynamic> action)
	{
		dynamic obj = LoadJson(file);
		action(obj);
		SaveJson(file, obj);
	}

	private static dynamic LoadJson(string file) => JsonConvert.DeserializeObject(File.ReadAllText(file))!;
	private static void SaveJson(string file, dynamic obj) => File.WriteAllText(file, JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
}


