namespace ExploreLib._1_DllFinding.Structs;

public record DllNfo(
	string Name,
	Version Ver,
	string File,
	long FileSize
)
{
	public static DllNfo FromFile(string file) => new(
		Path.GetFileNameWithoutExtension(file),
		Version.Parse("1.0.0"),
		file,
		new FileInfo(file).Length
	);
}