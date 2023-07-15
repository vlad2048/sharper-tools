using ExploreLib._1_DllFinding;
using ExploreLib.UILogic;
using PowJson;

namespace ExploreLib.Utils;

static class PersistUtils
{
	public static void SaveDllCache(DllCache e) => JsonUtils.SaveFile(FileUtils.DllListCacheFile, e);
	public static DllCache LoadDllCache() => JsonUtils.LoadFile(FileUtils.DllListCacheFile, DllCache.Empty);

	public static void SaveUserPrefs(UserPrefs e) => JsonUtils.SaveFile(FileUtils.UserPrefsFile, e);
	public static UserPrefs LoadUserPrefs() => JsonUtils.LoadFile(FileUtils.UserPrefsFile, UserPrefs.Empty);
}