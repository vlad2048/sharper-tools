using Mono.Cecil;
using PowTrees.Algorithms;

namespace ExploreLib._1_Structs.Utils;

public static class LibExt
{
	public static TypSet LoadTypSet(this Lib lib)
	{
		var typs = LoadTyps(lib);
		var typMap = typs.ToDictionary(e => e.FullName, e => Nod.Make(e));
		foreach (var typ in typs)
		{
			var parentNods = GetTypParentNames(typ).Where(typMap.ContainsKey).Select(e => typMap[e]);
			var typNod = typMap[typ.FullName];
			foreach (var parentNod in parentNods)
				AddChildIfUnique(parentNod, typNod);
			//parentNod.AddChild(typNod);
		}
		var roots = typMap
			.Values
			.Where(e => e.Parent == null)
			.OrderByDescending(e => e.Count())
			.ToArray();
		return new TypSet(roots);
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

	private static Typ[] LoadTyps(Lib lib)
	{
		using var ass = AssemblyDefinition.ReadAssembly(lib.DllFile);
		var module = ass.Modules.Single();
		return module.Types
			.Where(e => e.IsPublic)
			.Where(IsInterestingType)
			.Select(def => new Typ(def))
			.ToArray();
	}
	
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