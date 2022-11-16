namespace ExploreLib.NugetLogic.Structs.Refs;

public record PrjRef(string File) : IRef
{
	public string Name => Path.GetFileNameWithoutExtension(File);
	public string Folder = Path.GetDirectoryName(File)!;
}