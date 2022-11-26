using ExploreLib.Utils;
using Mono.Cecil;
using Attr = Mono.Cecil.FieldAttributes;

namespace ExploreLib.Structs;

public class Typ
{
	private const Attr EnumValuesFieldAttributes = Attr.Public | Attr.Static | Attr.Literal | Attr.HasDefault;

	public TypeDefinition Def { get;  }

	public bool IsPublic => Def.IsPublic;
	public string Name { get;  }
	public TypKind Kind { get;  }
	public string FullName => Def.FullName;
	public int CodeSize { get; }

	public string[] EnumValues
	{
		get
		{
			if (Kind != TypKind.Enum) throw new ArgumentException();
			return Def.Fields
				.Where(e => e.Attributes == EnumValuesFieldAttributes)
				.Select(e => e.Name)
				.ToArray();
		}
	}

	public Typ(TypeDefinition def)
	{
		Def = def;
		Name = Def.GetName();
		Kind = Def.GetKind();
		CodeSize = Def.Methods
			.Where(e => e.Body != null)
			.SumOrZero(e => e.Body.CodeSize);
	}
}