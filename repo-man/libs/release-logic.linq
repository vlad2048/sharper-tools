<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras.Scripting_Batcher</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>PowMaybe</Namespace>
  <Namespace>PowMaybeErr</Namespace>
</Query>

#load "..\cfg"
#load "..\libs-lowlevel\xml"
#load "..\libs-lowlevel\watcher"
#load ".\api-common"
#load ".\api-nuget"
#load ".\api-github"
#load ".\api-git"
#load ".\api-solution"
#load ".\structs"


void Main()
{
	var folder = @"C:\Dev_Nuget\Libs\LINQPadExtras";
	var dryRun = false;
	
	var slnNfo = new SlnNfo(folder);
	var maySln = SlnDetails.Retrieve(slnNfo);
	
	if (maySln.IsNone(out var sln, out var err))
	{
		$"Error: {err}".Dump();
		return;
	}
	
	//ReleaseLogic.ReleaseSln(sln, "0.0.6", dryRun); return;
	
	var mayGitState = ApiGit.RetrieveGitState(folder);
	var releaseNfo = ReleaseLogic.CanRelease(mayGitState, sln);
	if (!releaseNfo.CanRelease)
	{
		$"Cannot release: {releaseNfo.ErrorMessage}".Dump();
		return;
	}
	//ReleaseLogic.ReleaseSln(sln, releaseNfo.ReleaseVersion!, dryRun);
}



public record ReleaseNfo(
	bool CanRelease,
	string? ReleaseVersion,
	string? ErrorMessage
)
{
	public static ReleaseNfo MakeYes(string releaseVersion) => new(true, releaseVersion, null);
	public static ReleaseNfo MakeNo(string errorMessage) => new(false, null, errorMessage);
}


public static class ReleaseLogic
{
	public static void ReleaseSln(SlnDetails sln, string releaseVersion, bool dryRun)
	{
		Batcher.Run(
			$"Releasing {releaseVersion}",
			cmd =>
			{
				var abc = 123;
				
				cmd.Cd(sln.Folder);
				cmd.Run("dotnet", "build");
			
				var version = Xml.Get(sln.Nfo.DirectoryBuildPropsFile, XmlPaths.Version);
				if (version != releaseVersion && !dryRun)
				{
					Xml.Set(sln.Nfo.DirectoryBuildPropsFile, XmlPaths.Version, releaseVersion);
					ApiGit.AddAndPush(sln.Folder, ApiGit.GetVersionTagName(releaseVersion));
				}
				if (!dryRun)
					ApiGit.CreateVersionTag(sln.Folder, releaseVersion);
				foreach (var prj in sln.Prjs)
					ApiNuget.Release(cmd, NugetSource.Remote, prj.Nfo, releaseVersion, true, dryRun);
			},
			opt =>
			{
				opt.DryRun = dryRun;
			}
		);
	}
	
	
	public static ReleaseNfo CanRelease(Maybe<GitState> mayGitState, SlnDetails slnDetails)
	{
		if (mayGitState.IsNone(out var gitState)) return ReleaseNfo.MakeNo("no git repo");
		if (gitState.FileState != GitFileState.Clean) return ReleaseNfo.MakeNo("pending changes");
		if (gitState.SyncState != GitSyncState.Clean) return ReleaseNfo.MakeNo("unsynced changes");
		if (gitState.IsHeadOnTag) return ReleaseNfo.MakeNo("no changes");
		
		var releaseVersion = slnDetails.Version;
		var reason = IsVersionTaggedOrReleasedToNuget(gitState, slnDetails, releaseVersion, out _);
		if (reason != VersionFailReason.None)
		{
			releaseVersion = IncVersion(releaseVersion);
			reason = IsVersionTaggedOrReleasedToNuget(gitState, slnDetails, releaseVersion, out var prjNameIssue);
			if (reason != VersionFailReason.None)
				return reason switch
				{
					VersionFailReason.AlreadyTagged => ReleaseNfo.MakeNo($"{releaseVersion} already tagged in repo"),
					VersionFailReason.AlreadyReleasedToNugetRemotely => ReleaseNfo.MakeNo($"{releaseVersion} already released to Nuget for {prjNameIssue}"),
					VersionFailReason.AlreadyReleasedToNugetLocally => ReleaseNfo.MakeNo($"{releaseVersion} already released locally for {prjNameIssue}"),
					_ => throw new ArgumentException()
				};
		}
		
		return ReleaseNfo.MakeYes(releaseVersion);
	}
	
	private enum VersionFailReason
	{
		None,
		AlreadyTagged,
		AlreadyReleasedToNugetRemotely,
		AlreadyReleasedToNugetLocally,
	}
	
	private static VersionFailReason IsVersionTaggedOrReleasedToNuget(GitState gitState, SlnDetails slnDetails, string version, out string? prjNameIssue)
	{
		prjNameIssue = null;
		if (gitState.TaggedVersions.Contains(version)) return VersionFailReason.AlreadyTagged;
		foreach (var prj in slnDetails.Prjs)
		{
			if (ApiNuget.GetVers(NugetSource.Local, prj.Nfo.Name).Contains(version))
			{
				prjNameIssue = prj.Nfo.Name;
				return VersionFailReason.AlreadyReleasedToNugetLocally;
			}
			if (ApiNuget.GetVers(NugetSource.Remote, prj.Nfo.Name).Contains(version))
			{
				prjNameIssue = prj.Nfo.Name;
				return VersionFailReason.AlreadyReleasedToNugetRemotely;
			}
		}
		return VersionFailReason.None;
	}
	
	private static string IncVersion(string version)
	{
		var ver = Version.Parse(version);
		return new Version(ver.Major, ver.Minor, ver.Build + 1).ToString();
	}
}