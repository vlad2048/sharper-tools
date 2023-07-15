<Query Kind="Program">
  <NuGetReference>LINQPadExtras</NuGetReference>
  <NuGetReference>Microsoft.Build</NuGetReference>
  <NuGetReference>PowMaybeErr</NuGetReference>
  <NuGetReference>PowTrees.LINQPad</NuGetReference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>Microsoft.Build.Construction</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowMaybeErr</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowTrees.LINQPad</Namespace>
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
	Util.HtmlHead.AddStyles(@"body { font-family: consolas }");
	var allSlns = Cfg.Solutions.SelectToArray(e => SlnDetails.Retrieve(new SlnNfo(e)).Ensure());
	var trees = PkgRefUpdater.MakeRefTree(allSlns);
	var slnBasics = SlnDetails.Retrieve(new SlnNfo(@"C:\Dev_Nuget\Libs\PowBasics")).Ensure();
	var slnTrees = SlnDetails.Retrieve(new SlnNfo(@"C:\Dev_Nuget\Libs\PowTrees")).Ensure();
	
	foreach (var tree in trees)
		GenericTreePrinter.Print(tree).Dump();
}


public class Sln : IDisposable
{
	public Disp D { get; } = new();
	public void Dispose() => D.Dispose();
	
	private readonly IRwVar<Maybe<GitState>> gitState;
	private readonly IRwVar<MaybeErr<SlnDetails>> details;
	
	public SlnNfo Nfo { get; }
	public IRoVar<Maybe<GitState>> GitState => gitState;
	public IRoVar<MaybeErr<SlnDetails>> Details => details;
	
	public Sln(SlnNfo nfo)
	{
		Nfo = nfo;
		gitState = Var.Make(May.None<GitState>()).D(D);
		details = Var.Make(MayErr.None<SlnDetails>("loading")).D(D);
	}

	public void Load()
	{
		gitState.V = ApiGit.RetrieveGitState(Nfo.Folder);
		details.V = SlnDetails.Retrieve(Nfo);
	}
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

public record PkgRef(string Name, string Version);

public record SlnDetails(
	SlnNfo Nfo,
	string Version,
	string SolutionFile,
	Norm Norm,
	Prj[] Prjs,
	PrjNfo[] IgnoredPrjs,
	PkgRef[] PkgRefs
)
{
	public string Folder => Nfo.Folder;

	public static MaybeErr<SlnDetails> Retrieve(SlnNfo nfo)
	{
		bool IsPackable(PrjNfo prj) => Xml.GetFlag(prj.File, XmlFlag.IsPackable);

		var slnFile = Directory.GetFiles(nfo.Folder, "*.sln").OrderByDescending(e => new FileInfo(e).Length).FirstOrDefault();
		if (slnFile == null) return MayErr.None<SlnDetails>("no .sln file found");
		if (!File.Exists(nfo.DirectoryBuildPropsFile)) return MayErr.None<SlnDetails>("Directory.Build.props file not found");
		
		var allPrjs = Files.FindRecursively(nfo.Folder, "*.csproj")
			.Where(e => !e.Contains("_infos"))
			.SelectToArray(e => new PrjNfo(e, nfo));
		var prjs = allPrjs.Where(IsPackable).SelectToArray(e => new Prj(e, nfo));
		var prjsIgnored = allPrjs.WhereNotToArray(IsPackable);
		var pkgRefs = (
			from prj in prjs
			from pkgRef in PkgRefReader.Read(prj.Nfo)
			select pkgRef
		)
			.Distinct()
			.ToArray();
		
		return MayErr.Some(new SlnDetails(
			nfo,
			Xml.Get(nfo.DirectoryBuildPropsFile, XmlPaths.Version),
			slnFile,
			ApiSolution.GetNorm(nfo.Folder),
			prjs,
			prjsIgnored,
			pkgRefs
		));
	}
}

public record NugetReleaseConditions(
	bool NugetIsVersionNotYetReleased,
	bool GitIsTagNotYetMade,
	bool GitIsRepoClean,
	bool GitIsRepoInSync
);


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
		HasNuget:		ApiNuget.GetVers(NugetSource.Remote, nfo.Name).Any(),
		IsVerOnNuget:	ApiNuget.GetVers(NugetSource.Remote, nfo.Name).Contains(version),
		LastLocalVer:	ApiNuget.GetVers(NugetSource.Local, nfo.Name).FirstOrDefault(),
		LastRemoteVer:	ApiNuget.GetVers(NugetSource.Remote, nfo.Name).FirstOrDefault(),
		TimePrj:		ApiSolution.GetPrjTime(nfo),
		TimeLocal:		ApiNuget.GetVerTimestamp(NugetSource.Local, nfo.Name, version),
		TimeRemote:		ApiNuget.GetVerTimestamp(NugetSource.Remote, nfo.Name, version)
	);
}



static class PkgRefReader
{
	public static PkgRef[] Read(PrjNfo prj)
	{
		var root = ProjectRootElement.Open(prj.File)!;
		var itemGrps = root.ItemGroups.ToArray();
		var pkgRefs = (
			from itemGrp in itemGrps
			from elt in itemGrp.Children
			where elt is ProjectItemElement
			let prjElt = (ProjectItemElement)elt
			where prjElt.ElementName == "PackageReference"
			where prjElt.Metadata.Any(e => e.Name == "Version")
			select new PkgRef(
				prjElt.Include,
				prjElt.Metadata.First(e => e.Name == "Version").Value
			)
		).ToArray();
		return pkgRefs;
	}
}

public record SlnDetailsNeedUpdate(SlnDetails Sln, bool NeedUpdate);

public record SlnNode(SlnDetails Sln)
{
	public override string ToString() => $"{Sln.Nfo.Name} / {NeedsUpdate}";
	public bool NeedsUpdate { get; internal set; }
}

public static class PkgRefUpdater
{
	public static TNod<SlnNode>[] MakeRefTree(SlnDetails[] allSlns)
	{
		var roots = allSlns.SelectToList(sln => Nod.Make(new SlnNode(sln)));
		var map = roots.ToDictionary(e => e.V.Sln, e => e);
		foreach (var sln in allSlns)
		{
			var nod = map[sln];
			var deps = GetDependentSlns(sln, allSlns);
			foreach (var dep in deps)
			{
				var depNod = map[dep.Sln];
				roots.Remove(depNod);
				nod.AddChild(depNod);
			}
		}
		return roots.ToArray();
	}
	
	public static SlnDetailsNeedUpdate[] GetDependentSlns(SlnDetails sln, SlnDetails[] allSlns) =>
	(
		from prj in sln.Prjs
		from other in allSlns
		where other != sln
		from pkgRef in other.PkgRefs
		where pkgRef.Name == prj.Nfo.Name
		select new SlnDetailsNeedUpdate(other, pkgRef.Version != sln.Version)
	)
		.Distinct()
		.ToArray();
		
	public static bool DoesPkgNeedUpdating(SlnDetails sln, SlnDetails[] allSlns) => GetDependentSlns(sln, allSlns).Any(e => e.NeedUpdate);
	
	private record UpdateNfo(SlnDetails Sln, string PkgName, string PkgVer);
	
	public static void UpdateOthers(SlnDetails sln, SlnDetails[] others)
	{
		var updates = (
			from prj in sln.Prjs
			from other in others
			where other != sln
			from pkgRef in other.PkgRefs
			where pkgRef.Name == prj.Nfo.Name
			where pkgRef.Version != sln.Version
			select new UpdateNfo(other, prj.Nfo.Name, sln.Version)
		)
			.Distinct()
			.ToArray();
		
		foreach (var update in updates)
			UpdatePkgRefInSln(update.Sln, update.PkgName, update.PkgVer);
	}
	
	internal static void UpdatePkgRefInSln(SlnDetails sln, string pkgName, string pkgVer)
	{
		var prjFiles = sln.Prjs.Select(e => e.Nfo.File).Concat(sln.IgnoredPrjs.Select(e => e.File)).ToArray();
		foreach (var prjFile in prjFiles)
			UpdatePkgRefInPrj(prjFile, pkgName, pkgVer);
	}

	private static void UpdatePkgRefInPrj(string prjFile, string pkgName, string pkgVer)
	{
		//prjFile.Dump();
		var root = ProjectRootElement.Open(prjFile)!;
		var itemGrps = root.ItemGroups.ToArray();
		var pkgRefs = (
			from itemGrp in itemGrps
			from elt in itemGrp.Children
			where elt is ProjectItemElement
			let prjElt = (ProjectItemElement)elt
			where prjElt.ElementName == "PackageReference"
			where prjElt.Include == pkgName
			select prjElt
		).ToArray();
		
		foreach (var pkgRef in pkgRefs)
		{
			var verElt = pkgRef.Metadata.FirstOrDefault(e => e.Name == "Version");
			if (verElt != null)
				verElt.Value = pkgVer;
		}
		
		root.Save();

		var str = Xml.ModSaveToString(prjFile, mod => {
			//mod.SetFlag(XmlFlag.GenerateDocumentationFile, mod.GetFlag(XmlFlag.IsPackable));
		});
		File.WriteAllText(prjFile, str);
	}
}

















