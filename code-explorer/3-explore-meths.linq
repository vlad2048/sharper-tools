<Query Kind="Program">
  <Reference Relative="ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll">&lt;MyDocuments&gt;\LINQPad Queries\sharper-tools\code-explorer\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>LINQPadExtras</NuGetReference>
  <Namespace>ExploreLib.Loading</Namespace>
  <Namespace>ExploreLib.Structs</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#load ".\libs\common"
#load ".\libs\api-nuget"
#load ".\libs\rx-dbg"
#load ".\libs\todump"

void Main()
{
	Css.Init();
	V.Init(true);
	var d = new Disp();

	var dllFile = @"C:\Users\vlad\.nuget\packages\dynamicdata\7.12.1\lib\net6.0\DynamicData.dll";
	var className = "DynamicData.ObservableCacheEx";
	var typSet = TypSetLoader.Load(dllFile);
	var typ = (
		from root in typSet.Roots
		from nod in root.Root
		let t = nod.V
		where t.FullName == className
		select t
	).First();
	
	var selTyp = Var.Make(May.Some(typ));
	
	MethsExplorer(selTyp)
		.D(d).Dump();
}

(object, IDisposable) MethsExplorer(IRwVar<Maybe<Typ>> selTyp)
{
	var d = new Disp();
	var selMeths = selTyp.SelectVarMay(typ => typ.Def.Methods.SelectToArray(e => new Meth(e)));
	var txtRet = V.Make("", "txtRet").D(d);
	var txtName = V.Make("", "txtName").D(d);
	var txtParams = V.Make("", "txtParams").D(d);
	var txtFull = V.Make("", "txtFull").D(d);
	var filter = Var.Expr(() => new MethodFilter(txtRet.V, txtName.V, txtParams.V, txtFull.V));
	var filteredMeths = Var.Expr(() => FilterMeths(selMeths.V, filter.V));
	
	var ui = Util.VerticalRun(
	
		Util.HorizontalRun(true,
			new Button("Back", _ => selTyp.V = May.None<Typ>()),
			Html.TextBox("", txtRet),
			Html.TextBox("", txtName),
			Html.TextBox("", txtParams),
			Html.TextBox("", txtFull)
		),

		filteredMeths
			.DisplayArr(meth => new
			{
				ret = meth.MakeRetDiv(),
				name = meth.MakeNameDiv(),
				@params = meth.MakeParamsDiv(),
			}).D(d)
	
	);
	
	return (ui, d);
}

Meth[] FilterMeths(Maybe<Meth[]> mayMeths, MethodFilter filter) => mayMeths.IsSome(out var meths) switch
{
	true => meths.WhereToArray(filter.IsMatch),
	false => Array.Empty<Meth>()
};

record MethodFilter(string Ret, string Name, string Params, string Full)
{
	public bool IsMatch(Meth meth) =>
		StrUtils.IsMatch(meth.Ret, Ret) &&
		StrUtils.IsMatch(meth.Name, Name) &&
		StrUtils.IsMatch(meth.ParamsStr, Params) &&
		StrUtils.IsMatch($"{meth.Ret} {meth.Name} {meth.ParamsStr}", Full);
}

static class RenderExt
{
	private static readonly string MethRet = "#C14071";
	private static readonly string MethName = "#52FE7F";
	private static readonly string MethOff = "#8F8F8F";
	private static readonly string MethParamType = "#5EAAF5";
	private static readonly string MethParamName = "#BDBDBD";


	public static Control MakeRetDiv(this Meth meth) => new Div(
		new Span($"{meth.Ret} ")
			.SetForeColor(MethRet)
	);

	public static Control MakeNameDiv(this Meth meth) => new Div(
		new Span(meth.Name)
			.SetForeColor(MethName)
	);

	public static Control MakeParamsDiv(this Meth meth)
	{
		var spans = new List<Control>();
		void Add(string content, string color) => spans.Add(new Span(content).SetForeColor(color));
		Add("(", MethOff);
		for (var i = 0; i < meth.Params.Length; i++)
		{
			if (i > 0) Add(", ", MethOff);
			var p = meth.Params[i];
			Add(p.Type + " ", MethParamType);
			Add(p.Name, MethParamName);
		}
		Add(")", MethOff);
		return new Div(spans);
	}
}









