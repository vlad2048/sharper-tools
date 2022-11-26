namespace ExploreLib.Utils;

public static class EnumExt
{
	public static int SumOrZero<T>(this IEnumerable<T> source, Func<T, int> fun)
	{
		var sum = 0;
		foreach (var elt in source)
			sum += fun(elt);
		return sum;
	}
}