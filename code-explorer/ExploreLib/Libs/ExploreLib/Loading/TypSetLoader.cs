using ExploreLib.Structs;
using Mono.Cecil;
using PowTrees.Algorithms;

namespace ExploreLib.Loading;

public static class TypSetLoader
{
	public static TypSet Load(this string dllFile)
	{
		var typs = LoadTyps(dllFile);
		var typMap = typs.ToDictionary(e => e.FullName, e => Nod.Make(e));
		foreach (var typ in typs)
		{
			var parentNods = GetTypParentNames(typ).Where(typMap.ContainsKey).Select(e => typMap[e]);
			var typNod = typMap[typ.FullName];
			foreach (var parentNod in parentNods)
				AddChildIfUnique(parentNod, typNod);
		}
		var roots = typMap
			.Values
			.Where(e => e.Parent == null)
			.Select(e => new TypRoot(e))
			.OrderByDescending(e => e.CodeSize)
			.ToArray();
		return new TypSet(roots);
	}


	

	private static Typ[] LoadTyps(string dllFile)
	{
		using var ass = AssemblyDefinition.ReadAssembly(dllFile);
		var module = ass.Modules.Single();
		return module.Types
			//.Where(e => e.IsPublic)
			.Where(IsInterestingType)
			.Select(def => new Typ(def))
			.ToArray();
	}


	private static void AddChildIfUnique(TNod<Typ> parent, TNod<Typ> child)
	{
		var root = parent.GoUpToRoot();
		var existingNods = root.ToHashSet();
		var isDuplicate = child.Any(existingNods.Contains);
		if (!isDuplicate)
			parent.AddChild(child);
	}


	private static string[] GetTypParentNames(Typ typ) =>
		(
			typ.Def.BaseType switch
			{
				null => Array.Empty<string>(),
				not null => new[] { typ.Def.BaseType.FullName }
			}
		)
		.Concat(
			typ.Def.Interfaces
				.Select(e => e.InterfaceType.FullName)
		)
		.ToArray();
	
	private static readonly HashSet<string> uninterestingTypes = new()
	{
		"<Module>",
		"EmbeddedAttribute",
		"NullableAttribute",
		"NullableContextAttribute",
	};
	private static bool IsInterestingType(TypeDefinition def) =>
		!(
			uninterestingTypes.Contains(def.Name) ||
			def.Name.StartsWith("<>f__AnonymousType")
		);
}