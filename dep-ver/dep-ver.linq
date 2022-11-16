<Query Kind="Program">
  <NuGetReference>PowTrees</NuGetReference>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

#load ".\libs\01_structs"
#load ".\libs\02_lowlevel"
#load ".\libs\03_resolver"
#load ".\libs\04_structs-proj"
#load ".\libs\05_logic"

public static readonly Sln SlnMain = new("WebExtractor", @"C:\Dev\WebExtractor");

public static readonly Sln[] DepSlns = Directory.GetDirectories(@"C:\Dev_Nuget\Libs").SelectToArray(e => new Sln(Path.GetDirectoryName(e), e));

public record Fix(string Name, Version Ver);

enum Mode
{
	Analyze,
	Fix,
	Tree
}

void Main()
{
	switch (Mode.Tree)
	{
		case Mode.Analyze:
			Analyzer.Analyze(opt =>
			{
				opt.Type = Analyzer.ListType.AllExternal;
			});
			break;

		case Mode.Fix:
			Fixer.Fix(
				SlnMain,
				new Fix[] { new("System.ComponentModel.Annotations", Version.Parse("4.6.0")) },
				dryRun: true
			);
			break;

		case Mode.Tree:
			var proj_backend = new Proj(SlnMain, @"C:\Dev\WebExtractor\Tools\SnapTagger\SnapTagger.csproj");
			var maxDepth = proj_backend.FindMaxDepth();
			var tree = proj_backend.GetTree(opt =>
			{
				opt.MaxDepth = maxDepth;
			});

			var rg = new RangeControl(0, maxDepth, 0)
			{
				Width = "80vw",
			};
			var rgLabel = new Label();
			Util.HorizontalRun(true, rg, rgLabel).Dump();
			var dc = ConUtils.GetDC();
			void UpdateDC()
			{
				var limitTree = tree.LimitDepth(rg.Value);
				dc.Update(limitTree.LogToString());
				rgLabel.Text = $"{rg.Value}/{maxDepth} (nodes:{limitTree.Count()})";
			}
			UpdateDC();
			rg.ValueInput += (_, _) => UpdateDC();
			
			var conflicts = (
				from nod in tree
				where nod.V is PkgRef
				let pkgRef = (PkgRef)nod.V
				group pkgRef by pkgRef.Name into pkgRefNameGrp
				where pkgRefNameGrp.Count() > 1
				select new
				{
					Pkg = pkgRefNameGrp.Key,
					Vers = pkgRefNameGrp.SelectToArray(e => e.Ver)
				}
			).ToArray();

			new Lazy<object>(() => conflicts).Dump($"{tree.Count()} nodes. {conflicts.Length} conflicts");
			
			break;
	}
}



// **************
// **************
// ** Analyzer **
// **************
// **************
public static class Analyzer
{
	public enum ListType
	{
		All,
		AllExternal,
		UseNamesAndMasks
	}
	public class AnalyzeVersionsOpt
	{
		public Sln[] Slns { get; set; } = new[] {
			SlnMain
		};
		public bool ShowConflictedOnly { get; set; } = false;
		public ListType Type { get; set; } = ListType.AllExternal;
		public string[] DepNames { get; set; } = { };
		public string[] DepMasks { get; set; } = { };

		public static AnalyzeVersionsOpt Build(Action<AnalyzeVersionsOpt>? optFun) { var opt = new AnalyzeVersionsOpt(); optFun?.Invoke(opt); return opt; }
	}


	public static void Analyze(Action<AnalyzeVersionsOpt>? optFun = null)
	{
		var opt = AnalyzeVersionsOpt.Build(optFun);

		var allPkgRefs =
		(
			from sln in opt.Slns
			from proj in sln.GetProjs()
			from pkgRef in proj.GetRefs().OfType<PkgRefInProj>()
			where opt.DoesRefMatch(pkgRef)
			select pkgRef
		).ToArray();

		if (opt.ShowConflictedOnly)
		{
			var conflictMap = allPkgRefs
				.GroupBy(e => e.Name)
				.ToDictionary(
					e => e.Key,
					e => e.Select(f => f.Ver).Distinct().Count() > 1
				);
			allPkgRefs = allPkgRefs
				.Where(e => conflictMap[e.Name])
				.ToArray();
		}

		(
			from pkgRef in allPkgRefs
			group pkgRef by pkgRef.Name into pkgRefNameGrp
			select new
			{
				Dep = pkgRefNameGrp.Key,
				DepVers =
					from pkgRefName in pkgRefNameGrp
					group pkgRefName by pkgRefName.Proj.Sln.Name into pkgRefNameSlnGrp
					select new
					{
						Sln = pkgRefNameSlnGrp.Key,
						SlnVers =
							from pkgRefNameSln in pkgRefNameSlnGrp
							group pkgRefNameSln by pkgRefNameSln.Ver into pkgRefNameSlnVerGrp
							select new
							{
								Ver = pkgRefNameSlnVerGrp.Key,
								Cnt = pkgRefNameSlnVerGrp.Count(),
								Projs = new Lazy<Proj[]>(() => pkgRefNameSlnVerGrp.SelectToArray(e => e.Proj))
							}
					}
			}
		).Dump();
	}

	private static bool DoesRefMatch(this AnalyzeVersionsOpt opt, IPkgRef r) => opt.Type switch
	{
		ListType.All => true,
		ListType.AllExternal => !r.Name.StartsWith("Se."),
		ListType.UseNamesAndMasks =>
			opt.DepNames.Any(depName => r.Name == depName) ||
			opt.DepMasks.Any(depMask => r.Name.StartsWith(depMask)),
		_ => throw new ArgumentException()
	};
}



// ***********
// ***********
// ** Fixer **
// ***********
// ***********
public static class Fixer
{
	public static void Fix(Sln sln, Fix[] fixes, bool dryRun)
	{
		var pkgRefs =
		(
			from proj in sln.GetProjs()
			from pkgRef in proj.GetRefs().OfType<PkgRefInProj>()
			where fixes.Any(fix => pkgRef.Name == fix.Name && pkgRef.Ver != fix.Ver)
			select pkgRef
		).ToArray();

		pkgRefs.Dump();

		foreach (var pkgRef in pkgRefs)
		{
			var fix = fixes.Single(fix => fix.Name == pkgRef.Name);
			pkgRef.Ver = fix.Ver;
		}

		var xmls = pkgRefs.Select(e => e.Xml).Distinct().ToArray();
		if (!dryRun)
		{
			foreach (var xml in xmls)
				xml.Save();
		}

		$"Fixed {xmls.Length} projects".Dump();
	}
}



// ****************
// ****************
// ** TreeGetter **
// ****************
// ****************
public static class TreeGetter
{
	public class TreeOpt
	{
		public int MaxDepth { get; set; } = 3;
		public string[]? DepFilter { get; set; } = null;

		public static TreeOpt Build(Action<TreeOpt>? optFun)
		{
			var opt = new TreeOpt();
			optFun?.Invoke(opt);
			return opt;
		}
	}


	public static int FindMaxDepth(this Proj proj)
	{
		const int MaxDepth = 32;
		var cntPrev = -1;
		for (var i = 0; i < MaxDepth; i++)
		{
			var tree = proj.GetTree(opt => opt.MaxDepth = i);
			var cntNext = tree.Count();
			if (cntNext == cntPrev) return i - 1;
			cntPrev = cntNext;
		}
		return MaxDepth;
	}

	public static TNod<IRef> GetTree(this Proj proj, Action<TreeOpt>? optFun = null)
	{
		var opt = TreeOpt.Build(optFun);
		var set = new HashSet<IRef>();

		void RecurseInPkg(TNod<IRef> nod, IPkgRef pkgRef, int depth)
		{
			if (depth >= opt.MaxDepth) return;
			var pkgDeps = pkgRef.GetPkgDeps();
			foreach (var pkgDep in pkgDeps.WhereNotAdd(set))
			{
				var pkgDepNod = N(pkgDep, nod);
				RecurseInPkg(pkgDepNod, pkgDep, depth + 1);
			}
		}

		void RecurseInProj(TNod<IRef> nod, Proj proj, int depth)
		{
			if (depth >= opt.MaxDepth) return;
			var refs = proj.GetRefs();
			
			refs.OfType<ProjRef>().WhereNotAdd(set).ForEach(projRef =>
			{
				var projRefNod = N(projRef, nod);
				RecurseInProj(projRefNod, projRef.Proj, depth + 1);
			});
			
			refs.OfType<PkgRefInProj>().WhereNotAdd(set).ForEach(pkgRefInProj =>
			{
				var pkgRefInProjNod = N(pkgRefInProj, nod);
				RecurseInPkg(pkgRefInProjNod, pkgRefInProj, depth + 1);
			});
		}


		var root = N(new RootRef(), null);
		
		RecurseInProj(root, proj, 0);
		
		var filteredRoot = opt.Filter(root);
		return filteredRoot;
	}

	private static TNod<IRef> N(IRef r, TNod<IRef>? parent)
	{
		var nod = Nod.Make(r);
		if (parent != null) parent.AddChild(nod);
		return nod;
	}
	
	private static IEnumerable<T> WhereNotAdd<T>(this IEnumerable<T> source, HashSet<IRef> set) where T : IRef
	{
		var list = source.WhereNot(e => set.Contains(e)).ToArray();
		list.ForEach(e => set.Add(e));
		return list;
	}

	private static TNod<IRef> Filter(this TreeOpt opt, TNod<IRef> root)
	{
		if (opt.DepFilter == null) return root;
		var leafNods = root.Where(e => e.Children.Count == 0);
		var keepMap = new Dictionary<TNod<IRef>, bool>();

		bool Set(TNod<IRef> nod, bool setVal)
		{
			if (keepMap.TryGetValue(nod, out var val) && val) return false;
			keepMap[nod] = setVal;
			return true;
		}

		void FlagLeaf(TNod<IRef> leafNod)
		{
			var keep = opt.DepFilter.Any(dep => leafNod.V.Name == dep);
			var curNod = leafNod;

			while (curNod != null)
			{
				var cont = Set(curNod, keep);
				if (!cont) break;
				curNod = curNod.Parent;
			}
		}

		foreach (var leafNod in leafNods)
		{
			FlagLeaf(leafNod);
		}

		var filteredTree = root.FilterN(nod => keepMap[nod], opt => opt.Type = TreeFilterType.KeepIfAllParentsMatchingToo).Single();
		return filteredTree;
	}
}