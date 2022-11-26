<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>PowRxVar</NuGetReference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Concurrency</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
</Query>

void Main()
{
	V.Init(true);
	
	var a = V.Make(5, "a");
	//var b = V.Make(7, "b");
	
	Thread.Sleep(500);
	a.V = 123;
}


record VNfo(DumpContainer DC, Div DCDiv, string Name, Type Type, object? Val)
{
	public bool Disposed { get; set; }
}

public const string CssStyle = """
	.rxdbg {
		background-color: #448;
		padding: 3px;
		font-family: consolas;
	}
	
	.rxdbg > div > div {
		display: flex;
		flex-wrap: wrap;
		gap: 3px;
	}
	
	.rxdbgpanel {
		background-color: #100c36;
		padding: 4px;
		border: 5px solid black;
		border-radius: 5px;
		line-height: 1.3em;
	}
""";
	// .rxdbg > div > div > div {

class VarDisplayer
{
	private readonly Div dcDiv;
	private readonly DumpContainer dc;
	private readonly Dictionary<object, VNfo> valMap = new();
	
	public VarDisplayer()
	{
		dc = new DumpContainer();
		dcDiv = new Div(dc);
		dcDiv.CssClass = "rxdbg";
		dcDiv.Dump();
	}
	
	public void SignalVarCreated<T>(IRoVar<T> rxVar, string name)
	{
		if (valMap.ContainsKey(rxVar)) throw new ArgumentException();
		
		var varDC = new DumpContainer();
		var varDCDiv = new Div(varDC);
		varDCDiv.CssClass = "rxdbgpanel";
		var nfo = new VNfo(varDC, varDCDiv, name, rxVar.GetType().GetGenericArguments()[0], rxVar.V);
		valMap[rxVar] = nfo;
		DisplayNfo(nfo);
		dc.AppendContent(varDCDiv);
	}
	
	public void SignalVarDisposed<T>(IRoVar<T> rxVar)
	{
		if (valMap[rxVar].Disposed) throw new ArgumentException("It shouldn't be disposed here");
		valMap[rxVar] = valMap[rxVar] with { Disposed = true };
		valMap[rxVar].DCDiv.SetBackColor("#c92a67");
	}
	
	public void SignalVarChanged<T>(IRoVar<T> rxVar)
	{
		if (!valMap.ContainsKey(rxVar)) throw new ArgumentException();
		if (valMap[rxVar].Disposed) throw new ArgumentException("It shouldn't be disposed here");
		valMap[rxVar] = valMap[rxVar] with { Val = rxVar.V };
		DisplayNfo(valMap[rxVar]);
	}
	
	private void DisplayNfo(VNfo nfo)
	{
		var ui = Util.VerticalRun(
			$"name: {nfo.Name}",
			$"type: {nfo.Type.Name}",
			nfo.Val
		);
		nfo.DC.Content = ui;
		//dc.Refresh();
	}
}





public static class V
{
	private static VarDisplayer? disp;
	
	public static void Init(bool enable)
	{
		disp = enable switch
		{
			false => null,
			true => new VarDisplayer()
		};
		if (enable)
			Util.HtmlHead.AddStyles(CssStyle);
	}
	
	public static IRwVar<T> Make<T>(
		T initVal,
		string name
	)
	{
		var rxVar = Var.Make(initVal);
		if (disp != null)
		{
			disp.SignalVarCreated(rxVar, name);
			rxVar
				.Skip(1)
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe(val => disp.SignalVarChanged(rxVar))
				.D(rxVar);
			
			rxVar.WhenDisposed.Subscribe(_ =>
			{
				disp.SignalVarDisposed(rxVar);
			}).D(rxVar);
		}
		return rxVar;
	}
}
