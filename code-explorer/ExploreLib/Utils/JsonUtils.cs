using System.Text.Json;
using System.Text.Json.Serialization;
using ExploreLib.NugetLogic.Structs.Refs;
using PowTrees.Serializer;
using NuGet.Versioning;

namespace ExploreLib.Utils;

static class JsonUtils
{
	private static readonly JsonSerializerOptions jsonOpt = new()
	{
		WriteIndented = true
	};
	static JsonUtils()
	{
		jsonOpt.Converters.Add(new TNodSerializer<IRef>());
		jsonOpt.Converters.Add(new RefSerializer());
		jsonOpt.Converters.Add(new NuGetVersionSerializer());
	}

	public static T LoadGen<T>(string file, Func<T> makeFun)
	{
		if (File.Exists(file)) return LoadJson<T>(file);
		var obj = makeFun();
		SaveJson(file, obj);
		return obj;
	}

	public static void SaveJson<T>(string file, T obj) => File.WriteAllText(file, JsonSerializer.Serialize(obj, jsonOpt));

	private static T LoadJson<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(file), jsonOpt)!;
}


class RefSerializer : JsonConverter<IRef>
{
	private enum RefType
	{
		Prj,
		Pkg
	}

	private record RefRec(
		RefType Type,
		string? PrjFile,
		string? PkgId,
		string? PkgVerRange
	);

	private static RefRec Ref2Rec(IRef e) => e switch
	{
		PrjRef f => new RefRec(RefType.Prj, f.File, null, null),
		PkgRef f => new RefRec(RefType.Pkg, null, f.Id, $"{f.VerRange}"),
		_ => throw new ArgumentException()
	};

	private static IRef Rec2Ref(RefRec e) => e.Type switch
	{
		RefType.Prj => new PrjRef(e.PrjFile!),
		RefType.Pkg => new PkgRef(e.PkgId!, VersionRange.Parse(e.PkgVerRange!)),
		_ => throw new ArgumentException()
	};

	public override IRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var serR = doc.Deserialize<RefRec>(options)!;
		return Rec2Ref(serR);
	}

	public override void Write(Utf8JsonWriter writer, IRef value, JsonSerializerOptions options)
	{
		var serR = Ref2Rec(value);
		JsonSerializer.Serialize(writer, serR, options);
	}
}


class NuGetVersionSerializer : JsonConverter<NuGetVersion>
{
	public override NuGetVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var serR = doc.Deserialize<string>(options)!;
		return NuGetVersion.Parse(serR);
	}

	public override void Write(Utf8JsonWriter writer, NuGetVersion value, JsonSerializerOptions options)
	{
		var serR = $"{value}";
		JsonSerializer.Serialize(writer, serR, options);
	}
}