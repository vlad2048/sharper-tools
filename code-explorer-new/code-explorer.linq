<Query Kind="Program">
  <Reference Relative="ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll">&lt;MyDocuments&gt;\LINQPad Queries\sharper-tools\code-explorer-new\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>Mono.Cecil</NuGetReference>
  <NuGetReference>PowRxVar</NuGetReference>
  <Namespace>ExploreLib._1_DllFinding</Namespace>
  <Namespace>ExploreLib._1_DllFinding.Structs</Namespace>
  <Namespace>ExploreLib._2_DllReading</Namespace>
  <Namespace>ExploreLib._2_DllReading.Structs</Namespace>
  <Namespace>ExploreLib._3_DllGraphing</Namespace>
  <Namespace>ExploreLib._3_DllGraphing.Structs</Namespace>
  <Namespace>ExploreLib.UILogic</Namespace>
  <Namespace>ExploreLib.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowBasics.EqualityCode</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.LINQPad</Namespace>
  <Namespace>PowTrees.LINQPad.DrawerLogic</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
</Query>

#load ".\libs\ui"
#load ".\libs\fmt"

using TabObj = System.Func<PowRxVar.Disp, (object, LINQPad.Controls.Div)>;

interface IFocus {}
record FocusNone : IFocus;
record FocusDll(DllNfo Dll) : IFocus;
record FocusType(DllNfo Dll, TypeDef Type) : IFocus;
static class Focus {
	public static IFocus None => new FocusNone();
	public static IFocus Dll(DllNfo dll) => new FocusDll(dll);
	public static IFocus Type(DllNfo dll, TypeDef type) => new FocusType(dll, type);
	public static IFocus Dll(string dllName) => Dll(DllFinder.Dlls.Single(e => e.Name == dllName));
	public static IFocus Type(string dllName, string typeName)
	{
		var dll = DllFinder.Dlls.Single(e => e.Name == dllName);
		var types = DllReader.Read(dll);
		var type = types.Single(e => e.Name == typeName);
		return Type(dll, type);
	}
}

static IRwVar<TabObj> tab = null!;
static IRwVar<DllNfo[]> favsDlls = null!;
static IRwVar<IFocus> focus = null!;

void Main()
{
	Run();
	//ExploreTyp();
}



void Run()
{
	//Debugger.Launch();
	//UserPrefsLogic.DisableSave = true;
	
	favsDlls = UserPrefsLogic.GetFavsDlls().D(D);
	focus = Var.Make(Focus.None).D(D);
	
	//focus.V = Focus.Type("System.Reactive", "Observable");
	//focus.V = Focus.Dll("PowRxVar");
	//focus.V = Focus.Dll(DllNfo.FromFile(@"C:\Dev_Explore\WinDX\Libs\Gui\ControlSystem\bin\Debug\net7.0-windows\2_ControlSystem.dll"));
	
	var tabsUI = UI.Tabs(out tab, MkDllListUI);
	
	focus
		.SelectVar<IFocus, TabObj>(f => f switch
		{
			FocusNone => MkDllListUI,
			FocusDll e => MkDllDetailsUI(e.Dll),
			FocusType e => MkTypeDetailsUI(e.Dll, e.Type),
			_ => throw new ArgumentException()
		})
		.PipeTo(tab);
	
	tabsUI.Dump();
}



(object, Div) MkDllListUI(Disp d) => (
	Util.VerticalRun(
		favsDlls
			.DisplayList(dll => Util.Merge(dll, new
			{
				Unfav = UI.Link("Unfav", () => favsDlls.RemoveFromArray(dll))
			}))
			.Build(d),
		DllFinder.Dlls
			.DisplayList(dll => Util.Merge(dll, new
				{
					Fav = UI.Link("Fav", () => favsDlls.AddToArray(dll)).React(favsDlls, (btn, favs) => btn.Visible = !favs.Contains(dll)).D(d),
				}))
			.WithSearch(dll => dll.Name, UserPrefsLogic.GetDllSearchText().D(d))
			.WithOrdering(dll => dll.FileSize, dll => dll.Name)
			.WithPaging(20)
			.Build(d)
	),
	UI.TabsHeader("Choose a DLL...", null)
);





TabObj MkDllDetailsUI(DllNfo dll) => d =>
{
	var dllGraphs = dll.GetGraphs();
	var (searchText, searchUI) = UI.SearchBox().D(d);
	return (
		Util.VerticalRun(
			searchUI,

			dllGraphs.StaticClasses.DisplayList(type => new
					{
						kind = $"{type.Kind}".ToLower(),
						name = UI.Link(type.Name, () => focus.V = Focus.Type(dll, type)),
						size = $"{type.CodeSize / 1024} kb".PadLeft(8),
						members = $"{type.Members.Length}",
					})
				.WithOrdering(e => e.Members.Length, e => e.CodeSize, e => e.Name)
				.WithSearch(type => type.Name)
				.WithPaging(20)
				.Build(D)
				.WithHeader($"Static classes (x{dllGraphs.StaticClasses.Length})"),

			searchText.SelectVar(str =>
			{
				var g = dllGraphs.FilterGraphs(str);
				return Util.VerticalRun(
					g.InterfaceRoots.DisplayForest("Interfaces", e => focus.V = Focus.Type(dll, e)).D(d),
					g.ClassRoots.DisplayForest("Classes", e => focus.V = Focus.Type(dll, e)).D(d),
					g.StructRoots.DisplayForest("Structs", e => focus.V = Focus.Type(dll, e)).D(d)
				);
			}).ToDynaDC().D(d)
			
		),
		UI.TabsHeader(dll.Name, () => focus.V = Focus.None)
	);
};


TabObj MkTypeDetailsUI(DllNfo dll, TypeDef type) => d => (
	type.GetGfxs()
		.DisplayList(gfx => gfx switch
		{
			FieldGfx e => new
			{
				ret = e.DivType,
				name = e.DivName
			},
			PropGfx e => new
			{
				ret = e.DivType,
				name = e.DivName,
				@params = e.DivMethods
			},
			MethodGfx e => new
			{
				ret = e.DivRet,
				name = e.DivName,
				@params = e.DivParams
			},
			_ => throw new ArgumentException()
		})
		.WithSearch(gfx => gfx.Text)
		.Build(D),
	UI.TabsHeader($"{type.Name}", () => focus.V = Focus.Dll(dll))
);





static class FilterExt
{
	public static DllGraphs FilterGraphs(this DllGraphs g, string searchText) => new(
		g.InterfaceRoots.FilterForest(searchText),
		g.ClassRoots.FilterForest(searchText),
		g.StructRoots.FilterForest(searchText),
		g.StaticClasses
	);
	
	private static TNod<TypeNode<TypeDef>>[] FilterForest(this TNod<TypeNode<TypeDef>>[] roots, string searchText) =>
		roots
			.Select(root => root.FilterBranches(e => StringSearchUtils.IsMatch(e.Def.FullName, searchText)))
			.Where(e => e != null)
			.Select(e => e!)
			.ToArray();
}


static class UIExts
{
	public static (object, IDisposable) DisplayForest(this TNod<TypeNode<TypeDef>>[] roots, string title, Action<TypeDef> onClick)
	{
		if (roots.Length == 0) return ("", Disposable.Empty);
		var d = new Disp();
		var ui = roots.DrawForest(
			n => new Sz(n.Def.Name.Length, 1),
			(n, enabled) => enabled switch
			{
				false => new Span(n.Def.Name),
				true => UI.Link(n.Def.Name, () => { }),
			},
			n => onClick(n.Def)
		).D(d).WithHeader($"{title} (x{roots.Length})");
		return (ui, d);
	}
}



public static class DllCache
{
	private static readonly Dictionary<DllNfo, DllGraphs> cache = new();
	
	public static DllGraphs GetGraphs(this DllNfo dll) => cache.GetOrCreate(dll, () => dll.Read().BuildGraphs());
}



public static object ToDump(object o) => o switch
{
	DllNfo dll => new
	{
		DLL = UI.Link(dll.Name, () => focus.V = Focus.Dll(dll)),
		Ver = dll.Ver,
		Size = $"{dll.FileSize / 1024} kb".PadLeft(8),
	},
	_ => o
};



















