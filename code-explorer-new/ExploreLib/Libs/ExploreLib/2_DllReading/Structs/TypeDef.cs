using Mono.Cecil;
using PowBasics.CollectionsExt;

namespace ExploreLib._2_DllReading.Structs;

public record MemberDef(string Name);

public record FieldDef(string Name, string Type, bool IsEnum) : MemberDef(Name);

[Flags]
public enum PropertyMethods
{
	Getter = 1,
	Setter = 2,
	Init = 4
}
public record PropertyDef(string Name, string Type, PropertyMethods Methods) : MemberDef(Name);

public record MethodParamDef(string Name, string Type);
public record MethodDef(string Name, string ReturnType, MethodParamDef[] Params, int CodeSize) : MemberDef(Name);

public enum TypeKind
{
	Interface,
	Class,
	Struct,
	StaticClass,
	Enum
}

public record TypeDef(
	TypeDefinition Ref,
	string Name,
	TypeKind Kind,
	string FullName,
	MemberDef[] Members,
	int CodeSize
)
{
	public string[] GetEnumValues() => Members.OfType<FieldDef>().Where(e => e.IsEnum).SelectToArray(e => e.Name);

	public virtual bool Equals(TypeDef? other) => other switch
	{
		null => false,
		not null => other.FullName == FullName
	};
	public override int GetHashCode() => FullName.GetHashCode();
}