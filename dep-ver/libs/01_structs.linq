<Query Kind="Program" />

void Main()
{
	
}

public record Sln(string Name, string Folder);

public record Proj(Sln Sln, string File)
{
	public string Name => Path.GetFileNameWithoutExtension(File);
	public override string ToString() => Name;
}
