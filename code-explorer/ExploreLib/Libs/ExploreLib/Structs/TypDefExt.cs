using Mono.Cecil;
using PowBasics.CollectionsExt;

namespace ExploreLib.Structs;

static class TypDefExt
{
	public static string GetName(this TypeDefinition def)
	{
		if (!def.HasGenericParameters) return def.Name;
		var parts = def.Name.Split('`');
		var baseName = parts[0];
		var genStr = def.GenericParameters.Select(e => e.FullName).JoinText(", ");
		return $"{baseName}<{genStr}>";
	}

	public static TypKind GetKind(this TypeDefinition def) => (def.IsInterface, def.IsClass, def.IsEnum, def.IsValueType) switch
	{
		(true, false, false, false) => TypKind.Interface,
		(false, true, false, false) => TypKind.Class,
		(false, true, false, true) => TypKind.Struct,
		(false, true, true, true) => TypKind.Enum,
		_ => throw new ArgumentException("Invalid TypeDef")
	};
}