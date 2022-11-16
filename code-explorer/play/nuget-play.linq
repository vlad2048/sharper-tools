<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>Microsoft.Build</NuGetReference>
  <NuGetReference>NuGet.PackageManagement</NuGetReference>
  <NuGetReference>NuGet.Protocol</NuGetReference>
  <Namespace>ExploreLib._2_Display</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay.DrawerLogic</Namespace>
  <Namespace>ExploreLib._2_Display.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>Microsoft.Build.Construction</Namespace>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Frameworks</Namespace>
  <Namespace>NuGet.Packaging</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>NuGet.Resolver</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.Serializer</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>NuGet.PackageManagement</Namespace>
  <Namespace>NuGet.ProjectManagement</Namespace>
</Query>

const string SlnFolder = @"C:\Dev_Nuget\Libs\PowWeb";
const string OutFolder = @"C:\Dev\sharper-tools\code-explorer\play";
public static string Mk(string baseName) => Path.Combine(OutFolder, $"{baseName}.json");
public static readonly string TreeFile = Mk("tree");
public static readonly string DepFile = Mk("deps");

static readonly NuGetFramework Framework = NuGetFramework.AnyFramework;

void TestPrj()
{
	var prjFile = @"C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker\ColorPicker.csproj";
	var root = ProjectRootElement.Open(prjFile);
	var itemGrps = root.ItemGroups.ToArray();
	var propGrps = root.PropertyGroups.ToArray();
	
	var targetFramework = (
		from propGrp in root.PropertyGroups
		from elt in propGrp.Children
		where elt is ProjectPropertyElement
		let propElt = (ProjectPropertyElement)elt
		where propElt.ElementName == "TargetFramework"
		select propElt.Value
	).First();
	
	var pkgRefs = (
		from itemGrp in itemGrps
		from elt in itemGrp.Children
		where elt is ProjectItemElement
		let prjElt = (ProjectItemElement)elt
		where prjElt.ElementName == "PackageReference"
		select new
		{
			Id = prjElt.Include,
			Ver = prjElt.OuterElement
		}
	).ToArray();

	var prjRefs = (
		from itemGrp in itemGrps
		from elt in itemGrp.Children
		where elt is ProjectItemElement
		let prjElt = (ProjectItemElement)elt
		where prjElt.ElementName == "ProjectReference"
		select new
		{
			Id = prjElt.Include,
		}
	).ToArray();

	prjRefs.Dump();

	pkgRefs.Dump();
}

void Main()
{
	Utils.SetStyles();
	
	var pkg = new PackageIdentity("PowWeb", NuGetVersion.Parse("0.0.2"));
	
	using var nugetRepos = new NugetRepos(SlnFolder);
	var nugetResolver = new NugetResolver(nugetRepos);
	
	var depTree = nugetRepos.BuildDepTree(pkg, 6);
	var depMap = nugetResolver.Resolve(pkg);

	var root = depTree.Map(dep => ResolvedDep.Make(dep, depMap.GetOpt(dep.Id) ?? "_"));

	PrintTree(root);

	(
		from nod in root
		let n = nod.V
		group n by n.Id into grp
		//orderby grp.Key
		let ranges = grp.Select(e => e.VerRange).Distinct().OrderBy(e => e).ToArray()
		let vers = grp.Select(e => e.Ver).Distinct().OrderBy(e => e).ToArray()
		select new
		{
			pkg = grp.Key,
			cnt = grp.Count(),
			ver = vers.JoinText(),
			ranges = ranges.JoinText(),
		}
	).Dump();
}


void PrintTree(TNod<ResolvedDep> root) =>
	TreeRenderer.Make(draw =>
	{
		draw.Tree(
			Pt.Empty,
			root,
			GetResolvedDepSz,
			(t, r) => DrawResolvedDep(draw, t, r),
			opt =>
			{
				opt.GutterSz = new Sz(3, 0);
				opt.AlignLevels = true;
			}
		);
	}).Dump();
	

Sz GetResolvedDepSz(ResolvedDep e) => new(e.DisplayStr.Length, 1);
void DrawResolvedDep(IDrawer draw, ResolvedDep e, R r)
{
	draw.UpdateDims(r);
	draw.Add(new Span(e.DisplayStr)
		.SetR(r)
		.Set("border", "1px solid #303030")
		.Set("border-radius", "3px")
	);
}


internal record Dep(string Id, string VerRange) { public string DisplayStr => $"{Id} {VerRange}"; }
record ResolvedDep(string Id, string VerRange, string Ver)
{
	public string DisplayStr => $"{Id} {VerRange} -> {Ver}";
	public static ResolvedDep Make(Dep dep, string ver) => new(dep.Id, dep.VerRange, ver);
}


class NugetReposOpt
{
	public ILogger Logger { get; set; } = new NullLogger();
	//public ILogger Logger { get; set; } = new LoggerImp();
	public NuGetFramework Framework { get; set; } = NuGetFramework.AnyFramework;
	public static NugetReposOpt Make(Action<NugetReposOpt>? optFun)
	{
		var opt = new NugetReposOpt();
		optFun?.Invoke(opt);
		return opt;
	}
	private class LoggerImp : LoggerBase
	{
		public override void Log(ILogMessage msg) => L($"[{msg.Time:HH:mm:ss.fff}]-[{msg.Level}] {msg.Message}");
		public override Task LogAsync(ILogMessage msg) => throw new NotImplementedException();
		private static void L(string s) => s.Dump();
	}
}
class NugetRepos : IDisposable
{
	public void Dispose() => srcCacheCtx.Dispose();

	private readonly NugetReposOpt opt;
	private readonly SourceCacheContext srcCacheCtx = new();

	public string SlnFolder { get; }
	public ISettings Settings { get; }
	public SourceRepositoryProvider Provider { get; }
	public SourceRepository[] Repos { get; }
	
	public NugetRepos(string slnFolder, Action<NugetReposOpt>? optFun = null)
	{
		slnFolder = @"C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker";
		opt = NugetReposOpt.Make(optFun);
		SlnFolder = slnFolder;
		Settings = NugetUtils.GetSettings(SlnFolder);
		var pkgSrcProvider = new PackageSourceProvider(Settings);
		Provider = new SourceRepositoryProvider(pkgSrcProvider, Repository.Provider.GetCoreV3());
		Repos = Provider.GetRepositories().ToArray();
	}

	public TNod<Dep> BuildDepTree(PackageIdentity pkg, int maxDepth) =>
		Utils.LoadJsonOrGen(
			TreeFile,
			() =>
				BuildInternal(pkg, maxDepth)
				.Map(e => new Dep(e.Pkg.Id, $"{e.VerRange}"))
		);

	private record RawDep(PackageIdentity Pkg, VersionRange VerRange);

	private TNod<RawDep> BuildInternal(PackageIdentity pkg, int maxDepth)
	{
		var root = Nod.Make(new RawDep(pkg, new VersionRange(pkg.Version)));

		void Recurse(TNod<RawDep> nod, int level)
		{
			if (level < 0) return;
			var deps = GetPkgDeps(nod.V.Pkg);
			foreach (var dep in deps)
			{
				var childPkg = new PackageIdentity(dep.Id, dep.VersionRange.MinVersion);
				var childNod = Nod.Make(new RawDep(childPkg, dep.VersionRange));
				nod.AddChild(childNod);
				Recurse(childNod, level - 1);
			}
		}
		Recurse(root, maxDepth);
		return root;
	}

	private PackageDependency[] GetPkgDeps(PackageIdentity pkg) =>
	(
		from repo in Repos
		let depNfoRes = repo.GetResource<DependencyInfoResource>()
		let depNfo = depNfoRes.ResolvePackage(
			pkg,
			opt.Framework,
			srcCacheCtx,
			opt.Logger,
			CancellationToken.None
		).Result
		where depNfo != null
		from pkgDep in depNfo.Dependencies
		select pkgDep
	).ToArray();
}

class NugetResolverOpt
{
	public string PkgFolder { get; set; } = @"C:\temp\fmt\pkgs";
	public DependencyBehavior Behavior { get; set; } = DependencyBehavior.Lowest;
	public bool IncludePrerelease { get; set; } = false;
	public bool IncludeUnlisted { get; set; } = false;
	public VersionConstraints Constraints { get; set; } = VersionConstraints.None;
	public static NugetResolverOpt Make(Action<NugetResolverOpt>? optFun)
	{
		var opt = new NugetResolverOpt();
		optFun?.Invoke(opt);
		return opt;
	}
}

class NugetResolver
{
	private readonly NugetRepos repos;
	private readonly PackageSourceProvider pkgSrcProv;
	private readonly ResolutionContext resolutionCtx;
	private readonly ProjectContext prjCtx;
	private readonly FolderNuGetProject prj;
	private readonly NuGetPackageManager pkgMan;

	public NugetResolver(NugetRepos repos, Action<NugetResolverOpt>? optFun = null)
	{
		var opt = NugetResolverOpt.Make(optFun);
		this.repos = repos;
		pkgSrcProv = new PackageSourceProvider(repos.Settings);
		resolutionCtx = new ResolutionContext(opt.Behavior, opt.IncludePrerelease, opt.IncludeUnlisted, opt.Constraints);
		prjCtx = new ProjectContext();
		prj = new FolderNuGetProject(repos.SlnFolder);
		pkgMan = new NuGetPackageManager(repos.Provider, repos.Settings, opt.PkgFolder)
		{
			PackagesFolderNuGetProject = prj
		};
		pkgMan.Pre
	}

	public Dictionary<string, string> Resolve(PackageIdentity pkg) =>
		Utils.LoadJsonOrGen(
			DepFile,
			() =>
				pkgMan.PreviewInstallPackageAsync(
					pkgMan.PackagesFolderNuGetProject,
					pkg.Id,
					resolutionCtx,
					prjCtx,
					repos.Repos,
					Array.Empty<SourceRepository>(),
					CancellationToken.None
				).Result
				.Where(e => e.NuGetProjectActionType == NuGetProjectActionType.Install)
				.ToDictionary(
					e => e.PackageIdentity.Id,
					e => $"{e.PackageIdentity.Version}"
				)
		);

	public class ProjectContext : INuGetProjectContext
	{
		public NuGetActionType ActionType { get; set; }
		public Guid OperationId { get; set; }

		public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.Ignore;
		public PackageExtractionContext PackageExtractionContext { get; set; }
		public XDocument OriginalPackagesConfig { get; set; }
		public ISourceControlManagerProvider SourceControlManagerProvider => null;
		public NuGet.ProjectManagement.ExecutionContext ExecutionContext => null;

		public void Log(MessageLevel level, string message, params object[] args) => L($"MSG_0: {string.Format(message, args)}");
		public void Log(ILogMessage message) => L($"MSG_1: {message.Message}");
		public void ReportError(string message) => LErr($"ERR_0: {message}");
		public void ReportError(ILogMessage message) => LErr($"ERR_1: {message.Message}");

		private static void L(string s)
		{
			//s.Dump();
		}
		private static void LErr(string s)
		{
			s.Dump();
		}
	}
}


static class NugetUtils
{
	public static ISettings GetSettings(string slnFolder) => Settings.LoadDefaultSettings(slnFolder, null, new MachineWideSettings());
	public static bool AreFrameoworksCompatible(NuGetFramework mainFramework, NuGetFramework depFramework) => DefaultCompatibilityProvider.Instance.IsCompatible(mainFramework, depFramework);


	private class MachineWideSettings : IMachineWideSettings
	{
		private readonly Lazy<ISettings> _settings;
		ISettings IMachineWideSettings.Settings => _settings.Value;
		public MachineWideSettings()
		{
			var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
			_settings = new Lazy<ISettings>(() => global::NuGet.Configuration.Settings.LoadMachineWideSettings(baseDirectory));
		}
	}
}




static class Utils
{
	public static void SetStyles() => Util.HtmlHead.AddStyles(HtmlStyle);

	private const string HtmlStyle = @"
		html {
			font-size: 16px;
		}
		body {
			margin: 5px;
			width: 100vw;
			height: 100vh;
			background-color: #030526;
			font-family: consolas;
		}
		div {
			color: #32FEC4;
		}
		a {
			display: block;
		}
	";
	
	public static V? GetOpt<K, V>(this Dictionary<K, V> map, K key)
		where K : notnull
		where V : class
		=>
			map.TryGetValue(key, out var val) switch
			{
				true => val,
				false => null
			};
	
	public static string JoinText(this IEnumerable<string> source) => string.Join(",", source);
	private static readonly JsonSerializerOptions jsonOpt = new()
	{
		WriteIndented = true
	};
	static Utils()
	{
		jsonOpt.Converters.Add(new TNodSerializer<Dep>());
	}
	private static void SaveJson<T>(this string filename, T obj) where T : class => File.WriteAllText(filename, JsonSerializer.Serialize(obj, jsonOpt));
	private static T? LoadJson<T>(this string filename) where T : class => File.Exists(filename) switch
	{
		true => JsonSerializer.Deserialize<T>(File.ReadAllText(filename), jsonOpt)!,
		false => null
	};
	public static T LoadJsonOrGen<T>(string filename, Func<T> genFun) where T : class
	{
		if (File.Exists(filename)) return filename.LoadJson<T>()!;
		var obj = genFun();
		filename.SaveJson(obj);
		return obj;
	}
}


