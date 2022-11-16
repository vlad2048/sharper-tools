using ExploreLib.NugetLogic;
using ExploreLib.NugetLogic.Components;
using ExploreLib.NugetLogic.Logging.Loggers;
using ExploreLib.NugetLogic.Structs;
using ExploreLib.NugetLogic.Structs.Refs;
using ExploreLib.NugetLogic.Utils;
using ExploreLib.Utils;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Resolver;

namespace ExploreLib;

static class MainPlay
{
	private static readonly Prj PrjColorPicker = PrjUtils.Load(@"C:\Dev_Nuget\Libs\WinFormsCtrlLibs\_Tools\ColorPicker\ColorPicker.csproj");
	private static readonly Prj PrjPowWeb = PrjUtils.Load(@"C:\Dev_Nuget\Libs\PowWeb\Libs\PowWeb\PowWeb.csproj");

	public static void Main()
	{
		var tree = DepTreeBuilder.Build(PrjPowWeb, opt =>
		{

		});

		/*var prjName = PrjPowWeb.Name;
		var nugetFolder = PrjPowWeb.Folder;
		var logger = new ConComLogger(LogLevel.Verbose);

		var prj = new Prj(prjName, NuGetFramework.AnyFramework, Array.Empty<PrjRef>(), Array.Empty<PkgRef>());
		var nugetRepos = new NugetRepos(nugetFolder);

		var resolveMap = NugetResolver.Resolve(
			prj,
			nugetRepos,
			DependencyBehavior.Lowest,
			false,
			false,
			VersionConstraints.None,
			logger
		);

		JsonUtils.SaveJson(@"C:\temp\code\abc.json", resolveMap);*/

		var abc = 123;
	}
}