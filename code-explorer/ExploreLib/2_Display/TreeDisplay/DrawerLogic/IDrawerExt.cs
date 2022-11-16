using PowBasics.Geom;
using PowTrees.Algorithms;

namespace ExploreLib._2_Display.TreeDisplay.DrawerLogic;

public static class IDrawerExt
{
	public static Sz Tree<T>(
		this IDrawer draw,
		Pt pt,
		TNod<T> root,
		Func<T, Sz> szFun,
		Action<T, R> drawFun,
		Action<Algo_Layout.AlgoLayoutOpt>? optFun = null
	)
	{
		var layout = root
			.Layout(szFun, optFun)
			.Offset(pt);
		foreach (var (nod, r) in layout)
			drawFun(nod.V, r);
		draw.Arrows(layout.GetRTree());
		return root.GetSz(szFun, optFun);
	}
}