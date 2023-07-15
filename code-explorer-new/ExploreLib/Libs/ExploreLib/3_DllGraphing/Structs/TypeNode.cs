namespace ExploreLib._3_DllGraphing.Structs;

public enum NodeRole
{
	Primary,
	FirstSecondary,
	Secondary
}

public record TypeNodePre<T>(
	T Def,
	NodeRole Role
);

public record TypeNode<T>(
	T Def,
	NodeRole Role,
	T[] Interfaces
);
