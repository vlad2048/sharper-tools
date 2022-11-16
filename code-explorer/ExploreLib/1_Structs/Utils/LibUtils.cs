using NuGet.Configuration;
using PowMaybe;

namespace ExploreLib._1_Structs.Utils;

public static class LibUtils
{
	public static Lib[] FindLibs() => (
			from depFolder in Directory.GetDirectories(GlobalPackagesFolder)
			select GetLibInFolder(depFolder)
		)
		.WhereSome()
		.ToArray();



	private static readonly Lazy<string> globalPackagesFolder = new(() => SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(null)));

	private static string GlobalPackagesFolder => globalPackagesFolder.Value;

	private static readonly string[] targets = {
		"net7.0",
		"net6.0",
		"net5.0",
		"netstandard2.0",
		"net45",
	};


	private static Maybe<Lib> GetLibInFolder(string depFolder) =>
		from verFolder in GetVerFolder(depFolder)
		from dll in GetDllInVerFolder(verFolder)
		select new Lib(dll);

	private static Maybe<string> GetVerFolder(string depFolder) =>
		Directory.GetDirectories(depFolder)
			.Select(Path.GetFileName)
			.Select(e => e!)
			.Where(e => Version.TryParse(e, out _))
			.Select(Version.Parse)
			.OrderByDescending(e => e)
			.Select(e => Path.Combine(depFolder, $"{e}"))
			.FirstOrMaybe();

	private static Maybe<string> GetDllInVerFolder(string verFolder)
	{
		var libFolder = Path.Combine(verFolder, "lib");
		if (!Directory.Exists(libFolder)) return May.None<string>();

		return (
				from target in targets
				let targetFolder = Path.Combine(libFolder, target)
				let mayDll = GetDllInTargetFolder(targetFolder)
				where mayDll.IsSome()
				select mayDll.Ensure()
			)
			.FirstOrMaybe();
	}

	private static Maybe<string> GetDllInTargetFolder(string targetFolder)
	{
		if (!Directory.Exists(targetFolder)) return May.None<string>();
		var dlls = Directory.GetFiles(targetFolder, "*.dll");
		return dlls.Length switch
		{
			1 => May.Some(dlls[0]),
			_ => May.None<string>()
		};
	}
}