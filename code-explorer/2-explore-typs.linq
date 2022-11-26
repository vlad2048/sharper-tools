<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>Mono.Cecil</NuGetReference>
  <NuGetReference>PowBasics</NuGetReference>
  <NuGetReference>PowTrees</NuGetReference>
  <NuGetReference>PowTrees.LINQPad</NuGetReference>
  <Namespace>ExploreLib.Loading</Namespace>
  <Namespace>ExploreLib.Structs</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
  <Namespace>PowBasics.StringsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.LINQPad</Namespace>
  <Namespace>PowTrees.LINQPad.DrawerLogic</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#load ".\libs\common"
#load ".\libs\rx-dbg"
#load ".\libs\api-nuget"
#load ".\libs\todump"


void Main()
{
	Css.Init();
	V.Init(true);
	var d = new Disp();
	
	//var dllFile = @"C:\Users\vlad\.nuget\packages\powrxvar\0.0.9\lib\net7.0\PowRxVar.dll";
	var dllFile = @"C:\Users\vlad\.nuget\packages\dynamicdata\7.12.1\lib\net6.0\DynamicData.dll";
	var lib = new Lib(null, null, null, dllFile, 0, DateTime.MinValue, null);
	var selLibs = V.Make(new[] { lib }, "selLibs").D(d);
	var selTyp = V.Make(May.None<Typ>(), "selTyp").D(d);

	TypExplorer(selLibs, selTyp).D(d).Dump();
}

public static (object, IDisposable) TypExplorer(
	IRwVar<Lib[]> selLibs,
	IRwVar<Maybe<Typ>> selTyp
)
{
	var d = new Disp();
	var searchText = V.Make("", "searchText").D(d);
	var roots = selLibs.SelectVar(libs => (
		from lib in libs
		from root in TypSetLoader.Load(lib.DllFile).Roots
		select root
	).ToArray());
	bool IsActive(Typ typ) => StrUtils.IsMatch(typ.Name, searchText.V);
	
	var ui = Util.VerticalRun(
	
		Util.HorizontalRun(true,
			new Button("Back", _ => selLibs.V = Array.Empty<Lib>()),
			Html.TextBox("", searchText)
		),
		
		Observable.Merge(roots.ToUnit(), searchText.ToUnit()).Select(_ => roots.V)
			.Display(roots =>
			{
				return PanelGfx.Make(gfx =>
				{
					var x = 16;
					var y = 0;
					var activeRoots = roots.WhereToArray(root => root.Root.Any(nod => IsActive(nod.V)));
					foreach (var root in roots)
					{
						var rootTyp = root.Root.V;
						
						var rootSz = gfx.TreeCtrl<Typ, Control>(
							new Pt(x, y),
							root.Root,
							typ => typ.Name,
							(typ, str) =>
							{
								if (typ == rootTyp)
								{
									var sizeStr = typ.CodeSize.FmtSize();
									gfx.DivText(new R(0, y, sizeStr.Length, 1), sizeStr, ctrl => ctrl.SetForeColor(Styles.ColSize));
								}
								
								var color = ColsUtils.GetColForTypKind(typ.Kind);
								
								Control ctrl = typ.Kind switch
								{
									TypKind.Enum => Html.Div(str)
										.Set("cursor", "default")
										.WithTooltip(typ.EnumValues.JoinText("\n")),
									_ => new Hyperlink(str, _ => selTyp.V = May.Some(typ))
										.Set("cursor", "pointer")
								};
								
								return ctrl
									.StyleCommon()
									.SetForeColor(color);
							},
							opt =>
							{
								opt.GutterSz = new Sz(3, 0);
								opt.AlignLevels = false;
							}
						);
						y += rootSz.Height;
					}
				});


			}).D(d)
		
	);
	
	return (ui, d);
}


static class HtmlPlus
{
	public static C StyleCommon<C>(this C ctrl) where C : Control => ctrl
		.Set("cursor", "pointer");
	
	public static Div WithTooltip(this Div div, string tooltip)
	{
		var tooltipSpan = new Span(tooltip)
		{
			CssClass = "tooltiptext"
		};
		div.CssClass = "tooltip";
		div.Children.Add(tooltipSpan);
		return div;
	}
}


static class ColsUtils
{
	public static string GetColForTypKind(TypKind kind) => kind switch
	{
		TypKind.Interface => Styles.Interface,
		TypKind.Class => Styles.Class,
		TypKind.Struct => Styles.Struct,
		TypKind.Enum => Styles.Enum,
		_ => throw new ArgumentException()
	};
}

static class Styles
{
	public static readonly string ColSize = "#4f4e28";
	
	public static readonly string Interface = "#e8fc50";
	public static readonly string Class = "#8cbde4";
	public static readonly string Struct = "#9367e2";
	public static readonly string Enum = "#e64cde";
	public static readonly string InactiveTypeBrightness = "brightness(40%)";
}




