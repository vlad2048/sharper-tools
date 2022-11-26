<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>LINQPadExtras.Utils.Exts</Namespace>
</Query>

void Main()
{
	Thread.Sleep(200);
	DC.Content = 123;
	Thread.Sleep(200);
	DC.Content = 456;
	Thread.Sleep(200);
}

public enum Tab
{
	Lib,
	Typ,
	Meth
}

public static readonly DumpContainer DC = new DumpContainer().Dump();

public static class Css
{
	public static void Init() => Util.HtmlHead.AddStyles("""
		thead>tr:first-of-type {
			display: none;
		}
		td {
			vertical-align: middle;
		}
		legend {
			background: transparent;
		}
		body {
			font-family: consolas;
		}
		a {
			display: block;
			font-family: consolas;
		}
		
		
		
		
		
		.tooltip {
		  position: relative;
		  display: block;
		  border-bottom: 1px dotted black;
		}

		.tooltip .tooltiptext {
		  visibility: hidden;
		  width: 120px;
		  background-color: #555;
		  color: #fff;
		  text-align: left;
		  border-radius: 6px;
		  padding: 5px;
		  position: absolute;
		  z-index: 1;
		  bottom: 125%;
		  left: 50%;
		  margin-left: -60px;
		  opacity: 0;
		  transition: opacity 0.3s;
		}

		.tooltip .tooltiptext::after {
		  content: "";
		  position: absolute;
		  top: 100%;
		  left: 50%;
		  margin-left: -5px;
		  border-width: 5px;
		  border-style: solid;
		  border-color: #555 transparent transparent transparent;
		}

		.tooltip:hover .tooltiptext {
		  visibility: visible;
		  opacity: 1;
		}

		"""
	);
}


public static class DispUtils
{
	/*public static void DumpD(this (object, IDisposable) t, IRoDispBase d)
	{
		t.Item1.Dump();
		t.Item2.D(d);
	}*/
	
	public static (DumpContainer, IDisposable) DisplayMay<T>(this IObservable<Maybe<T>> valObs, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		var d = new Disp();
		valObs
			.Subscribe(mayVal =>
			{
				var content = mayVal.IsSome(out var val) switch
				{
					true => dispFun(val),
					false => ""
				};
				dc.UpdateContent(content);
			}).D(d);
		return (dc, d);
	}
	
	public static (DumpContainer, IDisposable) DisplayArr<T>(this IObservable<T[]> valObs, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		var d = new Disp();
		valObs
			.Subscribe(arr =>
			{
				//dc.UpdateContent(arr.Select(val => dispFun(val)));
				dc.Content = arr.Select(val => dispFun(val));
			}).D(d);
		return (dc, d);
	}
	
	public static (DumpContainer, IDisposable) Display<T>(this IObservable<T> valObs, Func<T, object> dispFun)
	{
		var dc = new DumpContainer();
		var d = new Disp();
		valObs
			.Subscribe(val => dc.UpdateContent(dispFun(val))).D(d);
		return (dc, d);
	}
	
	
	public static C SetIf<C>(this C ctrl, bool cond, string key, string val) where C : Control => cond switch
	{
		false => ctrl,
		true => ctrl.Set(key, val),
	};
}


public static class StrUtils
{
	//static int idx;
	
	public static bool IsMatch(string itemStr, string searchStr) =>
		//(idx++ % 2) == 0;
		
		/*itemStr
			.Trim()
			.Contains(searchStr.Trim(), StringComparison.InvariantCultureIgnoreCase);*/
			
		searchStr
			.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.All(part => itemStr.Contains(part, StringComparison.InvariantCultureIgnoreCase));
	
	public static string FmtSize(this int e)
	{
		if (e == 0) return "_";
		if (e < 1024) return $"{e}bytes";
		if (e < 1024 * 1024) return $"{e / 1024.0:F1}kb";
		return $"{e / (1024.0 * 1024):F1}mb";
	}
}