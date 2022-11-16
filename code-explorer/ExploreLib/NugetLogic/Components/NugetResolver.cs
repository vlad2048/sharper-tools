using System.Xml.Linq;
using ExploreLib.NugetLogic.Logging.Loggers.Base;
using ExploreLib.NugetLogic.Structs;
using NuGet.Common;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace ExploreLib.NugetLogic.Components;

static class NugetResolver
{
	private const string pkgFolder = @"C:\non-existing-absolute-path";

	public static Dictionary<string, Pkg> Resolve(
		Prj prj,
		NugetRepos nugetRepos,
		DependencyBehavior behavior,
		bool includePrerelease,
		bool includeUnlisted,
		VersionConstraints constraints,
		IComLogger logger
	)
	{
		var resolutionCtx = new ResolutionContext(behavior, includePrerelease, includeUnlisted, constraints);
		var prjCtx = new ProjectContext(logger);
		var folderPrj = new FolderNuGetProject(nugetRepos.SlnFolder);
		var pkgMan = new NuGetPackageManager(nugetRepos.Provider, nugetRepos.Settings, pkgFolder)
		{
			PackagesFolderNuGetProject = folderPrj
		};

		var pkg = new PackageIdentity(prj.Name, NuGetVersion.Parse("1.0.0"));

		return pkgMan.PreviewInstallPackageAsync(
			pkgMan.PackagesFolderNuGetProject,
			pkg.Id,
			resolutionCtx,
			prjCtx,
			nugetRepos.Repos,
			Array.Empty<SourceRepository>(),
			CancellationToken.None
		).Result
			.Where(e => e.NuGetProjectActionType == NuGetProjectActionType.Install)
			.ToDictionary(
				e => e.PackageIdentity.Id,
				e => new Pkg(e.PackageIdentity.Id, e.PackageIdentity.Version)
			);
	}


	public class ProjectContext : INuGetProjectContext
	{
		private readonly IComLogger comLogger;

		public ProjectContext(IComLogger comLogger)
		{
			this.comLogger = comLogger;
		}

		public NuGetActionType ActionType { get; set; }
		public Guid OperationId { get; set; }

		public FileConflictAction ResolveFileConflict(string message) => FileConflictAction.Ignore;
		public PackageExtractionContext PackageExtractionContext { get; set; } = null!;
		public XDocument OriginalPackagesConfig { get; set; } = null!;
		public ISourceControlManagerProvider SourceControlManagerProvider => null!;
		public NuGet.ProjectManagement.ExecutionContext ExecutionContext => null!;

		public void Log(MessageLevel level, string msg, params object[] args) => L(MapMessageLevel(level), string.Format(msg, args));
		public void Log(ILogMessage msg) => L(msg.Level, msg.Message);
		public void ReportError(string msg) => L(LogLevel.Error, msg);
		public void ReportError(ILogMessage msg) => L(msg.Level, msg.Message);

		private void L(LogLevel logLevel, string s) => comLogger.Log(logLevel, $"[RESOLVER] - {s}");

		private static LogLevel MapMessageLevel(MessageLevel e) => e switch
		{
			MessageLevel.Info => LogLevel.Information,
			MessageLevel.Warning => LogLevel.Warning,
			MessageLevel.Debug => LogLevel.Debug,
			MessageLevel.Error => LogLevel.Error,
			_ => throw new ArgumentException()
		};
	}
}