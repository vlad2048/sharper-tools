using ExploreLib.NugetLogic.Structs.Refs;

namespace ExploreLib.NugetLogic.Structs;

public record DepTree(
	TNod<IRef> Root,
	Dictionary<string, Pkg> ResolveMap
);