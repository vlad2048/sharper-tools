using ExploreLib.Utils;
using PowTrees.Algorithms;

namespace ExploreLib._1_Structs.Utils;

public static class TypSetUtils
{
	public static TypVisSet Filter(this TypSet set, string searchText) => new(
		set.Roots
			.Select(root => root.Map(typ => new TypVis(typ, StrSearchUtils.IsMatch(typ.Name, searchText))))
			.Where(root => root.Any(visNod => visNod.V.Visible))
			.ToArray()
	);
}