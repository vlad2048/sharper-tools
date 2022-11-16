namespace ExploreLib.Utils;

public static class StrSearchUtils
{
	public static bool IsMatch(string itemStr, string searchStr) =>
		searchStr
			.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.All(part => itemStr.Contains(part, StringComparison.InvariantCultureIgnoreCase));
}