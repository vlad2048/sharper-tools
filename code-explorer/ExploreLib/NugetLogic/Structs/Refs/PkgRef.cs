using NuGet.Versioning;

namespace ExploreLib.NugetLogic.Structs.Refs;

public record PkgRef(string Id, VersionRange VerRange) : IRef;