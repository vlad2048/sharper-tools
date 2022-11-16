<Query Kind="Program">
  <Reference Relative="ExploreLib\bin\Debug\net7.0\ExploreLib.dll">C:\Dev\sharper-tools\code-explorer\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <Namespace>ExploreLib.NugetLogic.Structs</Namespace>
  <Namespace>ExploreLib.NugetLogic</Namespace>
  <Namespace>ExploreLib.NugetLogic.Structs.Refs</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay.DrawerLogic</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
</Query>

void Main()
{
	Utils.SetStyles();
	var prj = Prj.Load(@"C:\Dev_Nuget\Libs\PowWeb\Libs\PowWeb\PowWeb.csproj");
	//prj = Prj.Load(@"C:\Dev\sharper-tools\code-explorer\ExploreLib\ExploreLib.csproj");
	var depTree = DepTreeBuilder.Build(prj, opt =>
	{
	});
	PrintTree(depTree);
}

void PrintTree(DepTree depTree) =>
	TreeRenderer.Make(draw =>
	{
		draw.Tree(
			Pt.Empty,
			depTree.Root,
			e => e.GetSz(depTree.ResolveMap),
			(t, r) => t.Draw(draw, r, depTree.ResolveMap),
			opt =>
			{
				opt.GutterSz = new Sz(3, 0);
				opt.AlignLevels = true;
			}
		);
	}).Dump();


static class RefExt
{
	public static void Draw(this IRef e, IDrawer draw, R r, Dictionary<string, Pkg> resolveMap)
	{
		draw.DivText(r, e.GetStr(resolveMap));
	}
	public static Sz GetSz(this IRef e, Dictionary<string, Pkg> resolveMap) => new Sz(e.GetStr(resolveMap).Length, 1);
	public static string GetStr(this IRef e, Dictionary<string, Pkg> resolveMap) => e switch
	{
		PrjRef r => $"{r.Name}",
		PkgRef r => $"{r.Id} {r.VerRange}{resolveMap.GetOpt(r.Id, e => $"{e.Ver}", string.Empty)}",
		_ => throw new ArgumentException()
	};
	private static string GetOpt<T>(this Dictionary<string, T> map, string key, Func<T, string> strFun, string defStr) =>
		map.TryGetValue(key, out var val) switch
		{
			true => $" {strFun(val)}",
			false => defStr
		};
}

/*	draw.UpdateDims(r);
	draw.Add(new Span(e.DisplayStr)
		.SetR(r)
		.Set("border", "1px solid #303030")
		.Set("border-radius", "3px")
	);*/


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
}