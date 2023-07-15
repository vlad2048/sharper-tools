using PowBasics.CollectionsExt;
using PowTrees.Algorithms;

namespace ExploreLib._3_DllGraphing.Utils;


static class ForestMaker
{
    public static TNod<T>[] Make<T>(
	    T[] types,
	    Func<T, T[]> parentsFun,
	    Func<T, T> cloneFun
	) where T : class
    {
        var map = new Dictionary<T, TNod<T>>();
        TNod<T> Get(T e) => map.GetOrCreate(e, () => Nod.Make(e));

        foreach (var type in types)
        {
            var nod = Get(type);
            var parents = parentsFun(type);
            foreach (var parent in parents)
                Get(parent).AddChild(nod);
        }

        return map.Values
            .Where(e => e.Parent == null)
            .Select(e => e.Map(cloneFun))
            .Select(e => e.RemoveTransitiveDependencies())
            .ToArray();
    }
}