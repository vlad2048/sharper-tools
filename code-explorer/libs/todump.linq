<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>LINQPadExtras</NuGetReference>
  <Namespace>ExploreLib.Structs</Namespace>
  <Namespace>NuGet.Frameworks</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>PowMaybe</Namespace>
</Query>

#load ".\api-nuget"


void Main()
{
}



public static object ToDump(object o)
{
	if (IsMaybeAndGetStr(o, out var str)) return str ?? "_";
	
	return o switch
	{
		Lib[] e => string.Join(", ", e.Select(e => e.CapitalizedName)),
		Typ e => e.Name,

		NuGetFramework e => e.GetShortFolderName(),
		NuGetVersion e => $"{e}",
		PackageIdentity e => $"[{e.Version}] {e.Id}",

		_ => o
	};
}


private static bool IsMaybeAndGetStr(object obj, out string? str)
{
	str = null;
	var t = obj.GetType();
	if (!t.IsGenericType || t.BaseType == null || !t.BaseType.GetGenericTypeDefinition().IsAssignableTo(typeof(Maybe<>))) return false;
	str = $"{obj}";
	return true;
}