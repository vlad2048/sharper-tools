<Query Kind="Program">
  <NuGetReference>CliWrap</NuGetReference>
  <NuGetReference>Mono.Cecil</NuGetReference>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>CliWrap</Namespace>
  <Namespace>CliWrap.Buffered</Namespace>
</Query>

#load ".\01_structs"
#load ".\02_lowlevel"


void Main()
{
	
}




public record PkgId(string Name, Version PkgVer)
{
	public override string ToString() => $"{Name}@{PkgVer}";
	internal static PkgId Parse(string s)
	{
		var parts = s.Split('@');
		return new PkgId(parts[0], Version.Parse(parts[1]));
	}
}
public record PkgNfo(Version AssVer, PkgId[] Deps);


public class Cache
{
	public Dictionary<PkgId, PkgNfo> Map { get; set; } = null!;
}


static class CacheUtils
{
	public static Dictionary<PkgId, PkgNfo> CacheMap => Cache.Map;
	
	public static Version? GetAssVer(string pkgName, Version pkgVer)
	{
		var pkgId = new PkgId(pkgName, pkgVer);
		if (!CacheMap.TryGetValue(pkgId, out var pkgNfo)) return null;
		return pkgNfo.AssVer;
	}

	private class CacheJson
	{
		public Dictionary<string, PkgNfo> Map { get; set; }
	}

	private static CacheJson ToJson(this Cache cache) => new CacheJson
	{
		Map = cache.Map.ToDictionary(
			kv => $"{kv.Key}",
			kv => kv.Value
		)
	};
	private static Cache FromJson(this CacheJson cache) => new Cache
	{
		Map = cache.Map.ToDictionary(
			kv => PkgId.Parse(kv.Key),
			kv => kv.Value
		)
	};
	private static void Save(Cache cache) => File.WriteAllText(cacheFile, JsonSerializer.Serialize(cache.ToJson(), jsonOpt));
	private static Cache Load() => JsonSerializer.Deserialize<CacheJson>(File.ReadAllText(cacheFile), jsonOpt)!.FromJson();

	private static readonly JsonSerializerOptions jsonOpt = new()
	{
		WriteIndented = true
	};
	private static readonly string cacheFile = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "pkg-cache.json");

	private static readonly Lazy<Cache> cache = new(() =>
	{
		Cache cache;
		if (!File.Exists(cacheFile))
		{
			cache = BuildCache();
			Save(cache);
		}
		else
		{
			cache = Load();
		}
		return cache;
	});


	private static Cache Cache => cache.Value;



	private static Cache BuildCache()
	{
		var resolver = new NugetAssemblyResolver();

		static Version? Folder2Ver(string folder)
		{
			try
			{
				var verStr = Path.GetFileName(folder);
				return Version.Parse(verStr);
			}
			catch (Exception)
			{
				return null;
			}
		}

		"Building cache".Dump();
		var arr = (
			from pkgFolder in Directory.GetDirectories(Nuget.GlobalPackagesFolder)
			let pkgName = Path.GetFileName(pkgFolder)
			where !pkgName.StartsWith("runtime.")
			let ver = Directory.GetDirectories(pkgFolder).Select(Folder2Ver).Where(e => e != null).OrderByDescending(e => e).FirstOrDefault()
			where ver != null
			let nameRef = new AssemblyNameReference(pkgName, ver)
			let assDef = resolver.Resolve(nameRef)
			where assDef != null
			select new
			{
				Pkg = new PkgId(assDef.Name.Name, ver),
				PkgNfo = new PkgNfo(
					assDef.Name.Version,
					assDef.MainModule.AssemblyReferences.SelectToArray(e => new PkgId(e.Name, e.Version))
				)
			}
		)
			.ToArray();
		"Done".Dump();

		var grps = arr.GroupBy(e => e.Pkg).Select(e => new { Key = e.Key, Arr = e.ToArray() }).Where(e => e.Arr.Length > 1).ToArray();

		return new Cache
		{
			Map = arr.ToDictionary(
				e => e.Pkg,
				e => e.PkgNfo
			)
		};
	}
}





internal class NugetAssemblyResolver : IAssemblyResolver
{
	private readonly IAssemblyResolver defaultResolver = new DefaultAssemblyResolver();
	public void Dispose() => defaultResolver.Dispose();

	public AssemblyDefinition? Resolve(AssemblyNameReference name) => Resolve(name, new ReaderParameters());

	public AssemblyDefinition? Resolve(AssemblyNameReference name, ReaderParameters parameters)
	{
		try
		{
			return defaultResolver.Resolve(name);
		}
		catch (Exception ex)
		{
			var dllFile = Nuget.ResolveAssembly(name.Name, name.Version);
			if (dllFile == null) return null;
			var assDef = AssemblyDefinition.ReadAssembly(dllFile, parameters);
			return assDef;
		}
	}
}

static class Nuget
{
	private static readonly string[] preferredFws =
	{
		"net6.0-windows10.0.19041",
		"net6.0-windows7.0",
		"net6.0",
		"net5.0-windows10.0.19041",
		"net5.0-windows7.0",
		"net5.0",
		"netstandard2.1",
		"netstandard2.0",
		"netstandard1.6",
		"netstandard1.5",
		"netstandard1.4",
		"netstandard1.3",
		"netstandard1.2",
		"netstandard1.1",
		"netstandard1.0",
	};

	public static string? ResolveAssembly(string name, Version ver)
	{
		var libFolder = Path.Combine(GlobalPackagesFolder, name.ToLowerInvariant(), ver.Fmt(), "lib");
		if (!Directory.Exists(libFolder)) return null;
		foreach (var preferredFw in preferredFws)
		{
			var dllFolder = Path.Combine(libFolder, preferredFw);
			if (Directory.Exists(dllFolder))
			{
				var dllFile = Path.Combine(dllFolder, $"{name}.dll");
				if (File.Exists(dllFile)) return dllFile;
				var dllFiles = Directory.GetFiles(dllFolder, "*.dll");
				//if (dllFiles.Length > 1) throw new ArgumentException($"Wrong number of DLLs ({dllFiles.Length}) in '{dllFolder}'");
				if (dllFiles.Length > 1) return null;
				if (dllFiles.Length == 0) return null;
				return dllFiles[0];
			}
		}
		return null;
	}

	internal static string GlobalPackagesFolder => globalPackagesFolder.Value;



	private static string Fmt(this Version ver) => (ver.Revision == 0) switch
	{
		true => $"{ver.Major}.{ver.Minor}.{ver.Build}",
		false => $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}",
	};

	private static readonly Lazy<string> globalPackagesFolder = new(() =>
	{
		var res = Cli.Wrap("nuget")
			.WithArguments(new[]
			{
				"locals",
				"global-packages",
				"-list"
			})
			.ExecuteBufferedAsync()
			.GetAwaiter().GetResult();
		var folder = res.StandardOutput.Extract("(?<=global-packages: ).*").Trim().TrimEnd(Path.DirectorySeparatorChar);
		return folder;
	});

	private static string Extract(this string str, string regexStr)
	{
		var regex = new Regex(regexStr);
		if (!regex.IsMatch(str)) throw new ArgumentException();
		var match = regex.Match(str);
		return match.Captures[0].Value;
	}
}