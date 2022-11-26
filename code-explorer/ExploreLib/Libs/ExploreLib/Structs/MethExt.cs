using Mono.Cecil;
using PowBasics.CollectionsExt;

namespace ExploreLib.Structs;

static class MethExt
{
	public static string GetName(this MethodDefinition def) => def.Name.Simplify();
	public static string GetRet(this MethodDefinition def) => def.ReturnType.FullName.Simplify();
	public static MethParam[] GetParams(this MethodDefinition def) => def.Parameters.SelectToArray(e => new MethParam(
		e.Name,
		Simplify(e.ParameterType.FullName)
	));

	public static (string, string) GetParamsAndFullStr(this Meth meth)
	{
		var paramsStr = string.Join(", ", meth.Params.Select(e => $"{e.Type} {e.Name}"));
		var fullStr = $"{meth.Ret} {meth.Name} {paramsStr}";
		return (paramsStr, fullStr);
	}


	private static string Simplify(this string s) => s
		.KeepLastPartOnly("<>")
		.Replace("`1", "")
		.Replace("`2", "")
		.Replace("`3", "")
		.Replace("`4", "")
		.Replace("Nullable<TimeSpan>", "TimeSpan?")
		.Replace("Void", "void")
		.Replace("Boolean", "bool")
		.Replace("Double", "double")
		.Replace("Single", "float")
		.Replace("Decimal", "decimal")
		.Replace("Int64", "long")
		.Replace("UInt64", "ulong")
		.Replace("Int32", "int")
		.Replace("UInt32", "uint")
		.Replace("Int16", "short")
		.Replace("UInt16", "ushort")
		.Replace("Byte", "byte")
		.Replace("Char", "char")
		.Replace("ObservableCacheEx::", "")
	;

	private static string KeepLastPartOnly(this string s, string seps)
	{
		foreach (var ch in seps)
			s = s.KeepLastPartOnly(ch);
		return s;
	}
	private static string KeepLastPartOnly(this string s, char sep) => string.Join(sep, s.Split(sep).Select(e => e.KeepLastPartOnly()));
	private static string KeepLastPartOnly(this string s) => s.Split('.')[^1];
}