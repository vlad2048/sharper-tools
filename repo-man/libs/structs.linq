<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
  <Namespace>PowMaybe</Namespace>
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
}


public class Sln : IDisposable
{
	public Disp D { get; } = new();
	public void Dispose() => D.Dispose();
	
	private readonly IRwVar<Maybe<SlnDetails>> details;
	
	public SlnNfo Nfo { get; }
	public IRoVar<Maybe<SlnDetails>> Details => details;
	
	public Sln(SlnNfo nfo)
	{
		Nfo = nfo;
		details = Var.Make(May.None<SlnDetails>()).D(D);
	}
	
	public void Load() => details.V = May.Some(SlnDetails.Retrieve(Nfo));
}


public class Prj : IDisposable
{
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();

	private readonly IRwVar<Maybe<PrjDetails>> details;
	
	public PrjNfo Nfo { get; }
	public SlnNfo Sln { get; }
	public IRoVar<Maybe<PrjDetails>> Details => details;

	public Prj(PrjNfo nfo, SlnNfo sln)
	{
		Nfo = nfo;
		Sln = sln;
		details = Var.Make(May.None<PrjDetails>()).D(d);
	}
	
	public void Load(string version) => details.V = May.Some(PrjDetails.Retrieve(Nfo, Sln, version));
}


public record SlnDetails(
	SlnNfo Nfo,
	string Version,
	string SolutionFile,
	bool HasGitHub,
	GitStatus GitStatus,
	GitTrackingStatus GitTrackingStatus,
	Norm Norm,
	Prj[] Prjs,
	PrjNfo[] IgnoredPrjs
)
{
	public string Folder => Nfo.Folder;

	public static SlnDetails Retrieve(SlnNfo nfo)
	{
		var allPrjs = Files.FindRecursively(nfo.Folder, "*.csproj").SelectToArray(e => new PrjNfo(e, nfo));
		bool IsPackable(PrjNfo prj) => Xml.GetFlag(prj.File, XmlFlag.IsPackable);
		return new SlnDetails(
			nfo,
			Xml.Get(nfo.DirectoryBuildPropsFile, XmlPaths.Version),
			Directory.GetFiles(nfo.Folder, "*.sln").OrderByDescending(e => new FileInfo(e).Length).First(),
			ApiGithub.DoesRepoExist(nfo.Name),
			ApiGit.GetStatus(nfo.Folder),
			ApiGit.GetTrackingStatus(nfo.Folder),
			ApiSolution.GetNorm(nfo.Folder),
			allPrjs.Where(IsPackable).SelectToArray(e => new Prj(e, nfo)),
			allPrjs.WhereNotToArray(IsPackable)
		);
	}
}


public record PrjDetails(
	PrjNfo Nfo,
	SlnNfo Sln,
	string Version,
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

	public static PrjDetails Retrieve(PrjNfo nfo, SlnNfo sln, string version) => new(
		nfo,
		sln,
		version,
		ApiNuget.DoesPkgExist(nfo.Name),
		ApiNuget.DoesPkgVerExist(nfo.Name, version),
		ApiNuget.GetLastLocalVer(nfo),
		ApiNuget.GetLastRemoteVer(nfo),
		ApiSolution.GetPrjTime(nfo),
		ApiNuget.GetLocalVerTime(nfo, version),
		ApiNuget.GetRemoteVerTime(nfo, version)
	);
}
