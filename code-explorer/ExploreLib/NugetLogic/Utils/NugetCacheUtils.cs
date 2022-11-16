using ExploreLib.NugetLogic.Structs;
using ExploreLib.Utils;

namespace ExploreLib.NugetLogic.Utils;

static class NugetCacheUtils
{
	private record CacheEntry(
		DateTime PrjLastWriteTimestamp,
		DepTree Tree
	);

	public static DepTree GetGenDepTree(Prj prj, string cacheFolder, Func<DepTree> genFun)
	{
		var fileApi = new FileApi(cacheFolder);
		var file = fileApi.NugetDepTreeCacheFile;
		var cache = JsonUtils.LoadGen(file, () => new Dictionary<string, CacheEntry>());
		if (cache.TryGetDepTree(prj.File, out var depTree)) return depTree!;

		var time = new FileInfo(prj.File).LastWriteTime;
		depTree = genFun();
		cache[prj.File] = new CacheEntry(time, depTree);
		JsonUtils.SaveJson(file, cache);
		return depTree;
	}

	private static bool TryGetDepTree(
		this Dictionary<string, CacheEntry> cache,
		string prjFile,
		out DepTree? depTree
	)
	{
		depTree = null;
		if (!cache.TryGetValue(prjFile, out var cacheEntry)) return false;
		var timeCache = cacheEntry.PrjLastWriteTimestamp;
		var timeNow = new FileInfo(prjFile).LastWriteTime;
		if (timeNow > timeCache) return false;
		depTree = cacheEntry.Tree;
		return true;
	}

}