<Query Kind="Program">
  <NuGetReference>LINQPadExtras</NuGetReference>
  <NuGetReference>CliWrap</NuGetReference>
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <Namespace>CliWrap</Namespace>
  <Namespace>CliWrap.Buffered</Namespace>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>LINQPadExtras.Scripting_LockChecker</Namespace>
  <Namespace>LINQPadExtras.Scripting_Batcher</Namespace>
</Query>

#load "..\cfg"
#load ".\api-common"

void Main()
{
	Util.HorizontalRun(true,
		ApiNuget.GetVers(NugetSource.Local, "PowRxVar"),
		ApiNuget.GetVers(NugetSource.Remote, "PowRxVar")
	).Dump();
}


public enum NugetSource
{
	Local,
	Remote
}

public static class ApiNuget
{
	public static string[] GetVers(NugetSource src, string pkgId) => NugetLogic.Get(src).GetVers(pkgId);
	public static DateTime? GetVerTimestamp(NugetSource src, string pkgId, string pkgVer) => NugetLogic.Get(src).GetVerTimestamp(pkgId, pkgVer);
	public static IPackageSearchMetadata[] Search(NugetSource src, string str) => NugetLogic.Get(src).Search(str);
	
	public static void Release(NugetSource src, PrjNfo prj, string pkgVer, bool skipBuild, bool dryRun)
	{
		if (!CheckLocks(prj, pkgVer, dryRun)) return;
		
		Batcher.Run(
			$"Releasing {prj.Name} to {src}",
			cmd =>
			{
				Release(cmd, src, prj, pkgVer, skipBuild, dryRun);
			},
			opt =>
			{
				opt.DryRun = dryRun;
			}
		);
	}
	
	public static void Release(ICmd cmd, NugetSource src, PrjNfo prj, string pkgVer, bool skipBuild, bool dryRun)
	{
		var isRemote = src == NugetSource.Remote;
		
		var packedPrj = cmd.Pack(prj.Sln.Folder, prj, pkgVer, skipBuild, dryRun);
		cmd.ReleaseToFolder(packedPrj, Cfg.Nuget.LocalPackageFolder, false);
		cmd.ReleaseToFolder(packedPrj, Cfg.Nuget.GlobalPackageFolder, true);

		if (isRemote)
		{
			cmd.Run("nuget",
				"push",
				packedPrj.PkgFile,
				"-source",
				Cfg.Nuget.RemoteRepoUrl,
				"-apikey",
				Cfg.Nuget.ApiKey
			);
			cmd.AddArtifact(prj.NugetUrl);
		}

		cmd.DeleteFile(packedPrj.PkgFile);
	}


	private record PackedPrj(PrjNfo Prj, string PkgFile)
	{
		private string BaseFile => Path.GetFileNameWithoutExtension(PkgFile);
		public string PkgFolder => Path.GetDirectoryName(PkgFile)!;
		public string Version => BaseFile.Extract(@"(?<=\.)\d+\.\d+\.\d+.*");
		public string ProjName => BaseFile.Extract(@".*(?=\.\d\.\d\.\d)");
		public string ProjNameLower => ProjName.ToLowerInvariant();
	}


	private static PackedPrj Pack(this ICmd cmd, string slnFolder, PrjNfo prj, string ver, bool skipBuild, bool dryRun)
	{
		//	Alternatives:
		//	
		//	nuget pack -version 0.0.2
		//	dotnet pack /p:version=0.0.2
		//	dotnet pack -p:packageversion=0.0.2
		//	dotnet pack -property:version=0.0.2
		//	dotnet pack -p:version=0.0.2				(the one we use)
		//	
		//	note:
		//		- dotnet puts the .pkg file in [Project]\bin\Debug
		//		- nuget  puts the .pkg file in [Project]\
		cmd.Cd(prj.Folder);

		cmd.Run("dotnet",
			new[]
			{
				"pack",
				$"-p:version={ver}",
				$"-p:SolutionDir=\"{slnFolder}\""
			}.AddIf(skipBuild, "--no-build")
		);
		var pkgFile = Path.Combine(prj.Folder, "bin", "Debug", $"{prj.Name}.{ver}.nupkg");
		if (!dryRun && !File.Exists(pkgFile)) throw new ArgumentException($"Did not find the packed file @ '{pkgFile}'");
		return new PackedPrj(prj, pkgFile);
	}
	
	private static string[] AddIf(this string[] arr, bool condition, string extra) => condition switch
	{
		false => arr,
		true => arr.Concat(new[] { extra }).ToArray()
	};


	private static void ReleaseToFolder(this ICmd cmd, PackedPrj packedPrj, string packageFolder, bool expand)
	{
		var folder = MkReleaseFolder(packedPrj.Prj, packedPrj.Version, packageFolder);
		cmd.DeleteFolder(folder);

		var args = new List<string>
		{
				"add",
				packedPrj.PkgFile,
				"-source",
				packageFolder
		};
		if (expand)
			args.Add("-expand");
		cmd.Run("nuget", args.ToArray());
		cmd.AddArtifact(folder);
	}




	private static string MkReleaseFolder(PrjNfo prj, string version, string packageFolder) => Path.Combine(packageFolder, prj.NameLower, version);

	private static bool CheckLocks(PrjNfo prj, string ver, bool dryRun)
	{
		if (dryRun) return true;
		return LockChecker.CheckFolders(
			MkReleaseFolder(prj, ver, Cfg.Nuget.LocalPackageFolder),
			MkReleaseFolder(prj, ver, Cfg.Nuget.GlobalPackageFolder)
		);
	}

	private static string Extract(this string str, string regexStr)
	{
		var regex = new Regex(regexStr);
		if (!regex.IsMatch(str)) throw new ArgumentException($"Failed to extract regex:'{regexStr}' from string:'{str}'");
		var match = regex.Match(str);
		return match.Captures[0].Value;
	}
}

internal static class NugetLogic
{
	public static Nuget Get(NugetSource src) => nugetMap[src].Value;
	
	private static readonly Dictionary<NugetSource, Lazy<Nuget>> nugetMap = new()
	{
		{ NugetSource.Local, new Lazy<Nuget>(() => new Nuget(Cfg.Nuget.LocalPackageFolder)) },
		{ NugetSource.Remote, new Lazy<Nuget>(() => new Nuget(Cfg.Nuget.RemoteRepoUrl)) },
	};
	
	internal class Nuget
	{
		private readonly ILogger logger = NullLogger.Instance;
		private readonly IEnumerable<Lazy<INuGetResourceProvider>> providers;
		private readonly PackageSource pkgSource;
		private readonly SourceRepository repo;
		private readonly SourceCacheContext cache = new();
		private readonly FindPackageByIdResource findLogic;
		private readonly PackageMetadataResource metadataLogic;
		private readonly PackageSearchResource searchLogic;

		public Nuget(string pkgSourceLocation)
		{
			providers = Repository.Provider.GetCoreV3();
			pkgSource = new PackageSource(pkgSourceLocation);
			repo = new SourceRepository(pkgSource, providers);
			findLogic = repo.GetResource<FindPackageByIdResource>();
			metadataLogic = repo.GetResource<PackageMetadataResource>();
			searchLogic = repo.GetResource<PackageSearchResource>();
		}

		public string[] GetVers(string pkgId) =>
			findLogic.GetAllVersionsAsync(pkgId, cache, logger, CancellationToken.None).Result
				.Where(e => !e.IsPrerelease)
				.OrderByDescending(e => e)
				.Select(e => $"{e}")
				.ToArray();

		public DateTime? GetVerTimestamp(string pkgId, string pkgVer)
		{
			var pkg = new PackageIdentity(pkgId, NuGetVersion.Parse(pkgVer));
			var meta = metadataLogic.GetMetadataAsync(pkg, cache, logger, CancellationToken.None).Result;
			return meta?.Published?.DateTime;
		}
		
		public IPackageSearchMetadata[] Search(string str) => searchLogic.SearchAsync(
			str,
			new SearchFilter(
				includePrerelease: true,
				filter: null
			)
			{
				IncludeDelisted = true,
			},
			0, 100,
			logger,
			CancellationToken.None
		).Result.ToArray();
	}



	//public class Logger : ILogger
	//{
	//	private void L(LogLevel level, string s, string? extra = null) => $"[{level}{FmtExtra(extra)}] {s}".Dump();
	//	private string FmtExtra(string? extra) => extra switch
	//	{
	//		null => string.Empty,
	//		not null => $"-{extra}"
	//	};
	//
	//	public void LogDebug(string data) => L(LogLevel.Debug, data);
	//	public void LogVerbose(string data) => L(LogLevel.Verbose, data);
	//	public void LogInformation(string data) => L(LogLevel.Information, data);
	//	public void LogMinimal(string data) => L(LogLevel.Minimal, data);
	//	public void LogWarning(string data) => L(LogLevel.Warning, data);
	//	public void LogError(string data) => L(LogLevel.Error, data);
	//	public void LogInformationSummary(string data) => L(LogLevel.Information, data, "summary");
	//
	//	public void Log(LogLevel level, string data) => L(level, data, "l0");
	//	public async Task LogAsync(LogLevel level, string data) => L(level, data, "l1");
	//	public void Log(ILogMessage msg) => L(msg.Level, msg.Message, "l2");
	//	public async Task LogAsync(ILogMessage msg) => L(msg.Level, msg.Message, "l3");
	//}
}




/*
public static class ApiNuget
{
	public static string? GetLatestVer(string name) => GetVers(name).FirstOrDefault();
	
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

	public static string? GetLastRemoteVer(PrjNfo prj) => GetVers(prj.Name).FirstOrDefault();




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
		//	Alternatives:
		//	
		//	nuget pack -version 0.0.2
		//	dotnet pack /p:version=0.0.2
		//	dotnet pack -p:packageversion=0.0.2
		//	dotnet pack -property:version=0.0.2
		//	dotnet pack -p:version=0.0.2				(the one we use)
		//	
		//	note:
		//		- dotnet puts the .pkg file in [Project]\bin\Debug
		//		- nuget  puts the .pkg file in [Project]\
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
		return list
			.OrderByDescending(e => NuGetVersion.Parse(e))
			.ToArray();
	}


	//private class MyLogger : LoggerBase
	//{
	//	public override void Log(ILogMessage msg) => L($"{msg.Level} {msg.Time} {msg.Message}");
	//	public override Task LogAsync(ILogMessage msg) => throw new NotImplementedException();
	//	private static void L(string s) => s.Dump();
	//}

	private static string Extract(this string str, string regexStr)
	{
		var regex = new Regex(regexStr);
		if (!regex.IsMatch(str)) throw new ArgumentException($"Failed to extract regex:'{regexStr}' from string:'{str}'");
		var match = regex.Match(str);
		return match.Captures[0].Value;
	}
}
*/