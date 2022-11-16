using NuGet.Versioning;

namespace ExploreLib.NugetLogic.Structs;

public record Pkg(string Id, NuGetVersion Ver);