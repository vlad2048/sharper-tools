<Query Kind="Program">
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>NuGet.Frameworks</Namespace>
  <Namespace>NuGet.Packaging</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

const string SrcLocal = @"C:\Dev_Nuget\packages";
const string SrcGlobal = @"C:\Users\vlad\.nuget\packages";
const string SrcRemote = @"https://api.nuget.org/v3/index.json";

static readonly NuGetFramework TargetFramework = NuGetFramework.Parse("net7.0");





void Main()
{
	ApiNuget.ListLibs().Dump();
	return;
	
	var search = ApiNuget.Get<PackageSearchResource>(SrcRemote);
	search.SearchAsync(
		"math",
		new SearchFilter(false, SearchFilterType.IsLatestVersion),
		0, 100, NullLogger.Instance, CancellationToken.None
	).Result
		.Select(pkg => new
		{
			Nfo = Util.VerticalRun(
				pkg.Title,
				pkg.ProjectUrl switch
				{
					not null => new Hyperlinq(pkg.ProjectUrl?.AbsoluteUri),
					null => (object)"_"
				},
				pkg.Tags
			),
			DownloadCount = pkg.DownloadCount,
			Description = pkg.Description,
			Summary = pkg.Summary,
		})
		.OrderByDescending(pkg => pkg.DownloadCount)
		.Dump();
}

public record Lib(
	string CapitalizedName,
	PackageIdentity Pkg,
	NuGetFramework Framework,
	string DllFile,
	long DllFileSize,
	DateTime Time,
	PackageDependency[] Deps
)
{
	public string Name => CapitalizedName;
	public string Version => $"{Pkg.Version}";
	public override string ToString() => Name;
}


public static class ApiNuget
{
	private static readonly ILogger logger = new Logger();
	private static readonly CancellationToken cancel = CancellationToken.None;
	
	public static Lib[] ListLibs()
	{
		var source = SrcGlobal;
		var finder = Get<FindLocalPackagesResource>(source);
		
		var arr = (
			from pkg in finder.GetPackages(logger, cancel)
			where !pkg.Identity.Version.IsPrerelease
			group pkg by pkg.Identity.Id into pkgNameGrp
			let lastPkg = pkgNameGrp.OrderByDescending(e => e.Identity.Version).First()
			let deps = lastPkg.Nuspec.GetDependencyGroups()
			where deps.Any(dep => AreFrameworksCompatible(TargetFramework, dep.TargetFramework))
			let lastDep = deps
				.Where(dep => AreFrameworksCompatible(TargetFramework, dep.TargetFramework))
				.OrderByDescending(dep => dep.TargetFramework.Version).First()
			let dll = GetDll(source, lastPkg.Identity, lastDep.TargetFramework)
			where dll != null
			let nfo = new FileInfo(dll)
			let capitalizedName = GetCapitalizedName(source, lastPkg.Identity)
			select new Lib(
				capitalizedName,
				lastPkg.Identity,
				lastDep.TargetFramework,
				dll,
				nfo.Length,
				nfo.LastWriteTime,
				lastDep.Packages.ToArray()
			)
		).ToArray();
		
		return arr;
	}
	
	private static string GetCapitalizedName(string source, PackageIdentity pkgId)
	{
		var nuspecFile = Path.Combine(source, pkgId.Id, $"{pkgId.Version}", $"{pkgId.Id}.nuspec");
		if (!File.Exists(nuspecFile)) throw new ArgumentException($"Cannot find .nuspec file: '{nuspecFile}'");
		var lines = File.ReadAllLines(nuspecFile);
		var str = $"<id>{pkgId.Id}</id>";
		var line = lines.FirstOrDefault(e => e.Contains(str, StringComparison.InvariantCultureIgnoreCase));
		if (line == null) throw new ArgumentException($"Cannot find id in .nuspec file: '{nuspecFile}'");
		line = line.Trim();
		var capitalizedName = line[4..^5];
		return capitalizedName;
	}
	
	private static string? GetDll(string source, PackageIdentity pkg, NuGetFramework framework)
	{
		var libFolder = Path.Combine(source, pkg.Id, $"{pkg.Version}", "lib");
		if (!Directory.Exists(libFolder)) return null;
		var fw = framework.GetShortFolderName();
		var dllFolder = (fw == "any") switch
		{
			true => PickAnyDllFolder(libFolder),
			false => Path.Combine(libFolder, fw)
		};
		if (!Directory.Exists(dllFolder)) return null; //throw new ArgumentException($"Cannot find DLL folder for {pkg} / {fw}");
		var dlls = Directory.GetFiles(dllFolder, "*.dll");
		var exactDll = Path.Combine(dllFolder, $"{pkg.Id}.dll");
		return dlls.Length switch
		{
			0 => File.Exists(Path.Combine(dllFolder, "_._")) switch
			{
				true => null,
				false => throw new ArgumentException($"No DLL found for {pkg} / {fw}"),
			},
			1 => dlls[0],
			_ => File.Exists(exactDll) switch
			{
				true => exactDll,
				false => null, //throw new ArgumentException($"Found too many DLLs ({dlls.Length}) for {pkg} / {fw}"),
			}
		};
	}
	
	private static string PickAnyDllFolder(string libFolder)
	{
		var ver = Directory.GetDirectories(libFolder)
			.Select(e => Path.GetFileName(e))
			.Select(e => NuGetFramework.Parse(e))
			.OrderByDescending(e => e.Version)
			.FirstOrDefault();
		return ver switch
		{
			null => libFolder,
			not null => Path.Combine(libFolder, ver.GetShortFolderName())
		};
	}
	
	internal static bool AreFrameworksCompatible(NuGetFramework mainFramework, NuGetFramework depFramework) => DefaultCompatibilityProvider.Instance.IsCompatible(mainFramework, depFramework);

	internal static T Get<T>(string source) where T : class, INuGetResource
	{
		var logger = new Logger();
		var cancel = CancellationToken.None;
		var providers = Repository.Provider.GetCoreV3();
		var pkgSource = new PackageSource(source);
		var repo = new SourceRepository(pkgSource, providers);
		var res = repo.GetResource<T>();
		return res;
	}
}



class Logger : ILogger
{
	private void L(LogLevel level, string s, string? extra = null) => $"[{level}{FmtExtra(extra)}] {s}".Dump();
	private string FmtExtra(string? extra) => extra switch
	{
		null => string.Empty,
		not null => $"-{extra}"
	};

	public void LogDebug(string data) => L(LogLevel.Debug, data);
	public void LogVerbose(string data) => L(LogLevel.Verbose, data);
	public void LogInformation(string data) => L(LogLevel.Information, data);
	public void LogMinimal(string data) => L(LogLevel.Minimal, data);
	public void LogWarning(string data) => L(LogLevel.Warning, data);
	public void LogError(string data) => L(LogLevel.Error, data);
	public void LogInformationSummary(string data) => L(LogLevel.Information, data, "summary");

	public void Log(LogLevel level, string data) => L(level, data, "l0");
	public async Task LogAsync(LogLevel level, string data) => L(level, data, "l1");
	public void Log(ILogMessage msg) => L(msg.Level, msg.Message, "l2");
	public async Task LogAsync(ILogMessage msg) => L(msg.Level, msg.Message, "l3");
}


static class StrExt
{
	public static string FmtSize(this long e)
	{
		if (e < 1024) return $"{e} Bytes";
		if (e < 1024 * 1024) return $"{e / 1024.0:F1} KB";
		return $"{e / (1024.0 * 1024):F1} MB";		
	}
}