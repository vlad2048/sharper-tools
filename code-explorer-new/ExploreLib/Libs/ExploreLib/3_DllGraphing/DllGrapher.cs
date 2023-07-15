using ExploreLib._2_DllReading;
using ExploreLib._2_DllReading.Structs;
using ExploreLib._3_DllGraphing.Structs;
using ExploreLib._3_DllGraphing.Utils;
using PowBasics.CollectionsExt;
using PowTrees.Algorithms;

namespace ExploreLib._3_DllGraphing;

public static class DllGrapher
{
	public static DllGraphs BuildGraphs(this TypeDef[] types) => new(
		BuildInterfaceRoots(types.WhereToArray(e => e.Kind == TypeKind.Interface)),
		BuildClassRoots(types.WhereToArray(e => e.Kind == TypeKind.Class)),
		BuildStructRoots(types.WhereToArray(e => e.Kind == TypeKind.Struct)),
		BuildStaticClassRoots(types.WhereToArray(e => e.Kind == TypeKind.StaticClass))
	);

	private static TNod<TypeNode<TypeDef>>[] BuildInterfaceRoots(TypeDef[] types)
	{
		var map = types.ToDictionary(e => e.FullName);
		return
			ForestMaker.Make(
					types,
					e => e.GetParentInterfaces(map),
					e => e with { }
				)
				//.Select(e => e.OrderByLongestBranchesFirst())
				.Select(e => e.ComputePrimaries())
				.Select(e => e.AddInterfacesNo(map))
				.ToArray();
	}

	private static TNod<TypeNode<TypeDef>>[] BuildClassRoots(TypeDef[] types)
	{
		var map = types.ToDictionary(e => e.FullName);
		return
			ForestMaker.Make(
					types,
					e => e.GetBaseClass(map),
					e => e with { }
				)
				//.Select(e => e.OrderByLongestBranchesFirst())
				.Select(e => e.ComputePrimaries())
				.Select(e => e.AddInterfacesYes(map))
				.ToArray();
	}

	private static TNod<TypeNode<TypeDef>>[] BuildStructRoots(TypeDef[] types)
	{
		var map = types.ToDictionary(e => e.FullName);
		return
			ForestMaker.Make(
					types,
					e => e.GetBaseClass(map),
					e => e with { }
				)
				//.Select(e => e.OrderByLongestBranchesFirst())
				.Select(e => e.ComputePrimaries())
				.Select(e => e.AddInterfacesYes(map))
				.ToArray();
	}

	private static TypeDef[] BuildStaticClassRoots(TypeDef[] types) => types.WhereToArray(e => e.Kind == TypeKind.StaticClass);
}



file static class DllTypeGraphBuilderExt
{
	public static TypeDef[] GetParentInterfaces(this TypeDef type, IReadOnlyDictionary<string, TypeDef> map) =>
		type.Ref.Interfaces
			.SelectWhereMap(e => e.GetInterfaceLookupFullName(), map)
			.ToArray();

	public static TypeDef[] GetBaseClass(this TypeDef type, IReadOnlyDictionary<string, TypeDef> map) =>
		map.TryGetValue(type.Ref.BaseType.GetTypeDefLookupFullName(), out var baseCls) switch
		{
			true => new[] { baseCls },
			false => Array.Empty<TypeDef>()
		};

	public static TNod<TypeDef> OrderByLongestBranchesFirst(this TNod<TypeDef> root) =>
		root.FoldRNSpec<TypeDef, TypeDef>((node, children) => Nod.Make(
			node.V,
			children
				.OrderByDescending(e => e.Count())
		));

	public static TNod<TypeNodePre<T>> ComputePrimaries<T>(this TNod<T> root)
	{
		var map = new HashSet<T>();
		NodeRole GetRole(T v)
		{
			if (map.Contains(v)) return NodeRole.Secondary;
			map.Add(v);
			return NodeRole.Primary;
		}

		return root
			.Map(e => new TypeNodePre<T>(e, GetRole(e)))
			.MapN(e => e.V with { Role = e.GoUp() })
			.Filter(e => e.Role != NodeRole.Secondary, opt => { }).First();
	}

	public static TNod<TypeNode<TypeDef>> AddInterfacesYes(this TNod<TypeNodePre<TypeDef>> root, IReadOnlyDictionary<string, TypeDef> map) => root.Map(n => new TypeNode<TypeDef>(n.Def, n.Role, n.Def.GetParentInterfaces(map)));
	public static TNod<TypeNode<TypeDef>> AddInterfacesNo(this TNod<TypeNodePre<TypeDef>> root, IReadOnlyDictionary<string, TypeDef> map) => root.Map(n => new TypeNode<TypeDef>(n.Def, n.Role, Array.Empty<TypeDef>()));


	private static NodeRole GoUp<T>(this TNod<TypeNodePre<T>> nod) => nod.V.Role switch
	{
		NodeRole.Primary => NodeRole.Primary,
		NodeRole.Secondary => (nod.Parent != null && nod.Parent.V.Role == NodeRole.Primary) switch
		{
			true => NodeRole.FirstSecondary,
			false => NodeRole.Secondary
		},
		_ => throw new ArgumentException()
	};


	//private static void Traverse<T>(this TNod<T> node, Action<T> action)
	//{
	//	foreach (var child in node.Children)
	//}


	private static TNod<U> FoldRNSpec<T, U>(
		this TNod<T> root,
		Func<TNod<T>, IReadOnlyList<TNod<U>>, TNod<U>> fun
	)
	{
		TNod<U> Recurse(TNod<T> node)
		{
			var foldedChildren = node.Children
				.Select(Recurse).ToArray();
			var foldedNode = fun(node, foldedChildren);
			return foldedNode;
		}
		return Recurse(root);
	}




	private static IEnumerable<V> SelectWhereMap<T, K, V>(this IEnumerable<T> source, Func<T, K> keyFun, IReadOnlyDictionary<K, V> map) where K : notnull =>
		source
			.Where(e => map.ContainsKey(keyFun(e)))
			.Select(e => map[keyFun(e)]);
}