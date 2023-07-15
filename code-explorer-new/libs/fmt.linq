<Query Kind="Program">
  <Reference>C:\Dev\sharper-tools\code-explorer-new\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>Mono.Cecil</NuGetReference>
  <Namespace>ExploreLib._2_DllReading.Structs</Namespace>
  <Namespace>ExploreLib.Utils</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

void Main()
{
	
}

public static class GfxExt
{
	public static IGfx[] GetGfxs(this TypeDef def)
	{
		var list = new List<IGfx>();
		list.AddRange(def.Members.OfType<FieldDef>().Select(e => new FieldGfx(e)));
		list.AddRange(def.Members.OfType<PropertyDef>().Select(e => new PropGfx(e)));
		list.AddRange(def.Members.OfType<MethodDef>().Select(e => new MethodGfx(e)));
		return list.ToArray();
	}
}

public interface IGfx
{
	string Text { get; }
}

public class FieldGfx : IGfx
{
	public readonly Div DivType;
	public readonly Div DivName;
	public string Text { get; }
	
	public FieldGfx(FieldDef def)
	{
		DivType = new Div(
			C.MkSpan(def.Type, C.ColRet)
		);
		DivName = new Div(
			C.MkSpan(def.Name, C.ColName)
		);
		Text = $"{def.Type} {def.Name}";
	}
}

public class PropGfx : IGfx
{
	public readonly Div DivType;
	public readonly Div DivName;
	public readonly Div DivMethods;
	public string Text { get; }
	
	public PropGfx(PropertyDef def)
	{
		DivType = new Div(
			C.MkSpan(def.Type, C.ColRet)
		);
		DivName = new Div(
			C.MkSpan(def.Name, C.ColName)
		);
		DivMethods = new Div(
			C.MkSpan(GetMethodsStr(def.Methods), C.ColParamType)
		);
		Text = $"{def.Type} {def.Name}";
	}
	
	private static string GetMethodsStr(PropertyMethods m)
	{
		var list = new List<string>();
		if (m.HasFlag(PropertyMethods.Getter)) list.Add("get;");
		if (m.HasFlag(PropertyMethods.Setter)) list.Add("set;");
		if (m.HasFlag(PropertyMethods.Init)) list.Add("init;");
		return "{ " + list.JoinText(" ") + " }";
	}
}

public class MethodGfx : IGfx
{
	public readonly Div DivRet;
	public readonly Div DivName;
	public readonly Div DivParams;
	public string Text { get; }
	
	public MethodGfx(MethodDef def)
	{
		DivRet = new Div(
			C.MkSpan(def.ReturnType, C.ColRet)
		);
		DivName = new Div(
			C.MkSpan(def.Name, C.ColName)
		);
		DivParams = new Div(
			def.Params
				.Select(e => C.A(
					C.MkSpan(e.Type + " ", C.ColParamType),
					C.MkSpan(e.Name, C.ColParamName)
				))
				.Intercalate(C.A( C.MkSpan(", ", C.ColOff) ))
				.Prepend(C.A( C.MkSpan("(", C.ColOff) ))
				.Append(C.A( C.MkSpan(")", C.ColOff) ))
				.SelectManyToArray()
		);
		Text = $"{def.ReturnType} {def.Name} {def.Params.Select(e => e.Type).JoinText(" ")}";
	}
}


file static class C
{
	public const string ColRet = "#C14071";
	public const string ColName = "#52FE7F";
	public const string ColOff = "#8F8F8F";
	public const string ColParamType = "#5EAAF5";
	public const string ColParamName = "#BDBDBD";
	
	public static T[] A<T>(params T[] arr) => arr;
	
	public static Span MkSpan(string content, string color)
	{
		var span = new Span(content);
		span.Styles["color"] = color;
		return span;
	}
	
	public static T[] Intercalate<T>(this IEnumerable<T> source, T elt)
	{
		var srcList = source.ToList();
		var dstList = new List<T>();
		for (var i = 0; i < srcList.Count; i++)
		{
			if (i > 0)
				dstList.Add(elt);
			dstList.Add(srcList[i]);
		}
		return dstList.ToArray();
	}
}
