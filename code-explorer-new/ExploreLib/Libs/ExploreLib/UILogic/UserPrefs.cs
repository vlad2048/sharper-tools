using System.Reactive.Linq;
using System.Text.Json.Serialization;
using ExploreLib._1_DllFinding;
using ExploreLib._1_DllFinding.Structs;
using ExploreLib.Utils;
using PowBasics.CollectionsExt;
using PowRxVar;

namespace ExploreLib.UILogic;

public record UserPrefs(
	string[] FavsDlls,
	[property: JsonRequired]
	string DllSearchText
)
{
	public static readonly UserPrefs Empty = new(
		Array.Empty<string>(),
		string.Empty
	);
}



public static class UserPrefsLogic
{
	public static bool DisableSave { get; set; }

	public static (IRwVar<DllNfo[]>, IDisposable) GetFavsDlls()
	{
		var d = new Disp();
		var rxV = Var.Make(
			DllFinder.Dlls.WhereToArray(e => inst.FavsDlls.Any(f => e.Name == f))
		).D(d);

		rxV
			.Skip(1)
			.Subscribe(favs => Update(prefs => prefs with { FavsDlls = favs.SelectToArray(e => e.Name) })).D(d);

		return (rxV, d);
	}

	public static (IRwVar<string>, IDisposable) GetDllSearchText()
	{
		var d = new Disp();
		var rxV = Var.Make(inst.DllSearchText).D(d);
		rxV
			.Skip(1)
			.Subscribe(e => Update(prefs => prefs with { DllSearchText = e })).D(d);
		return (rxV, d);
	}

	private static UserPrefs inst = PersistUtils.LoadUserPrefs();

	private static void Update(Func<UserPrefs, UserPrefs> updateFun)
	{
		if (DisableSave) return;
		inst = updateFun(inst);
		PersistUtils.SaveUserPrefs(inst);
	}
}