using ExploreLib._1_Structs.Enum;
using Mono.Cecil;
using PowBasics.CollectionsExt;

namespace ExploreLib._1_Structs;

public record TypVis(Typ Typ, bool Visible);

public record TypVisSet(TNod<TypVis>[] Roots);

public class Typ
{
	public TypeDefinition Def { get;  }

	public string Name { get;  }
	public TypKind Kind { get;  }
	public string FullName => Def.FullName;

	public Typ(TypeDefinition def)
	{
		Def = def;
		Name = Def.GetName();
		Kind = Def.GetKind();
	}
}


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