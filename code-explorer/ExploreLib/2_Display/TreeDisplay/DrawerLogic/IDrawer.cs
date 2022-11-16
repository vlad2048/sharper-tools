using LINQPad.Controls;
using PowBasics.Geom;
using PowTrees.Algorithms;

namespace ExploreLib._2_Display.TreeDisplay.DrawerLogic;

public interface IDrawer
{
	R UpdateDims(R r);
	void Add(Control ctrl);
	void Link(Pt pt, string text, Action onClick, Action<Control>? opt = null);
	void DivText(R r, string text, Action<Control>? opt = null);
	void Grid();
	void Arrows(TNod<R> root);
}

static class LayoutExt
{
	public static Sz GetSz<T>(this TNod<T> nod, Func<T, Sz> szFun, Action<Algo_Layout.AlgoLayoutOpt>? optFun = null) =>
		nod
			.Layout(szFun, optFun)
			.GetRTree()
			.Select(e => e.V)
			.Union()
			.Size;
	
	public static TNod<R> GetRTree<T>(this Dictionary<TNod<T>, R> layout) =>
		layout
			.Keys.Single(e => e.Parent == null)
			.MapN(e => layout[e]);

	public static Dictionary<TNod<T>, R> Offset<T>(this Dictionary<TNod<T>, R> layout, Pt ofs) =>
		layout.ToDictionary(
			e => e.Key,
			e => e.Value + ofs
		);
}