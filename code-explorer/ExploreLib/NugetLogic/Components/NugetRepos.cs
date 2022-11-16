using ExploreLib.NugetLogic.Utils;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace ExploreLib.NugetLogic.Components;


public class NugetRepos : IDisposable
{
    public void Dispose() => SrcCacheCtx.Dispose();

    public SourceCacheContext SrcCacheCtx = new();

    public string SlnFolder { get; }
    public ISettings Settings { get; }
    public SourceRepositoryProvider Provider { get; }
    public SourceRepository[] Repos { get; }

    public NugetRepos(string slnFolder)
    {
        SlnFolder = slnFolder;
        Settings = NugetUtils.GetSettings(SlnFolder);
        var pkgSrcProvider = new PackageSourceProvider(Settings);
        Provider = new SourceRepositoryProvider(pkgSrcProvider, Repository.Provider.GetCoreV3());
        Repos = Provider.GetRepositories().ToArray();
    }
}