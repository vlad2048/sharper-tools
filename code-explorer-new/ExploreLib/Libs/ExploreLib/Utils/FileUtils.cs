namespace ExploreLib.Utils;

static class FileUtils
{
	public static string DllRootFolder => dllRootFolder.Value;
	public static string DllListCacheFile => Path.Combine(cacheFolder, "dll-list.json");
	public static string UserPrefsFile => Path.Combine(cacheFolder, "user-prefs.json");

	private static readonly Lazy<string> dllRootFolder = new(() => $@"C:\Users\{Environment.UserName}\.nuget\packages");
	private static readonly string cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "code-explorer").EnsureFolder();
	private static string EnsureFolder(this string f)
	{
		if (!Directory.Exists(f))
			Directory.CreateDirectory(f);
		return f;
	}
}