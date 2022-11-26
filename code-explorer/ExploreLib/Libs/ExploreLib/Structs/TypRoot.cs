namespace ExploreLib.Structs;

public record TypRoot
{
	public TNod<Typ> Root { get; }
	public int CodeSize { get; }

	public TypRoot(TNod<Typ> root)
	{
		Root = root;
		CodeSize = Root.Sum(e => e.V.CodeSize);
	}
}