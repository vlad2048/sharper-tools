<Query Kind="Program">
  <Namespace>CliWrap</Namespace>
  <Namespace>CliWrap.Buffered</Namespace>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>System.Text.Json</Namespace>
</Query>

#load ".\01_structs"
#load ".\02_lowlevel"
#load ".\03_resolver"
#load ".\04_structs-proj"

void Main()
{
	//CheckCache();
	//CheckGetPkgDeps();
	//CheckResolver();
	//CheckAssVers();
}

static void CheckCache()
{
	var map = CacheUtils.CacheMap;
	var abc = 123;
}

static void CheckAssVers()
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

	var arr = (
		from pkgFolder in Directory.GetDirectories(Nuget.GlobalPackagesFolder)
		let pkgName = Path.GetFileName(pkgFolder)
		let ver = Directory.GetDirectories(pkgFolder).Select(Folder2Ver).Where(e => e != null).OrderByDescending(e => e).FirstOrDefault()
		where ver != null
		let nameRef = new AssemblyNameReference(pkgName, ver)
		let assDef = resolver.Resolve(nameRef)
		where assDef != null
		select new
		{
			PkgVer = ver,
			AssDef = assDef
		}
	);
	
	var sb = new StringBuilder();
	using var sw = new StreamWriter(@"C:\temp\nfo.txt") { AutoFlush = true };
	foreach (var elt in arr)
	{
		var name = elt.AssDef.Name.Name.PadRight(60);
		var pkgVer = elt.PkgVer.ToString().PadRight(16);
		var assVer = elt.AssDef.Name.Version.ToString().PadRight(16);
		var str = $"{name}  pkg: {pkgVer}  ass: {assVer}";
		sw.WriteLine(str);
		sb.AppendLine(str);
	}
	ConUtils.Print(sb.ToString());
}

static void CheckGetPkgDeps()
{
	var pkgRef = new PkgRef("PowWeb", Version.Parse("0.0.1"));	
	var pkgDeps = pkgRef.GetPkgDeps();	
	pkgDeps.Dump();
}

static void CheckResolver()
{
	var assName = new AssemblyNameReference("System.Collections", Version.Parse("4.3.0"));
	var resolver = new NugetAssemblyResolver();
	var assDef = resolver.Resolve(assName);
	assDef.Dump();
}



public static class SlnUtils
{
	public static Proj[] GetProjs(this Sln sln) => Files.FindRecursively(sln.Folder, "*.csproj").SelectToArray(file => new Proj(sln, file));
}


public static class ProjUtils
{
	private static readonly IAssemblyResolver nugetResolver = new NugetAssemblyResolver();
	
	public static IRef[] GetRefs(this Proj proj)
	{
		var xml = new Xml(proj);
		
		var pkgRefs = (
			from node in xml.SelNodes("//PackageReference")
			where !node.GetAttrs().Any(e => e.Name == "Update") && node.GetAttrs().Any(e => e.Name == "Version")
			select new PkgRefInProj(xml, proj, node)
		).OfType<IRef>();
		
		var projRefs = (
			from node in xml.SelNodes("//ProjectReference")
			select new ProjRef(proj, node)
		).OfType<IRef>();

		var refs = projRefs.Concat(pkgRefs).ToArray();
		return refs;
	}

	public static PkgRef[] GetPkgDeps(this IPkgRef pkgRef)
	{
		var map = CacheUtils.CacheMap;
		var pkgId = new PkgId(pkgRef.Name, pkgRef.Ver);
		if (map.TryGetValue(pkgId, out var pkgNfo))
		{
			return pkgNfo.Deps.SelectToArray(e => new PkgRef(e.Name, e.PkgVer));
		}
		
		var pkgRefName = new AssemblyNameReference(pkgRef.Name, pkgRef.Ver);
		var assDef = nugetResolver.Resolve(pkgRefName);
		if (assDef == null) return Array.Empty<PkgRef>();
		var deps = assDef.MainModule.AssemblyReferences;
		return deps.SelectToArray(e => new PkgRef(e.Name, e.Version));
	}
}



