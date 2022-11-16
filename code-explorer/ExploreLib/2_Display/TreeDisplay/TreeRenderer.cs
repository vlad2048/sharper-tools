using ExploreLib._2_Display.TreeDisplay.DrawerLogic;
using ExploreLib._2_Display.Utils;
using LINQPad.Controls;

namespace ExploreLib._2_Display.TreeDisplay;

public static class TreeRenderer
{
	public static Control Make(Action<IDrawer> action)
	{
		var drawer = new Drawer();
		action(drawer);
		return new Div(drawer.Ctrls)
			.Set("position", "relative")
			.Set("width", $"{drawer.Size.Width.h()}")
			.Set("height", $"{drawer.Size.Height.v()}");
	}
}