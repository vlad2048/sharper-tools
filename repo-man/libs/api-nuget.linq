<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>CliWrap</NuGetReference>
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <Namespace>CliWrap</Namespace>
  <Namespace>CliWrap.Buffered</Namespace>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

#load "..\cfg"
#load ".\api-common"

void Main()
{
	var p = "PowBasics";
	var prj = new PrjNfo(@"C:\Dev_Nuget\Libs\PowBasics\Libs\PowBasics\PowBasics.csproj");
	ApiNuget.GetLastRemoteVer(prj).Dump();
	//ApiNuget.DoesPkgExist(p).Dump();
	//ApiNuget.GetRemoteVers(p).Dump();
	
	//ApiNuget.ReleaseLocally(new PrjNfo(@"C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\LINQPadExtras.csproj"), "0.0.3");
	//ApiNuget.GetRemoteVers("LINQPadExtras");
}


public static class ApiNuget
{
	public static string[] GetRemoteVers(string name)
	{
		//GetVers(name);
		var mets = Nuget.Search.GetMetadataAsync(name, true, true, Nuget.Cache, Nuget.Logger, CancellationToken.None).Result.ToArray();
		mets.Select(e => e.Identity.Version).Dump();
		
		var arr = Nuget.Finder.GetAllVersionsAsync(name, Nuget.Cache, Nuget.Logger, CancellationToken.None).Result.ToArray();
		arr.Dump();
		return arr.Select(e => $"{e}").ToArray();		
	}
	
	
	
	
	public static bool DoesPkgExist(string name) => GetVers(name).Any();
	
	public static bool DoesPkgVerExist(string name, string ver) => GetVers(name).Contains(ver);

	public static void ReleaseLocally(string slnFolder, PrjNfo prj, string ver, bool dryRun)
	{
		Con.Start("Releasing ", prj.Name, " locally", dryRun);
		if (CheckLocks(prj, ver, dryRun)) return;
		var packedPrj = Pack(slnFolder, prj, ver, dryRun);
		Con.AddArtifact(ReleaseToFolder(packedPrj, Cfg.Nuget.LocalPackageFolder, false));
		Con.AddArtifact(ReleaseToFolder(packedPrj, GlobalPackagesFolder, true));
		Con.DeleteFile(packedPrj.PkgFile);
		Con.EndSuccess();
	}

	public static void ReleaseToNuget(string slnFolder, PrjNfo prj, string ver, string nugetUrlForLog, bool dryRun)
	{
		Con.Start("Releasing ", prj.Name, " to Nuget", dryRun);
		if (CheckLocks(prj, ver, dryRun)) return;
		var packedPrj = Pack(slnFolder, prj, ver, dryRun);

		Con.Run("nuget",
			"push",
			packedPrj.PkgFile,
			"-source",
			Cfg.Nuget.RemoteRepoUrl,
			"-apikey",
			Cfg.Nuget.ApiKey
		);

		Con.AddArtifact(ReleaseToFolder(packedPrj, Cfg.Nuget.LocalPackageFolder, false));
		Con.AddArtifact(ReleaseToFolder(packedPrj, GlobalPackagesFolder, true));

		Con.DeleteFile(packedPrj.PkgFile);

		verMap[packedPrj.ProjName].Add(ver);
		Con.AddArtifact(nugetUrlForLog);
		Con.EndSuccess();
	}

	public static DateTime? GetLocalVerTime(PrjNfo prj, string ver)
	{
		var folder = Path.Combine(Cfg.Nuget.LocalPackageFolder, prj.Name, ver);
		return Directory.Exists(folder) switch
		{
			false => null,
			true => ApiCommon.GetFolderLastTimestamp(folder)
		};
	}
	
	public static DateTime? GetRemoteVerTime(PrjNfo prj, string ver)
	{
		var pkgId = new PackageIdentity(prj.Name, NuGetVersion.Parse(ver));
		var meta = Nuget.Search.GetMetadataAsync(pkgId, Nuget.Cache, Nuget.Logger, CancellationToken.None).Result;
		if (meta == null) return null;
		return meta.Published!.Value.DateTime;
	}

	public static string? GetLastLocalVer(PrjNfo prj)
	{
		var folder = Path.Combine(Cfg.Nuget.LocalPackageFolder, prj.Name);
		if (!Directory.Exists(folder)) return null;
		return Directory.GetDirectories(folder)
			.Select(Path.GetFileName)
			.Where(e => NuGetVersion.TryParse(e, out _))
			.Select(NuGetVersion.Parse)
			.OrderByDescending(e => e)
			.Select(e => $"{e}")
			.FirstOrDefault();
	}

	public static string? GetLastRemoteVer(PrjNfo prj)
	{
		var vers = GetVers(prj.Name);
		return vers.OrderByDescending(e => NuGetVersion.Parse(e)).FirstOrDefault();
	}




	// ***********
	// * Private *
	// ***********
	private static string MkReleaseFolder(PrjNfo prj, string version, string packageFolder) => Path.Combine(packageFolder, prj.NameLower, version);

	private static bool CheckLocks(PrjNfo prj, string ver, bool dryRun)
	{
		if (dryRun) return false;
		if (Con.CheckForFolderLocks(
			MkReleaseFolder(prj, ver, Cfg.Nuget.LocalPackageFolder),
			MkReleaseFolder(prj, ver, GlobalPackagesFolder)
		))
		{
			Con.EndCancel();
			return true;
		}
		return false;
	}
	

	private record NugetNfo(
		ILogger Logger,
		SourceCacheContext Cache,
		SourceRepository Repo,
		FindPackageByIdResource Finder,
		PackageMetadataResource Search
	);
	private static readonly Lazy<NugetNfo> nuget = new(() =>
	{
		var repo = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3(Cfg.Nuget.RemoteRepoUrl);
		var finder = repo.GetResource<FindPackageByIdResource>();
		var search = repo.GetResource<PackageMetadataResource>();
		return new NugetNfo(
			NullLogger.Instance,
			new SourceCacheContext(),
			repo,
			finder,
			search
		);
	});
	private static NugetNfo Nuget => nuget.Value;

	private static readonly Lazy<string> globalPackagesFolder = new(() =>
	{
		var res = Cli.Wrap("nuget")
			.WithArguments(new[]
			{
				"locals",
				"global-packages",
				"-list"
			})
			.ExecuteBufferedAsync()
			.GetAwaiter().GetResult();
		var folder = res.StandardOutput.Extract("(?<=global-packages: ).*").Trim().TrimEnd(Path.DirectorySeparatorChar);
		return folder;
	});
	
	
	private static string GlobalPackagesFolder => globalPackagesFolder.Value;

	private record PackedPrj(PrjNfo Prj, string PkgFile)
	{
		private string BaseFile => Path.GetFileNameWithoutExtension(PkgFile);
		public string PkgFolder => Path.GetDirectoryName(PkgFile)!;
		public string Version => BaseFile.Extract(@"(?<=\.)\d+\.\d+\.\d+.*");
		public string ProjName => BaseFile.Extract(@".*(?=\.\d\.\d\.\d)");
		public string ProjNameLower => ProjName.ToLowerInvariant();
	}

	private static readonly Dictionary<string, List<string>> verMap = new();

	private static string ReleaseToFolder(PackedPrj packedPrj, string packageFolder, bool expand)
	{
		var folder = MkReleaseFolder(packedPrj.Prj, packedPrj.Version, packageFolder);
		Con.DeleteFolder(folder);

		var args = new List<string>
		{
				"add",
				packedPrj.PkgFile,
				"-source",
				packageFolder
		};
		if (expand)
			args.Add("-expand");
		Con.Run("nuget", args.ToArray());
		return folder;
	}

	private static PackedPrj Pack(string slnFolder, PrjNfo prj, string ver, bool dryRun)
	{
		/*
			Alternatives:
			
			nuget pack -version 0.0.2
			dotnet pack /p:version=0.0.2
			dotnet pack -p:packageversion=0.0.2
			dotnet pack -property:version=0.0.2
			dotnet pack -p:version=0.0.2				(the one we use)
			
			note:
				- dotnet puts the .pkg file in [Project]\bin\Debug
				- nuget  puts the .pkg file in [Project]\
		*/
		/*var cmdOut = Con.RunIn("dotnet", prj.Folder,
			"pack",
			$"-p:version={ver}"
		);
		var pkgFile = cmdOut.Extract("(?<=Successfully created package ').*(?=')");*/
		Con.RunIn("dotnet", prj.Folder,
			"pack",
			$"-p:version={ver}",
			$"-p:SolutionDir=\"{slnFolder}\""
		);
		var pkgFile = Path.Combine(prj.Folder, "bin", "Debug", $"{prj.Name}.{ver}.nupkg");
		if (!dryRun && !File.Exists(pkgFile)) throw new ArgumentException($"Did not find the packed file @ '{pkgFile}'");
		return new PackedPrj(prj, pkgFile);
	}


	private static string[] GetVers(string name)
	{
		if (!verMap.TryGetValue(name, out var list))
			list = verMap[name] = Nuget.Finder.GetAllVersionsAsync(name, Nuget.Cache, Nuget.Logger, CancellationToken.None).Result.Select(e => $"{e}").ToList();
		return list.ToArray();
	}


	/*private class MyLogger : LoggerBase
	{
		public override void Log(ILogMessage msg) => L($"{msg.Level} {msg.Time} {msg.Message}");
		public override Task LogAsync(ILogMessage msg) => throw new NotImplementedException();
		private static void L(string s) => s.Dump();
	}*/

	private static string Extract(this string str, string regexStr)
	{
		var regex = new Regex(regexStr);
		if (!regex.IsMatch(str)) throw new ArgumentException($"Failed to extract regex:'{regexStr}' from string:'{str}'");
		var match = regex.Match(str);
		return match.Captures[0].Value;
	}
}