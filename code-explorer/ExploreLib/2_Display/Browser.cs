using System.Reactive.Linq;
using ExploreLib.Utils.Exts;
using LINQPad;
using LINQPad.Controls;
using PowBasics.CollectionsExt;
using PowMaybe;
using PowRxVar;

namespace ExploreLib._2_Display;

public static class Browser
{
	public static DumpContainer Make<
		TData,
		TFilter,
		TFilteredData
	>(
		IRoVar<Maybe<TData>> data,
		Action? onBack,
		FltCombined<TFilter> filter,
		Func<TData, TFilter, TFilteredData> filterFun,
		Func<TFilteredData, object> renderFun
	)
	{
		var dcData = new DumpContainer();
		var dc = new DumpContainer(Util.VerticalRun(
			onBack switch
			{
				null => filter.Ui,
				not null => Util.HorizontalRun(true,
					new Hyperlink("back", _ => onBack()),
					filter.Ui
				)
			},
			dcData
		));
		dc.ToggleVisibility(data);

		var filteredData = Var.Expr(() => data.V.Select(dataV => filterFun(dataV, filter.Val.V)));
		filteredData.WhenSome().Subscribe(d => dcData.UpdateContent(renderFun(d)));
		return dc;
	}

	private static void ToggleVisibility<T>(this DumpContainer dc, IRoVar<Maybe<T>> v) =>
		v.Select(e => e.IsSome()).Subscribe(visible => dc.Style = visible ? string.Empty : "display:none");
}

public class FltCombined<TFilter>
{
	public IRoVar<TFilter> Val { get; }
	public object Ui { get; }
	public FltCombined(IRoVar<TFilter> val, object ui)
	{
		Val = val;
		Ui = ui;
	}
}

public class Flt
{
	public IRoVar<object> Val { get; }
	public object Ui { get; }

	private Flt(IRoVar<object> val, object ui)
	{
		Val = val;
		Ui = ui;
	}

	public static Flt MkText(string? initialText = null)
	{
		initialText ??= string.Empty;
		var text = Var.Make<object>(initialText);
		var textBox = new TextBox(initialText, onTextInput: t =>
		{
			text.V = t.Text;
		})
		{
			Width = "250px"
		};
		return new Flt(text, textBox);
	}

	public static FltCombined<TFilter> Combine<TFilter>(Func<object[], TFilter> filterFun, params Flt[] filters)
	{
		var filterVals = filters.SelectToArray(e => e.Val);
		var filterUis = filters.SelectToArray(e => e.Ui);

		var d = new Disp();
		var mergedVar = Var.Make(
			filterFun(filterVals.SelectToArray(e => e.V)),
			filterVals.CombineLatest(e => e.ToArray()).Select(filterFun)
		).D(d);

		var mergedUi = Util.HorizontalRun(true, filterUis);

		return new FltCombined<TFilter>(mergedVar, mergedUi);

	}
}
