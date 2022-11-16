using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;

namespace ExploreLib.NugetLogic.Utils;

static class NugetUtils
{
	public static ISettings GetSettings(string slnFolder) => Settings.LoadDefaultSettings(slnFolder, null, new MachineWideSettings());
	public static bool AreFrameoworksCompatible(NuGetFramework mainFramework, NuGetFramework depFramework) => DefaultCompatibilityProvider.Instance.IsCompatible(mainFramework, depFramework);


	private class MachineWideSettings : IMachineWideSettings
	{
		private readonly Lazy<ISettings> _settings;
		ISettings IMachineWideSettings.Settings => _settings.Value;
		public MachineWideSettings()
		{
			var baseDirectory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.MachineWideConfigDirectory);
			_settings = new Lazy<ISettings>(() => Settings.LoadMachineWideSettings(baseDirectory));
		}
	}
}