<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer\ExploreLib\bin\Debug\net6.0\ExploreLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\PowTrees\Libs\PowTrees\bin\Debug\net6.0\PowTrees.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\PowTrees\Tests\PowTrees.Tests\bin\Debug\net6.0\PowTrees.Tests.dll</Reference>
  <Namespace>ExploreLib._2_Display</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay</Namespace>
  <Namespace>ExploreLib._2_Display.TreeDisplay.DrawerLogic</Namespace>
  <Namespace>ExploreLib._2_Display.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
  <Namespace>PowBasics.Geom.Serializers</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.Serializer</Namespace>
  <Namespace>PowTrees.Tests.Algorithms.Layout.TestSupport</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
</Query>

void Main()
{
	//MakeTestCases(); return;
	
	Utils.SetStyles();
	
	// seed: 7
	var root = Utils.MakeRndTree(maxDepth: 4, maxChildCount: 3, seed: 32);
	
	//var layout = root.Layout(e => e.Size).Dump();
	//return;
	
	TreeRenderer.Make(draw =>
	{
		draw.Tree(
			Pt.Empty,
			root,
			e => e.Size,
			(t, r) =>
			{
				draw.DivText(r, t.Name, opt => opt.SetBackColor(t.Color));
			}
		);
	}).Dump();
}

void MakeTestCases()
{
	var fileOut = @"C:\Dev_Nuget\Libs\PowTrees\Tests\PowTrees.Tests\Algorithms\Layout\test-cases.json";
	var testCount = 100;
	
	var testCases = Enumerable.Range(0, testCount)
		.SelectToArray(i =>
			TestCaseUtils.Make(
				Utils.MakeRndTree(maxDepth: 4, maxChildCount: 3, seed: i == 0 ? 7 : null)
			)
		);
	var str = testCases.Ser();
	File.WriteAllText(fileOut, str);
}




static class Utils
{
	private const int NumCols = 10;
	private const double Sat = 0.72;
	private const double Val = 0.58;

	public static TNod<Rec> MakeRndTree(int maxDepth, int maxChildCount, int? seed = null)
	{
		var rnd = seed switch
		{
			not null => new Random(seed.Value),
			null => new Random((int)DateTime.Now.Ticks)
		};
		var nodIdx = 0;
		var colIdx = 0;
		string GetCol() { var col = Colors[colIdx]; colIdx = (colIdx + 1) % Colors.Length; return col; }
		string GetName() => $"n_{nodIdx++}";
		int GetWidth() => rnd.Next() % 8 + 3;
		int GetHeight() => rnd.Next() % 8 + 3;
		int GetChildCount() => rnd.Next() % maxChildCount;

		TNod<Rec> MkNod() => Nod.Make(new Rec(GetName(), GetCol(), GetWidth(), GetHeight()));

		TNod<Rec> Recurse(int level)
		{
			var nod = MkNod();
			if (level < maxDepth)
			{
				var childCount = GetChildCount();
				for (var i = 0; i < childCount; i++)
				{
					var childNod = Recurse(level + 1);
					nod.AddChild(childNod);
				}
			}
			return nod;
		}

		return Recurse(0);
	}

	public static void SetStyles() => Util.HtmlHead.AddStyles(@"
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
	");


	private static readonly Lazy<string[]> colors = new(() =>
	{
		var list = new List<string>();
		var hue = 0.0;
		var delta = 360.0 / NumCols;
		while (hue < 360)
		{
			list.Add(ColorFromHSV(hue, Sat, Val));
			hue += delta;
		}
		return list.ToArray();
	});
	private static string[] Colors => colors.Value;

	private static string ColorFromHSV(double hue, double saturation, double value)
	{
		int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
		double f = hue / 60 - Math.Floor(hue / 60);

		value = value * 255;
		int v = Convert.ToInt32(value);
		int p = Convert.ToInt32(value * (1 - saturation));
		int q = Convert.ToInt32(value * (1 - f * saturation));
		int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

		static string Mk(int r, int g, int b) => $"#{r:X2}{g:X2}{b:X2}";

		if (hi == 0)
			return Mk(v, t, p);
		else if (hi == 1)
			return Mk(q, v, p);
		else if (hi == 2)
			return Mk(p, v, t);
		else if (hi == 3)
			return Mk(p, q, v);
		else if (hi == 4)
			return Mk(t, p, v);
		else
			return Mk(v, p, q);
	}
}







static class TreeDumpExt
{
	private static Lazy<MethodInfo> genLogMethodDef = new(() => typeof(Algo_Logging).GetMethod("LogToString")!);
	private static MethodInfo GenLogMethodDef => genLogMethodDef.Value;

	public static bool IsGenNod(this object o)
	{
		var t = o.GetType();
		return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(TNod<>);
	}

	public static string LogGenNod(this object o)
	{
		var method = GenLogMethodDef.MakeGenericMethod(o.GetGenNodType());
		var strObj = method.Invoke(null, new object[] { o, null! });
		var str = strObj as string;
		return str!;
	}

	private static Type GetGenNodType(this object o)
	{
		if (!o.IsGenNod()) throw new ArgumentException();
		return o.GetType().GenericTypeArguments.Single();
	}
}


public static object ToDump(object o)
{
	if (o.IsGenNod()) return new Div(new Span(o.LogGenNod()));
	return o switch
	{
		R e => $"{e}",
		_ => o
	};
}