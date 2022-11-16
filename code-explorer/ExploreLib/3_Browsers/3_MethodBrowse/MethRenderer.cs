using ExploreLib._1_Structs;
using ExploreLib._2_Display;
using ExploreLib._2_Display.Utils;
using LINQPad.Controls;

namespace ExploreLib._3_Browsers._3_MethodBrowse;

public static class MethRenderer
{
	public static object Render(Meth[] meths, Action<Meth> onSel) =>
		meths
			.Select(meth => new
			{
				ret = meth.MakeRetDiv(),
				name = meth.MakeNameDiv(),
				@params = meth.MakeParamsDiv(),
			});


	private static Control MakeRetDiv(this Meth meth) => new Div(
		new Span($"{meth.Ret} ")
			.SetForeColor(Cols.MethRet)
	);

	private static Control MakeNameDiv(this Meth meth) => new Div(
		new Span(meth.Name)
			.SetForeColor(Cols.MethName)
	);

	private static Control MakeParamsDiv(this Meth meth)
	{
		var spans = new List<Control>();
		void Add(string content, string color) => spans.Add(new Span(content).SetForeColor(color));
		Add("(", Cols.MethOff);
		for (var i = 0; i < meth.Params.Length; i++)
		{
			if (i > 0) Add(", ", Cols.MethOff);
			var p = meth.Params[i];
			Add(p.Type + " ", Cols.MethParamType);
			Add(p.Name, Cols.MethParamName);
		}
		Add(")", Cols.MethOff);
		return new Div(spans);
	}


	/*private static Control MakeDiv(this Meth meth)
	{
		var divRet = new Div(
			new Span($"{meth.Ret} ")
				.SetForeColor(Cols.MethRet)
		);

		var divName = new Div(
			new Span(meth.Name)
				.SetForeColor(Cols.MethName)
		);


		var spans = new List<Control>();
		void Add(string content, string color) => spans.Add(new Span(content).SetForeColor(color));
		Add("(", Cols.MethOff);
		for (var i = 0; i < meth.Params.Length; i++)
		{
			if (i > 0) Add(", ", Cols.MethOff);
			var p = meth.Params[i];
			Add(p.Type + " ", Cols.MethParamType);
			Add(p.Name, Cols.MethParamName);
		}
		Add(")", Cols.MethOff);
		var divParams = new Div(
			spans
		);

		var div = new Div(
				divRet,
				divName,
				divParams
			)
			.Set("display", "flex");

		return div;
	}*/
}