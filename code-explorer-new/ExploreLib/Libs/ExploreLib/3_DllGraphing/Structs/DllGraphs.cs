using ExploreLib._2_DllReading.Structs;

namespace ExploreLib._3_DllGraphing.Structs;

public class DllGraphs
{
	public TNod<TypeNode<TypeDef>>[] InterfaceRoots { get; }
	public TNod<TypeNode<TypeDef>>[] ClassRoots { get; }
	public TNod<TypeNode<TypeDef>>[] StructRoots { get; }
	public TypeDef[] StaticClasses { get; }

	public DllGraphs(
		TNod<TypeNode<TypeDef>>[] interfaceRoots,
		TNod<TypeNode<TypeDef>>[] classRoots,
		TNod<TypeNode<TypeDef>>[] structRoots,
		TypeDef[] staticClasses
	)
	{
		InterfaceRoots = interfaceRoots;
		ClassRoots = classRoots;
		StructRoots = structRoots;
		StaticClasses = staticClasses;
	}
}