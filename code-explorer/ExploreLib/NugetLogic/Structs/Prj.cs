using ExploreLib.NugetLogic.Structs.Refs;
using ExploreLib.NugetLogic.Utils;
using NuGet.Frameworks;

namespace ExploreLib.NugetLogic.Structs;

public record Prj(
	string File,
	NuGetFramework TargetFramework,
	PrjRef[] PrjRefs,
	PkgRef[] PkgRefs
)
{
	public string Name => Path.GetFileNameWithoutExtension(File);
	public string Folder => Path.GetDirectoryName(File)!;

	public static Prj Load(string file) => PrjUtils.Load(file);
}