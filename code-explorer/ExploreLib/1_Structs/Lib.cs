namespace ExploreLib._1_Structs;

public record Lib(string DllFile)
{
	public string Name => Path.GetFileNameWithoutExtension(DllFile);
}