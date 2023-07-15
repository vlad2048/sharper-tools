using ExploreLib._1_DllFinding.Structs;
using ExploreLib.Utils;
using PowMaybe;

namespace ExploreLib._1_DllFinding;

record DllCache(
	DllNfo[] Dlls
)
{
	public static readonly DllCache Empty = new(
		Array.Empty<DllNfo>()
	);
}

public static class DllFinder
{
	public static DllNfo[] Dlls => dllsLazy.Value;


	private static readonly Lazy<DllNfo[]> dllsLazy = new(() => Find());

	private static DllNfo[] Find(bool refreshCache = false)
	{
		var cache = PersistUtils.LoadDllCache();
		if (cache.Dlls.Length > 0 && !refreshCache)
			return cache.Dlls;

		var dlls = Directory.GetDirectories(FileUtils.DllRootFolder)
			.Select(pkgFolder =>
				from verLibFolder in GetLastVerLibFolder(pkgFolder)
				from fwFolder in GetFwFolder(verLibFolder)
				let nameLower = Path.GetFileName(pkgFolder)
				from dll in GetDll(fwFolder, nameLower)
				select new DllNfo(
					ChooseName(nameLower, dll),
					Version.Parse(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(fwFolder)))!),
					dll,
					new FileInfo(dll).Length
				)
			)
			.WhereSome()
			.ToArray();

		cache = new DllCache(dlls);
		PersistUtils.SaveDllCache(cache);
		return dlls;
	}

	private static string ChooseName(string nameLower, string dll)
	{
		var dllName = Path.GetFileNameWithoutExtension(dll);
		return (string.Compare(nameLower, dllName, StringComparison.InvariantCultureIgnoreCase) == 0) switch
		{
			true => dllName,
			false => nameLower
		};
	}

	private static Maybe<string> GetLastVerLibFolder(string f) =>
		(
			from dir in Directory.GetDirectories(f)
			let verStr = Path.GetFileName(dir)
			where Version.TryParse(verStr, out _)
			select verStr
		)
		.OrderByDescending(Version.Parse)
		.Select(e => Path.Combine(f, e))
		.Select(e => Path.Combine(e, "lib"))
		.Where(Directory.Exists)
		.FirstOrMaybe();


	private static readonly string[] preferredFws =
	{
		"net20.0",
		"net19.0",
		"net18.0",
		"net17.0",
		"net16.0",
		"net15.0",
		"net14.0",
		"net13.0",
		"net12.0",
		"net11.0",
		"net10.0",
		"net9.0",
		"net8.0",
		"net7.0",
		"net6.0",
		"net5.0",

		"net4",
		"net3",
		"net2",

		"netcore",

		"netstandard"
	};

	private static Maybe<string> GetFwFolder(string verLibFolder)
	{
		var candidates = Directory.GetDirectories(verLibFolder).OrderByDescending(e => e).ToArray();
		foreach (var fw in preferredFws)
		{
			foreach (var candidate in candidates)
			{
				var candidateName = Path.GetFileName(candidate);
				if (candidateName.StartsWith(fw, StringComparison.InvariantCultureIgnoreCase))
					return May.Some(candidate);
			}
		}
		return May.None<string>();
	}

	private static Maybe<string> GetDll(string fwFolder, string name)
	{
		var dlls = Directory.GetFiles(fwFolder, "*.dll");
		var dll = dlls.FirstOrDefault(e => string.Compare(Path.GetFileNameWithoutExtension(e), name, StringComparison.InvariantCultureIgnoreCase) == 0);
		if (dll != null)
			return May.Some(dll);
		return dlls
			.OrderByDescending(e => new FileInfo(e).Length)
			.FirstOrMaybe();
	}
}