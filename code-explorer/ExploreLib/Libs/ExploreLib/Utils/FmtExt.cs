namespace ExploreLib.Utils;

public static class FmtExt
{
	public static string FmtSize(this int e)
	{
		if (e < 1024) return $"{e} Bytes";
		if (e < 1024 * 1024) return $"{e / 1024.0:F1} KB";
		return $"{e / (1024.0 * 1024):F1} MB";		
	}
}