using ExploreLib._1_Structs;
using ExploreLib._2_Display;
using ExploreLib._2_Display.TreeDisplay;
using ExploreLib._2_Display.TreeDisplay.DrawerLogic;
using ExploreLib._2_Display.Utils;
using LINQPad.Controls;
using PowBasics.Geom;

namespace ExploreLib._3_Browsers._2_TypBrowse;

public static class TypRenderer
{
	public static Control Render(TypVisSet set, Action<Typ> onSel) =>
		set
			.Roots
			.Render(onSel);


	private static Control Render(this IEnumerable<TNod<TypVis>> rootsSource, Action<Typ> onSel)
	{
		var res = TreeRenderer.Make(draw =>
		{
			var roots = rootsSource.ToArray();

			var y = 0;
			var gutterSz = new Sz(3, 0);
			foreach (var root in roots)
			{
				var rootSz = draw.Tree(new Pt(0, y), root, typ => new Sz(typ.Typ.Name.Length, 1), (typ, r) =>
				{
					var color = ColsUtils.GetColForTypKind(typ.Typ.Kind);

					draw.Link(
						r.Pos,
						typ.Typ.Name,
						() =>
						{
							if (!typ.Visible) return;
							onSel(typ.Typ);
						},
						ctrl => ctrl
							.SetForeColor(color)
							.SetIf(!typ.Visible, "filter", Cols.InactiveTypeBrightness)
							.SetIf(!typ.Visible, "cursor", "default")
							.SetIf(!typ.Visible, "text-decoration", "none")
					);
				}, opt =>
				{
					opt.GutterSz = gutterSz;
					opt.AlignLevels = false;
				});
				y += rootSz.Height + 1;
			}
		});
		return res;
	}
}