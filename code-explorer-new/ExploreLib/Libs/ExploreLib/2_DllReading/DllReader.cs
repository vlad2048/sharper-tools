using ExploreLib._1_DllFinding.Structs;
using ExploreLib._2_DllReading.Structs;
using ExploreLib.Utils;
using Mono.Cecil;
using Mono.Collections.Generic;
using PowBasics.CollectionsExt;
using FieldAttr = Mono.Cecil.FieldAttributes;
using TypeAttr = Mono.Cecil.TypeAttributes;

namespace ExploreLib._2_DllReading;

public static class DllReader
{
	public static TypeDef[] Read(this DllNfo dll)
	{
		using var ass = AssemblyDefinition.ReadAssembly(dll.File);
		var module = ass.MainModule;
		var types = module.Types
			.Where(IsTypeInteresting)
			.SelectToArray(ReadType);
		return types;
	}

	private static TypeDef ReadType(TypeDefinition def)
	{
		var name = def.GetName();
		var kind = def.GetKind();
		var methodsToIgnore = def.GetMethodsToIgnore();
		IEnumerable<MemberDef>[] members =
		{
			SafeReadArr(() => def.Fields).Select(ReadField),
			SafeReadArr(() => def.Properties).Select(ReadProperty),
			SafeReadArr(() => def.Methods).WhereNot(methodsToIgnore.Contains).Select(ReadMethod),
		};
		
		return new TypeDef(
			def,
			name,
			kind,
			def.GetTypeDefLookupFullName(),
			members.SelectMany(e => e).ToArray(),
			def.Methods.Where(e => e.HasBody).SumOrZero(e => e.Body.CodeSize)
		);
	}

	private const FieldAttr EnumValuesFieldAttributes = FieldAttr.Public | FieldAttr.Static | FieldAttr.Literal | FieldAttr.HasDefault;

	private static FieldDef ReadField(FieldDefinition def) => new(
		def.Name,
		def.FieldType.GetTypeName(),
		def.Attributes == EnumValuesFieldAttributes
	);

	private static PropertyDef ReadProperty(PropertyDefinition def) => new(
		def.Name,
		def.PropertyType.GetTypeName(),
		def.GetPropertyMethods()
	);

	private static MethodDef ReadMethod(MethodDefinition def) => new(
		def.Name,
		def.ReturnType.GetTypeName(),
		def.Parameters.SelectToArray(ReadMethodParam),
		def.HasBody ? def.Body.CodeSize : 0
	);

	private static MethodParamDef ReadMethodParam(ParameterDefinition def) => new(
		def.Name,
		def.ParameterType.GetTypeName()
	);

	private const string InitMethodName = "modreq(System.Runtime.CompilerServices.IsExternalInit)";
	private static PropertyMethods GetPropertyMethods(this PropertyDefinition def)
	{
		PropertyMethods res = 0;
		if (def.GetMethod != null) res |= PropertyMethods.Getter;
		if (def.SetMethod != null)
		{
			if (def.SetMethod.Name.Contains(InitMethodName))
				res |= PropertyMethods.Init;
			else
				res |= PropertyMethods.Setter;
		}
		return res;
	}

	private static HashSet<MethodDefinition> GetMethodsToIgnore(this TypeDefinition def)
	{
		static IEnumerable<MethodDefinition> ForProp(PropertyDefinition p)
		{
			var list = new List<MethodDefinition>();
			if (p.GetMethod != null) list.Add(p.GetMethod);
			if (p.SetMethod != null) list.Add(p.SetMethod);
			if (p.HasOtherMethods) list.AddRange(p.OtherMethods);
			return list;
		}

		return (
			from prop in def.Properties
			from meth in ForProp(prop)
			select meth
		).ToHashSet();
	}

	private static string GetName(this TypeReference def)
	{
		if (!def.HasGenericParameters) return def.Name;
		var parts = def.Name.Split('`');
		var baseName = parts[0];
		var genStr = def.GenericParameters.Select(e => e.FullName).JoinText(", ");
		return $"{baseName}<{genStr}>";
	}

	private const TypeAttr TypeAttrsForStaticClasses = TypeAttr.Abstract | TypeAttr.Sealed;

	private static TypeKind GetKind(this TypeDefinition def) => (def.IsInterface, def.IsClass, def.IsEnum, def.IsValueType) switch
	{

		(true, false, false, false) => TypeKind.Interface,
		(false, true, false, false) => def.Attributes.HasFlag(TypeAttrsForStaticClasses) ? TypeKind.StaticClass : TypeKind.Class,
		(false, true, false, true) => TypeKind.Struct,
		(false, true, true, true) => TypeKind.Enum,
		_ => throw new ArgumentException("Invalid TypeDefinition")
	};


	private static readonly HashSet<string> uninterestingTypes = new()
	{
		"<Module>",
		"EmbeddedAttribute",
		"NullableAttribute",
		"NullableContextAttribute",
	};
	private static bool IsTypeInteresting(TypeDefinition def) =>
		!(
			uninterestingTypes.Contains(def.Name) ||
			def.Name.StartsWith("<>f__AnonymousType")
		);

	private static T[] SafeReadArr<T>(Func<Collection<T>> collFun)
	{
		try
		{
			return collFun().ToArray();
		}
		catch (Exception)
		{
			return Array.Empty<T>();
		}
	}


	internal static string GetTypeDefLookupFullName(this TypeReference def) => def.IsGenericInstance switch
	{
		false => def.FullName,
		true => def.GetElementType().FullName,
	};

	/*internal static string GetTypeDefLookupFullName(this TypeReference def) => (def.IsGenericInstance || def.HasGenericParameters) switch
	{
		false => def.FullName,
		true => def.GetElementType().FullName,
	};*/

	internal static string GetInterfaceLookupFullName(this InterfaceImplementation impl) => impl.InterfaceType.GetTypeDefLookupFullName();

	private static string GetTypeName(this TypeReference def) =>
		def.FullName.Simplify();



	private static string Simplify(this string s) => s
		//.Replace("System.", "")
		//.Replace("DynamicData.", "")
		//.Replace("Collections.Generic.", "")
		//.Replace("Linq.Expressions.", "")
		//.Replace("Reactive.Concurrency.", "")
		//.Replace("Binding.", "")
		.KeepLastPartOnly("<>")
		.Replace("`1", "")
		.Replace("`2", "")
		.Replace("`3", "")
		.Replace("`4", "")
		.Replace("Nullable<TimeSpan>", "TimeSpan?")
		.Replace("Void", "void")
		.Replace("Boolean", "bool")
		.Replace("Double", "double")
		.Replace("Single", "float")
		.Replace("Decimal", "decimal")
		.Replace("Int64", "long")
		.Replace("UInt64", "ulong")
		.Replace("Int32", "int")
		.Replace("UInt32", "uint")
		.Replace("Int16", "short")
		.Replace("UInt16", "ushort")
		.Replace("Byte", "byte")
		.Replace("Char", "char")
		.Replace("ObservableCacheEx::", "")
	;
	
	private static string KeepLastPartOnly(this string s, string seps)
	{
		foreach (var ch in seps)
			s = s.KeepLastPartOnly(ch);
		return s;
	}
	private static string KeepLastPartOnly(this string s, char sep) => string.Join(sep, s.Split(sep).Select(e => e.KeepLastPartOnly()));
	private static string KeepLastPartOnly(this string s) => s.Split('.')[^1];
}