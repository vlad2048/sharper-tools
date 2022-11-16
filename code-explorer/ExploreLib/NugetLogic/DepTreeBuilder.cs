using ExploreLib.NugetLogic.Components;
using ExploreLib.NugetLogic.Logging.Exts;
using ExploreLib.NugetLogic.Logging.Loggers;
using ExploreLib.NugetLogic.Logging.Loggers.Base;
using ExploreLib.NugetLogic.Structs;
using ExploreLib.NugetLogic.Structs.Refs;
using ExploreLib.NugetLogic.Utils;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using PowBasics.CollectionsExt;

namespace ExploreLib.NugetLogic;

public class DepTreeBuilderOpt
{
	public string CacheFolder { get; set; } = @"C:\caches\code-explorer";

	public IComLogger Logger { get; set; } = new ConComLogger(LogLevel.Verbose);

	public int? MaxDepth { get; set; } = null;
	public DependencyBehavior ResolveBehavior { get; set; } = DependencyBehavior.Lowest;
	public bool ResolveIncludePrerelease { get; set; } = false;
	public bool ResolveIncludeUnlisted { get; set; } = false;
	public VersionConstraints ResolveConstraints { get; set; } = VersionConstraints.None;

	public static DepTreeBuilderOpt Make(Action<DepTreeBuilderOpt>? optFun)
	{
		var opt = new DepTreeBuilderOpt();
		optFun?.Invoke(opt);
		return opt;
	}
}

public class DepTreeBuilder
{
	public static DepTree Build(Prj prj, Action<DepTreeBuilderOpt>? optFun)
	{
		var opt = DepTreeBuilderOpt.Make(optFun);
		return NugetCacheUtils.GetGenDepTree(
			prj,
			opt.CacheFolder,
			() => BuildInternal(prj, opt)
		);
	}

	private static DepTree BuildInternal(Prj prj, DepTreeBuilderOpt opt)
	{
		var root = PrjUtils.BuildTree(prj);
		using var nugetRepos = new NugetRepos(prj.Folder);
		RecursivelyLookForPkgRefs(
			root,
			prj.TargetFramework,
			nugetRepos,
			opt.Logger.MakeNugetLogger(),
			opt.MaxDepth
		);

		var resolveMap = NugetResolver.Resolve(
			prj,
			nugetRepos,
			opt.ResolveBehavior,
			opt.ResolveIncludePrerelease,
			opt.ResolveIncludeUnlisted,
			opt.ResolveConstraints,
			opt.Logger
		);

		return new DepTree(root, resolveMap);
	}

	private static void RecursivelyLookForPkgRefs(
		TNod<IRef> root,
		NuGetFramework framework,
		NugetRepos nugetRepos,
		ILogger reposLogger,
		int? maxDepth
	)
	{
		var visited = new HashSet<PkgRef>();

		IEnumerable<PkgRef> FindPkgRefDeps(PkgRef pkg)
		{
			var pkgId = new PackageIdentity(pkg.Id, pkg.VerRange.MinVersion);
			return
				from repo in nugetRepos.Repos
				let depNfoRes = repo.GetResource<DependencyInfoResource>()
				let depNfo = depNfoRes.ResolvePackage(
					pkgId,
					framework,
					nugetRepos.SrcCacheCtx,
					reposLogger,
					CancellationToken.None
				).Result
				where depNfo != null
				from pkgDep in depNfo.Dependencies
				let pkgRef = new PkgRef(
					pkgDep.Id,
					pkgDep.VersionRange
				)
				where !visited.Contains(pkgRef)
				select pkgRef;
		}

		void Recurse(TNod<IRef> nod, int level)
		{
			if (maxDepth.HasValue && level >= maxDepth.Value) return;
			if (nod.V is PkgRef pkgRef)
			{
				var pkgChildrenToAdd = FindPkgRefDeps(pkgRef)
					.Select(e => Nod.Make<IRef>(e)).ToArray();
				foreach (var pkgChildToAdd in pkgChildrenToAdd)
				{
					visited.Add((PkgRef)pkgChildToAdd.V);
					nod.AddChild(pkgChildToAdd);
				}
			}
			nod.Children.ForEach(e => Recurse(e, level + 1));
		}
		Recurse(root, 0);
	}
}