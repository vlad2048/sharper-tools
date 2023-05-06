<Query Kind="Program">
  <Reference Relative="ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll">&lt;MyDocuments&gt;\LINQPad Queries\sharper-tools\code-explorer\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>ExploreLib.Structs</Namespace>
</Query>

#load ".\libs\common"
#load ".\libs\rx-dbg"
#load ".\libs\api-nuget"
#load ".\libs\todump"
#load ".\1-explore-libs"
#load ".\2-explore-typs"
#load ".\3-explore-meths"


void Main()
{
	Css.Init();
	//V.Init(true);
	var d = new Disp();
	
	CodeExplorer();
}

void CodeExplorer()
{
	var d = new Disp();
	
	var selLibs = V.Make(Array.Empty<Lib>(), "selLibs").D(d);
	var selTyp = V.Make(May.None<Typ>(), "selTyp").D(d);
	var tab = Var.Expr(() => TabUtils.GetTab(selLibs.V, selTyp.V));
	
	LibExplorer(selLibs)
		.Disp(tab, Tab.Lib, d);
	
	TypExplorer(selLibs, selTyp)
		.Disp(tab, Tab.Typ, d);
	
	MethsExplorer(selTyp)
		.Disp(tab, Tab.Meth, d);
}



static class TabUtils
{
	public static void Disp(this (object, IDisposable) ui, IRoVar<Tab> tabV, Tab tab, IRoDispBase d) =>
		ui
			.D(d)
			.ShowWhen(tabV.SelectVar(e => e == tab))
			.Dump();
	
	public static DumpContainer ShowWhen(object ui, IRoVar<bool> predicate)
	{
		var dc = new DumpContainer(ui);
		predicate.Subscribe(e => dc.Style = e ? "" : "display:none").D(predicate);
		return dc;
	}
	
	public static Tab GetTab(Lib[] selLibs, Maybe<Typ> mayTyp) => selLibs.Any() switch
	{
		false => Tab.Lib,
		true => mayTyp.IsSome() switch
		{
			false => Tab.Typ,
			true => Tab.Meth
		}
	};

}