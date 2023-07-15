namespace ExploreLib.Utils;

public static class EnumExt
{
	//public static U[] SelectToArray<T, U>(this IEnumerable<T> source, Func<T, U> fun) => source.Select(fun).ToArray();
	//public static T[] WhereToArray<T>(this IEnumerable<T> source, Func<T, bool> predicate) => source.Where(predicate).ToArray();
	public static U[] OfTypeToArray<T, U>(this IEnumerable<T> source) where U : T => source.OfType<U>().ToArray();
	public static T[] SelectManyToArray<T>(this IEnumerable<T[]> source) => source.SelectMany(e => e).ToArray();

	//public static string JoinText<T>(this IEnumerable<T> source, string separator = ";") => string.Join(separator, source);

	public static int SumOrZero<T>(this IEnumerable<T> source, Func<T, int> fun)
	{
		var sum = 0;
		foreach (var elt in source)
			sum += fun(elt);
		return sum;
	}
}