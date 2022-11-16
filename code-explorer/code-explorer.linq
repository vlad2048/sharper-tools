<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <Namespace>ExploreLib._1_Structs</Namespace>
  <Namespace>ExploreLib._1_Structs.Utils</Namespace>
  <Namespace>ExploreLib._2_Display</Namespace>
  <Namespace>ExploreLib._3_Browsers._1_LibBrowse</Namespace>
  <Namespace>ExploreLib._3_Browsers._2_TypBrowse</Namespace>
  <Namespace>ExploreLib._3_Browsers._3_MethodBrowse</Namespace>
  <Namespace>ExploreLib.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Numerics</Namespace>
  <Namespace>System.Globalization</Namespace>
</Query>

const string? SearchLib = null;
const string? SearchTyp = null;
const string? DllOverride = null;
//@"C:\Program Files\dotnet\shared\Microsoft.NETCore.App\7.0.0\System.Private.CoreLib.dll";

record Point<T>(
	T X,
	T Y
) where T : INumber<T>;

void Main()
{
	SetStyles();
	
	var state = new State();
	
	// Display list of libraries
	// =========================
	Browser.Make(
		state.DispAllLibs,
		null,
		Flt.Combine(
			os => (string)os[0],
			Flt.MkText(SearchLib)
		),
		(data, filter) => data.Where(e => StrSearchUtils.IsMatch(e.Name, filter)).ToArray(),
		filtered => LibRenderer.Render(filtered, selLib => state.SelLib.V = May.Some(selLib))
	).Dump();
	
	
	// Display types in selected library
	// =================================
	Browser.Make(
		state.DispSelTypSet,
		() => state.SelLib.V = May.None<Lib>(),
		Flt.Combine(
			os => (string)os[0],
			Flt.MkText(SearchTyp)
		),
		(data, filter) => data.Filter(filter),
		filtered => TypRenderer.Render(filtered, selTyp => state.SelTyp.V = May.Some(selTyp))
	).Dump();
	
	
	// Display methods in selected type
	// ================================
	Browser.Make(
		state.DispSelMeths,
		() => state.SelTyp.V = May.None<Typ>(),
		Flt.Combine(
			os => new MethodFilter((string)os[0], (string)os[1], (string)os[2], (string)os[3]),
			Flt.MkText(),
			Flt.MkText(),
			Flt.MkText(),
			Flt.MkText()
		),
		(data, filter) => data.Where(meth => filter.IsMatch(meth)).ToArray(),
		filtered => MethRenderer.Render(filtered, mselMeth => {})
	).Dump();
}



record MethodFilter(string Ret, string Name, string Params, string Full)
{
	public bool IsMatch(Meth meth) =>
		StrSearchUtils.IsMatch(meth.Ret, Ret) &&
		StrSearchUtils.IsMatch(meth.Name, Name) &&
		StrSearchUtils.IsMatch(meth.ParamsStr, Params) &&
		StrSearchUtils.IsMatch($"{meth.Ret} {meth.Name} {meth.ParamsStr}", Full);
}



class State
{
	public Lib[] AllLibs { get; } = LibUtils.FindLibs();
	public IRwVar<Maybe<Lib>> SelLib { get; } = Var.Make(May.None<Lib>());
	public IRwVar<Maybe<Typ>> SelTyp { get; } = Var.Make(May.None<Typ>());


	public IRoVar<Maybe<Lib[]>> DispAllLibs => Var.Expr(() =>
		(SelLib.V.IsSome() || SelTyp.V.IsSome())
			? May.None<Lib[]>()
			: May.Some(AllLibs)
	);
	
	public IRoVar<Maybe<TypSet>> DispSelTypSet => Var.Expr(() =>
		(SelTyp.V.IsSome() || SelLib.V.IsNone())
			? May.None<TypSet>()
			//: May.Some(SelLib.V.Ensure().LoadTypSet())
			: May.Some((DllOverride == null ? SelLib.V.Ensure() : new Lib(DllOverride!)).LoadTypSet())
	);
	
	public IRoVar<Maybe<Meth[]>> DispSelMeths => Var.Expr(() =>
		SelTyp.V.IsSome()
			? May.Some(SelTyp.V.Ensure().Def.Methods.Where(e => e.IsPublic).SelectToArray(e => new Meth(e)))
			: May.None<Meth[]>()
		);
}




static void SetStyles() => Util.HtmlHead.AddStyles(@"
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
			font-family: consolas;
		}
	");



static class EnumExt
{
	public static T[] Favoritize<T>(this IEnumerable<T> source, Func<T, string> strFun, string? str) => str switch
	{
		null => source.ToArray(),
		not null => source
			.OrderBy(e =>
			{
				var itemStr = strFun(e);
				if (itemStr == str) return -2;
				if (itemStr.Contains(str, StringComparison.InvariantCultureIgnoreCase)) return -1;
				return 0;
			})
			.ToArray()
	};

	public static TypSet Favoritize(this TypSet typSet, string? str) => str switch
	{
		null => typSet,
		not null => typSet with
		{
			Roots = typSet.Roots.Favoritize(e => e.V.Name, str)
		}
	};
}