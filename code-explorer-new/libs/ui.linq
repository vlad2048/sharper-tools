<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer-new\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>PowRxVar</NuGetReference>
  <NuGetReference>PowTrees.LINQPad</NuGetReference>
  <Namespace>ExploreLib._3_DllGraphing.Structs</Namespace>
  <Namespace>ExploreLib.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowBasics.Geom</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.LINQPad</Namespace>
  <Namespace>System.Reactive</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
  <Namespace>System.Reactive.Subjects</Namespace>
</Query>


using TabObj = System.Func<PowRxVar.Disp, (object, LINQPad.Controls.Div)>;

void Main()
{
	
}


public static Disp D => serD.Value!;
public static DumpContainer MouseDC = MakeMouseDC();

private static DumpContainer MakeMouseDC()
{
	var dc = new DumpContainer();
	var div = new Div(dc).Dump();
	div.Styles["position"] = "fixed";
	div.Styles["right"] = "5px";
	div.Styles["top"] = "2px";
	div.Styles["z-index"] = "200";
	div.Styles["color"] = "white";
	div.Styles["background-color"] = "#3336";
	div.Styles["width"] = "70px";
	div.Styles["height"] = "30px";
	return dc;
}




public static class ForestDrawExt
{
	private const int TreeGap = 1;

	private static int curNodeIdx;
	private static int curWrapIdx;
	
	public static (Div, IDisposable) DrawForest<T>(
		this TNod<T>[] roots,
		Func<T, Sz> szFun,
		Func<T, bool, Control> drawFun,
		Action<T> onClick
	)
	{
		var d = new Disp();
		var layouts = roots
			.SelectToArray(root => root.BuildLayout(
				szFun,
				opt =>
				{
					opt.GutterSz = new Sz(3, 0);
					opt.AlignLevels = true;
				})
			)
			.LayVertically(TreeGap);
		var totalSz = layouts.Select(e => e.BBox).Union().Size;

		/*layouts[0].Root
			.ToDictionary(e => e, e => e.V.R)
			.GetRTree();*/
		
		var idMap = (
			from layout in layouts
			from node in layout.Root
			select node
		)
			.Select((node, idx) => (node, idx))
			.ToDictionary(t => MkIdStr(curNodeIdx + t.idx), t => t.node);
		curNodeIdx += idMap.Count;
		
		var ctrlsArrows = layouts.Select(e => e.MakeArrows());
		var ctrlsNodes = idMap
			.SelectToArray(t =>
			{
				var id = t.Key;
				var layoutNode = t.Value;
				return drawFun(layoutNode.V.Node, false)
					.WithCssId(id)
					.StyleNode(layoutNode.V.R);
			});
		
		var wrapCtrl = new Div(ctrlsArrows.Concat(ctrlsNodes)).StyleWrapper(totalSz);

		var whenClick = TrackClicks(wrapCtrl, idMap).D(d);

		whenClick.Subscribe(e =>
		{
			//MouseDC.Content = $"{e.V.R}";
			onClick(e.V.Node);
		}).D(d);

		return (wrapCtrl, d);
	}

	private static string MkIdStr(int id) => $"fs-{id}";
	
	/*private static TNod<R> GetRTree<T>(this Dictionary<TNod<T>, R> layout) =>
		layout
			.Keys.Single(e => e.Parent == null)
			.MapN(e => layout[e]);*/

	private static (IObservable<T>, IDisposable) TrackClicks<T>(Control wrapCtrl, Dictionary<string, T> map)
	{
		var d = new Disp();
		var wrapId = $"wrapper-{curWrapIdx++}";
		wrapCtrl.WithCssId(wrapId);
		Util.HtmlHead.AddScript(@"
			function hookWrapper(wrapId) {
				const elt = document.getElementById(wrapId);
				document.addEventListener('click', evt => {
					const id = evt.target.id;
					if (!id.startsWith('fs-')) return;					
					elt.dispatchEvent(new CustomEvent('click', { detail: [ id ] }));
				});
			}
		");
		
		var whenClick = new Subject<T>().D(d);

		wrapCtrl.HtmlElement.InvokeScript(false, "eval", $"hookWrapper('{wrapId}')");
		
		void EvtHandler(object? sender, PropertyEventArgs args)
		{
			var id = args.Properties["detail"].Replace("[", "").Replace("]", "").Replace(@"""", "").Trim();
			if (!map.TryGetValue(id, out var item)) return;
			whenClick.OnNext(item);
		}
		wrapCtrl.HtmlElement.AddEventListener("click", evtProps, EvtHandler);
		Disposable.Create(() =>
		{
			wrapCtrl.HtmlElement.RemoveEventListener("click", evtProps, EvtHandler);
		}).D(d);
		
		return (
			whenClick
				.AsObservable()
				.Throttle(TimeSpan.FromMilliseconds(10)),
			d
		);
	}
	
	private static readonly string[] evtProps = { "detail" };
	
	
	/*private static int curIdx;
	private static (IRoVar<Maybe<string>>, IDisposable) TrackHtml(Control wrapCtrl, Control targetCtrl)
	{
		var d = new Disp();
		var curTargetId = Var.Make(May.None<string>()).D(d);
		var wrapId = $"wrapper-{curIdx}";
		var targetId = $"target-{curIdx}";
		curIdx++;
		wrapCtrl.WithCssId(wrapId);
		targetCtrl.WithCssId(targetId);
		Util.HtmlHead.AddScript(@"
			function hookWrapper(wrapId, targetId) {
				const elt = document.getElementById(wrapId);
				let curTargetId = '(none)';
				document.addEventListener('mousemove', evt => {
					
					const id = evt.target.id;
					if (!id.startsWith('fs-')) return;					
					if (id === curTargetId) return;
					curTargetId = id;
					elt.dispatchEvent(new CustomEvent('rightClick', { detail: [ id ] }));
					
				});
			}
		");

		wrapCtrl.HtmlElement.InvokeScript(false, "eval", $"hookWrapper('{wrapId}', '{targetId}')");
		void EvtHandler(object? sender, PropertyEventArgs args)
		{
			if (curTargetId.IsDisposed) return;
			var targetIdVal = args.Properties["detail"].Replace("[", "").Replace("]", "").Replace(@"""", "").Trim();
			curTargetId.V = targetIdVal == "(none)" ? May.None<string>() : May.Some(targetIdVal);
		}
		wrapCtrl.HtmlElement.AddEventListener("rightClick", new [] { "detail" }, EvtHandler);
		Disposable.Create(() => wrapCtrl.HtmlElement.RemoveEventListener("rightClick", new [] { "detail" }, EvtHandler)).D(d);
		
		return (curTargetId.ToReadOnly(), d);
	}*/
}



file static class ForestExt
{
	public static C StyleWrapper<C>(this C ctrl, Sz totalSz) where C : Control => ctrl
		.WithCss("position", "relative")
		.WithCss("width", $"{totalSz.Width.h()}")
		.WithCss("height", $"{totalSz.Height.v()}");
		
	public static C StyleNode<C>(this C ctrl, R r) where C : Control => ctrl
		.WithCssClass("node-cls")
		.WithCss("margin", "5px")
		.WithCss("position", "absolute")
		.WithCss("background-color", "#1e1e1e")
		.WithCss("line-height", $"{r.Height.v()}")
		.StyleSetR(r);
	
	public static C StyleSetR<C>(this C ctrl, R r) where C : Control => ctrl
		.WithCss("left", $"{r.X.h()}")
		.WithCss("top", $"{r.Y.v()}")
		.WithCss("width", $"{r.Width.h()}")
		.WithCss("height", $"{r.Height.v()}");

	private static string h(this int v) => $"{v}ch";
	private static string v(this int v) => $"{v}em";
	private static string h(this double v) => $"{v}ch";
	private static string v(this double v) => $"{v}em";
}






public interface IDisplayList<T>
{
	IDisplayList<T> WithOrdering(params Expression<Func<T, object>>[] orderExprs);
	IDisplayList<T> WithSearch(Func<T, string> strFun, IRwVar<string>? pSearchPersistVar = null);
	IDisplayList<T> WithPaging(int pageSize);
	object Build(Disp d);
}


public static class UI
{
	class DisplayListImpl<T> : IDisplayList<T>
	{
		private readonly IRoVar<T[]> source;
		private readonly Func<T, object> dispFun;

		private enum OrdDir { Asc, Desc }
		private class Ord
		{
			public bool IsNone { get; }
			public string Name { get; }
			public OrdDir Dir { get; }
			public Func<T, object> Fun { get; }
			public Ord(Expression<Func<T, object>> expr, OrdDir dir)
			{
				IsNone = false;
				Name = ExprTreeUtils.GetName(expr);
				Fun = ExprTreeUtils.CompileGetter(expr);
				Dir = dir;
			}
			public Ord()
			{
				IsNone = true;
				Name = "None";
				Dir = OrdDir.Asc;
				Fun = null!;				
			}
			public override string ToString() => IsNone switch
			{
				true => "None",
				false => $"{Name} " + $"{Dir}".ToLower(),
			};
			public IEnumerable<T> Order(IEnumerable<T> source) => IsNone switch
			{
				true => source,
				false => Dir switch
				{
					OrdDir.Asc => source.OrderBy(Fun),
					OrdDir.Desc => source.OrderByDescending(Fun),
					_ => throw new ArgumentException()
				}
			};
		}
		private Ord[]? orderings;
		
		private Func<T, string>? searchFun;
		private IRwVar<string>? searchPersistVar;
		
		private int? pageSize;
		
		
		public DisplayListImpl(IRoVar<T[]> source, Func<T, object> dispFun)
		{
			this.source = source;
			this.dispFun = dispFun;
		}

		public IDisplayList<T> WithOrdering(params Expression<Func<T, object>>[] orderExprs)
		{
			orderings = orderExprs
				.SelectMany(e => new [] {
					new Ord(e, OrdDir.Asc),
					new Ord(e, OrdDir.Desc)
				})
				.Prepend(new Ord())
				.ToArray();
			return this;
		}
		public IDisplayList<T> WithSearch(Func<T, string> pSearchFun, IRwVar<string>? pSearchPersistVar = null) { searchFun = pSearchFun; searchPersistVar = pSearchPersistVar; return this; }
		public IDisplayList<T> WithPaging(int pPageSize) { pageSize = pPageSize; return this; }
		
		public object Build(Disp d)
		{
			var uis = new List<Control>();
			IRoVar<T[]> list;
			
			if (orderings != null)
			{
				var idx = orderings.Length > 2 ? 2 : 0;
				var selectedOrdering = Var.Make(orderings[idx]).D(d);
				uis.Add(new Span(
					new Span("order:"),
					new SelectBox(orderings, idx, c => selectedOrdering.V = orderings[c.SelectedIndex])
				).WithHorizGap(5));
				list = Var.Expr(() => selectedOrdering.V.Order(source.V).ToArray());
			}
			else
			{
				list = source;
			}
			
			if (searchFun != null)
			{
				var (searchText, searchUI) = SearchBox(searchPersistVar).D(d);
				uis.Add(searchUI);
				list = list.FilterOn(searchText, searchFun);
			}
			
			if (pageSize.HasValue)
			{
				var pageSizeVal = pageSize.Value;
				var pageCount = list.SelectVar(items => CalcPageCount(items.Length, pageSizeVal));
				var pageIndex = Var.Make(0).D(d);
				pageCount.Subscribe(e => { if (pageIndex.V >= e) pageIndex.V = e - 1; }).D(d);
				var obs = Observable.Merge(pageIndex.ToUnit(), pageCount.ToUnit());
				uis.Add(new Span(
					new Span().React(obs, (c, _) => { c.Text = $"page: {pageIndex.V + 1}/{pageCount.V}"; }).D(d),
					new Button("-", _ => pageIndex.V--).React(obs, (c, _) => c.Enabled = pageIndex.V > 0).D(d),
					new Button("+", _ => pageIndex.V++).React(obs, (c, _) => c.Enabled = pageIndex.V < pageCount.V - 1).D(d)
				).WithHorizGap(5));
				list = list.Page(pageIndex, pageSizeVal);
			}
			
			var ctrlsUI = new Span(uis.ToArray()).WithHorizGap(10).WithHorizPad(5);
			var listUI = list
				.SelectVar(items => items.Select(dispFun))
				.ToDynaDC().D(d);
			return Util.VerticalRun(
				ctrlsUI,
				listUI
			);
		}
		
		private static int CalcPageCount(int total, int pageSize) => total switch
		{
			0 => 1,
			_ => Math.Max(1, ((total - 1) / pageSize) + 1)
		};
	}
	
	public static IDisplayList<T> DisplayList<T>(this IRoVar<T[]> source, Func<T, object> dispFun) => new DisplayListImpl<T>(source, dispFun);
	public static IDisplayList<T> DisplayList<T>(this IEnumerable<T> source, Func<T, object> dispFun) => DisplayList(Var.Make(source.ToArray(), Observable.Never<T[]>()).D(D), dispFun);
	
	
	public static object WithHeader(this object obj, string title) => Util.VerticalRun(
		Util.RawHtml($"<h1>{title}</h1>"),
		obj
	);
	
	public static DumpContainer ToDC(this object obj) => new(obj);
	
	public static (DumpContainer, IDisposable) ToDynaDC(this IRoVar<object> rxUI)
	{
		var d = new Disp();
		var dc = new DumpContainer();
		rxUI.Subscribe(ui => dc.UpdateContent(ui)).D(d);
		return (dc, d);
	}
	
	public static Hyperlink Link(string text, Action action) => new Hyperlink(text, _ => action()).MultiThread();
	
	public static (IRoVar<string>, Control, IDisposable) SearchBox(IRwVar<string>? persistVar = null)
	{
		var d = new Disp();
		var rxV = Var.Make(persistVar != null ? persistVar.V : string.Empty).D(d);
		var ui = new Span(
			new Span("Search: "),
			new TextBox(rxV.V, onTextInput: c => rxV.V = c.Text).WithCss("max-width", "150px")
		);
		rxV.Skip(1).PipeTo(persistVar).D(d);
		return (rxV, ui, d);
	}
	
	public static Div TabsHeader(string title, Action? onBack)
	{
		Hyperlink MkBackBtn()
		{
			var btn = new Hyperlink("Back", _ => onBack?.Invoke());
			btn.Styles["position"] = "absolute";
			btn.Styles["left"] = "5px";
			return btn;
		}
		Span MkTitleSpan() => new(title);
		var divChildren = onBack switch
		{
			null => new Control[] { MkTitleSpan() },
			not null => new Control[] { MkBackBtn(), MkTitleSpan() },
		};
		var div = new Div(divChildren);
		div.Styles["width"] = "100%";
		div.Styles["background-color"] = "#2e37a6";
		div.Styles["text-align"] = "center";
		div.Styles["padding"] = "5px 0";
		div.Styles["font-size"] = "22px";
		div.Styles["box-shadow"] = "0px 5px 15px 5px #000000";
		div.Styles["position"] = "relative";
		return div;
	}
	
	public static object Tabs(out IRwVar<TabObj> tab, TabObj initialTab)
	{
		var vTab = tab = Var.Make(initialTab);
		var tabSerD = new SerialDisp<Disp>().D(D);
		var headerDC = new DumpContainer();
		var tabDC = new DumpContainer();
		tabDC.Style = "padding: 10px 5px 0px 5px";
		
		var errorUI = new DumpContainer();
		var ui = Util.VerticalRun(
			errorUI,
			headerDC,
			tabDC
		);

		tab.Subscribe(tabObj =>
		{
			tabSerD.Value = null;
			var tabD = tabSerD.Value = new Disp();
			//try
			//{
				var (tabUI, headerUI) = tabObj(tabD);
				headerDC.UpdateContent(headerUI);
				tabDC.UpdateContent(tabUI);
			//}
			//catch (Exception ex)
			//{
			//	errorUI.Content = ex;
			//}
		}).D(D);
		
		return ui;
	}
}

public static class UIExt
{
	public static (C, IDisposable) React<C, T>(this C c, IObservable<T> rxV, Action<C, T> action) where C : Control
	{
		var d = new Disp();
		rxV.Subscribe(v => action(c, v)).D(d);
		return (c, d);
	}
	public static C MultiThread<C>(this C c) where C : Control
	{
		c.IsMultithreaded = true;
		return c;
	}
}


public static class RxExt
{
	public static bool IsInArray<T>(this IRoVar<T[]> v, T e) => v.V.Contains(e);
	public static void AddToArray<T>(this IRwVar<T[]> v, T e) => v.V = v.V.Append(e).ToArray();
	public static void RemoveFromArray<T>(this IRwVar<T[]> v, T e) { var list = v.V.ToList(); list.Remove(e); v.V = list.ToArray(); }
	
	public static IRoVar<T[]> FilterOn<T>(this IRoVar<T[]> list, IRoVar<string> searchText, Func<T, string> searchFun) => Var.Expr(() => list.V.WhereToArray(e => StringSearchUtils.IsMatch(searchFun(e), searchText.V)));
	public static IRoVar<T[]> Page<T>(this IRoVar<T[]> list, IRoVar<int> pageIndex, int pageSize) => Var.Expr(() => list.V.Skip(pageIndex.V * pageSize).Take(pageSize).ToArray());
}

public static class StringSearchUtils
{
	public static bool IsMatch(string itemStr, string searchStr) =>
		searchStr
			.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.All(part => itemStr.Contains(part, StringComparison.InvariantCultureIgnoreCase));
}

file static class StyleExt
{
	public static C WithHorizGap<C>(this C ctrl, int gap) where C : Control
	{
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["column-gap"] = $"{gap}px";
		ctrl.Styles["align-items"] = "center";
		return ctrl;
	}
	public static C WithHorizPad<C>(this C ctrl, int pad) where C : Control
	{
		ctrl.Styles["padding"] = $"0px {pad}px";
		return ctrl;
	}
}

public static class PublicStyleExt
{
	public static C WithCss<C>(this C ctrl, string propKey, string propVal) where C : Control
	{
		ctrl.Styles[propKey] = propVal;
		return ctrl;
	}
	
	public static C WithCss<C>(this C ctrl, string propKey, string propVal, bool cond) where C : Control => cond switch
	{
		true => ctrl.WithCss(propKey, propVal),
		false => ctrl
	};
	
	public static C WithCssClass<C>(this C ctrl, string cls) where C : Control
	{
		ctrl.CssClass = cls;
		return ctrl;
	}
	
	public static C WithCssId<C>(this C ctrl, string id) where C : Control
	{
		ctrl.HtmlElement.ID = id;
		return ctrl;
	}
}


private static readonly SerialDisp<Disp> serD = new();
void OnStart()
{
	Util.HtmlHead.AddStyles(@"
		html, body, #final {
			height: 100%;
			padding: 0;
			margin: 0;
		}
		a {
			font-family: unset;
		}
		body {
			font-family: consolas;
		}
		thead>tr:first-of-type { display: none; }
		
		.node-cls {
			position: absolute;
			cursor: pointer;
		}
	");
	serD.Value = null;
	serD.Value = new Disp();
}















