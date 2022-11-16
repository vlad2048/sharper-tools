using ExploreLib._1_Structs;
using LINQPad.Controls;

namespace ExploreLib._3_Browsers._1_LibBrowse;

public static class LibRenderer
{
	public static Div Render(Lib[] libs, Action<Lib> onSel) => new(
		libs
			.Select(e => new Hyperlink(e.Name, _ =>
			{
				onSel(e);
			}))
	);
}
