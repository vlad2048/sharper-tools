<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
</Query>

#load "..\cfg"
#load "..\libs-lowlevel\xml"
#load "..\libs-lowlevel\watcher"
#load ".\api-common"
#load ".\api-nuget"
#load ".\api-git"
#load ".\api-github"
#load ".\api-solution"

void Main()
{
	//var sln = new Sln(@"C:\Dev_Nuget\Libs\PowRxVar", (_, _) => {});
	//sln.SetVersion("0.0.5");
}



public record PrjComputed(
	bool HasNuget,
	bool IsVerOnNuget,
	string? LastLocalVer,
	string? LastRemoteVer,
	DateTime TimePrj,
	DateTime? TimeLocal,
	DateTime? TimeRemote
)
{
	public bool IsTimeLocalUpToDate => TimeLocal switch { null => false, not null => TimeLocal.Value >= TimePrj };
	public bool IsTimeRemoteUpToDate => TimeRemote switch { null => false, not null => TimeRemote.Value >= TimePrj };
	
	public static PrjComputed Retrieve(PrjNfo nfo, string version) => new(
		ApiNuget.DoesPkgExist(nfo.Name),
		ApiNuget.DoesPkgVerExist(nfo.Name, version),
		ApiNuget.GetLastLocalVer(nfo),
		ApiNuget.GetLastRemoteVer(nfo),
		ApiSolution.GetPrjTime(nfo),
		ApiNuget.GetLocalVerTime(nfo, version),
		ApiNuget.GetRemoteVerTime(nfo, version)
	);
}


public class Prj
{
	// Basic
	// =====
	public PrjNfo Nfo { get; }
	public string File => Nfo.File;
	public string Folder => Nfo.Folder;
	public string Name => Nfo.Name;
	public string NugetUrl => Nfo.NugetUrl;
	
	// Sln
	// ===
	public Sln Sln { get; }
	public string Version => Sln.Computed.Version;
	
	// Computed
	// ========
	public PrjComputed Computed { get; }

	public void Refresh() => Sln.Refresh();

	public Prj(PrjNfo nfo, Sln sln)
	{
		Nfo = nfo;
		Sln = sln;
		Computed = PrjComputed.Retrieve(Nfo, Version);
	}
}


public record SlnComputed(
	SlnNfo Nfo,
	string Version,
	string SolutionFile,
	bool HasGitHub,
	GitStatus GitStatus,
	GitTrackingStatus GitTrackingStatus,
	Norm Norm,
	PrjNfo[] Prjs,
	PrjNfo[] IgnoredPrjs
)
{
	public string Folder => Nfo.Folder;
	
	public static SlnComputed Retrieve(SlnNfo nfo)
	{
		var allPrjs = Files.FindRecursively(nfo.Folder, "*.csproj").SelectToArray(e => new PrjNfo(e));
		bool IsPackable(PrjNfo prj) => Xml.GetFlag(prj.File, XmlFlag.IsPackable);
		return new SlnComputed(
			nfo,
			Xml.Get(nfo.DirectoryBuildPropsFile, XmlPaths.Version),
			Directory.GetFiles(nfo.Folder, "*.sln").OrderByDescending(e => new FileInfo(e).Length).First(),
			ApiGithub.DoesRepoExist(nfo.Name),
			ApiGit.GetStatus(nfo.Folder),
			ApiGit.GetTrackingStatus(nfo.Folder),
			ApiSolution.GetNorm(nfo.Folder),
			allPrjs.WhereToArray(IsPackable),
			allPrjs.WhereNotToArray(IsPackable)
		);
	}
}

public class Sln : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	private readonly ISubject<Unit> whenRefreshSubj;

	// Basic
	// =====
	public SlnNfo Nfo { get; }
	public string Folder => Nfo.Folder;
	public string Name => Nfo.Name;

	// Computed
	// ========
	public SlnComputed Computed { get; }
	public string Version => Computed.Version;

	public IObservable<Unit> WhenRefresh { get; }
	public void Refresh() => whenRefreshSubj.OnNext(Unit.Default);

	public Sln(SlnNfo nfo, ISubject<Unit> whenRefreshSubj)
	{
		Nfo = nfo;
		this.whenRefreshSubj = whenRefreshSubj;
		Computed = SlnComputed.Retrieve(Nfo);

		var watcher = new FolderWatcher(Folder).D(d);

		WhenRefresh = watcher.WhenChange.ToUnit();
	}
}












