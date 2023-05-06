<Query Kind="Program">
  <NuGetReference>LibGit2Sharp</NuGetReference>
  <NuGetReference>PowMaybe</NuGetReference>
  <Namespace>LibGit2Sharp</Namespace>
  <Namespace>LibGit2Sharp.Handlers</Namespace>
  <Namespace>PowMaybe</Namespace>
</Query>

#load "..\cfg"

void Main()
{
	//ApiGit.RetrieveGitState(@"C:\tmp\GitTest").Dump();
	
	//ApiGit.AddAndPush(@"C:\Dev_Nuget\Libs\LINQPadExtras", "0.0.6");
	
	
	//var folder = @"C:\Dev_Nuget\Libs\PowRxVar";
	//var repo = new Repository(folder);
	//repo.Push(repo.Head.CanonicalName);
}


public record GitState(
	string Url,
	GitFileState FileState,
	GitSyncState SyncState,
	bool IsHeadOnTag,
	string[] TaggedVersions
);


public static class ApiGit
{
	private static string ToCanonical(this string tagName) => $"refs/tags/{tagName}";
	private static readonly HashSet<string> tagsFetchedSet = new();
	

	public static string GetVersionTagName(string version) => $"v{version}";
	
	public static Maybe<GitState> RetrieveGitState(string folder) =>
		from url in ApiGit.GetRepoUrl(folder)
		select new GitState(
			url,
			GetFileState(folder),
			GetSyncState(folder),
			GetIsHeadOnTag(folder),
			GetTaggedVersions(folder)
		);
		

	public static void CreateVersionTag(string folder, string version)
	{
		var repo = new Repository(folder);
		var tagName = GetVersionTagName(version);
		var canTagName = tagName.ToCanonical();
		var sig = MakeSignature();
		// git tag -a v0.0.1 -m "v0.0.1"
		// git push origin refs/tags/v0.0.1
		// git tag -d v0.0.1
		var tag = repo.ApplyTag(tagName, sig, tagName);
		repo.Push(canTagName);
	}
	
	
	public static void AddAndPush(string folder, string commitMessage)
	{
		var repo = new Repository(folder);
		Commands.Stage(repo, "*");
		var sig = MakeSignature();
		repo.Commit(commitMessage, sig, sig);
		repo.Push(repo.Head.CanonicalName);
	}
	

	public static IgnoreSet GetIgnoreSetOpt(string folder) => IsRepo(folder) switch
	{
		true => GetIgnoreSet(folder),
		false => IgnoreSet.Empty,
	};
	
	
	
	private static Repository FetchTags(this string folder)
	{
		var repo = new Repository(folder);
		if (tagsFetchedSet.Contains(folder)) return repo;
		var remote = repo.Network.Remotes["origin"].Dump();
		Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(e => e.Specification), new FetchOptions { TagFetchMode = TagFetchMode.Auto, CredentialsProvider = Credz }, "");
		tagsFetchedSet.Add(folder);
		return repo;
	}
	
	private static Maybe<string> GetRepoUrl(string folder)
	{
		if (!IsRepo(folder)) return May.None<string>();
		var repo = new Repository(folder);
		if (repo.Network.Remotes.Count() == 0) return May.None<string>();
		return May.Some(repo.Network.Remotes.First().Url);
	}
	
	private static GitFileState GetFileState(string folder) => IsRepo(folder) switch
	{
		false => GitFileState.None,
		true => new Repository(folder).RetrieveStatus(new StatusOptions()).All(e => e.State == FileStatus.Ignored) switch
		{
			true => GitFileState.Clean,
			false => GitFileState.PendingChanges
		}
	};

	private static GitSyncState GetSyncState(string folder)
	{
		GitSyncState GetValidTrackingStatus()
		{
			var details = new Repository(folder).Head.TrackingDetails;
			return (details.AheadBy, details.BehindBy) switch
			{
				(not null and not 0, null or 0) => GitSyncState.Ahead,
				(null or 0, not null and not 0) => GitSyncState.Behind,
				(null or 0, null or 0) => GitSyncState.Clean,
				_ => throw new ArgumentException()
			};
		}
		
		return IsRepo(folder) switch
		{
			false => GitSyncState.None,
			true => GetValidTrackingStatus()
		};
	}
	
	private static bool GetIsHeadOnTag(string folder)
	{
		var repo = new Repository(folder);
		var headCommitId = repo.Head.Tip.Id;
		return repo.Tags.Any(e => e.Target.Id == headCommitId && TryGetVersionFromCanTagName(e.CanonicalName, out _));
	}
	
	private static string[] GetTaggedVersions(string folder) =>
		new Repository(folder).Tags
			.Where(e => TryGetVersionFromCanTagName(e.CanonicalName, out _))
			.Select(e => { TryGetVersionFromCanTagName(e.CanonicalName, out var version); return version!; })
			.ToArray();

	
	
	private static readonly CredentialsHandler Credz = (_url, _user, _cred) => new UsernamePasswordCredentials
    {
        Username = Cfg.GitHub.Username,
        Password = Cfg.GitHub.Token,
    };
	
	private static Signature MakeSignature() => new Signature(Cfg.Git.Name, Cfg.Git.Email, DateTime.Now);
	
	public static void Push(this Repository repo, string objectish)
	{
		var remote = repo.Network.Remotes["origin"];
		try
		{
			repo.Network.Push(remote, objectish, new PushOptions { CredentialsProvider = Credz });
		}
		catch (LibGit2SharpException ex) when (ex.Message.Contains("authentication", StringComparison.InvariantCultureIgnoreCase))
		{
			throw new ApplicationException("Your repo-man github 'Personal access token' seems to have expired (check in Developer settings). It needs the repo(all) and delete_repo perms", ex);
		}
	}

	
	private static bool IsRepo(string folder) => Repository.IsValid(folder);
	
	private static IgnoreSet GetIgnoreSet(string folder) =>
		new(
			new Repository(folder)
				.RetrieveStatus(new StatusOptions
				{
				})
				.Where(e => e.State == FileStatus.Ignored)
				.Select(e => Path.Combine(folder, e.FilePath).Replace("/", @"\"))
				.ToArray()
		);
	
	private static bool TryGetVersionFromCanTagName(string canTagName, out string? version)
	{
		version = null;
		var parts = canTagName.Split('/');
		if (parts.Length != 3) return false;
		if (parts[0] != "refs") return false;
		if (parts[1] != "tags") return false;
		var t = parts[2];
		if (!t.StartsWith("v")) return false;
		if (t.Contains(' ')) return false;
		version = t[1..];
		return true;
	}
}






public class IgnoreSet
{
	public HashSet<string> Files { get; }
	public string[] Folders { get; }
	public static readonly IgnoreSet Empty = new(Array.Empty<string>());
	public IgnoreSet(string[] allIgnored)
	{
		Files = allIgnored.Where(e => !e.EndsWith(@"\")).ToHashSet();
		Folders = allIgnored.Where(e => e.EndsWith(@"\")).ToArray();
	}
	public bool IsNotIgnored(string path) =>
		!Files.Contains(path) &&
		Folders.All(folder => !path.StartsWith(folder));
}

public enum GitFileState
{
	None,
	PendingChanges,
	Clean,
}

public enum GitSyncState
{
	None,
	Ahead,
	Behind,
	Clean
}


public static class GitEnumUtils
{
	public static string Fmt(this GitFileState e) => e switch
	{
		GitFileState.None => "_",
		GitFileState.PendingChanges => "pending changes",
		GitFileState.Clean => "clean",
		_ => throw new ArgumentException()
	};

	public static string Fmt(this GitSyncState e) => e switch
	{
		GitSyncState.None => "_",
		GitSyncState.Ahead => "ahead",
		GitSyncState.Behind => "behind",
		GitSyncState.Clean => "in sync",
		_ => throw new ArgumentException()
	};
	
	public static bool IsNormEnabled(bool isNormEmpty, GitFileState gitStatus, GitSyncState gitTrackingStatus) =>
		!isNormEmpty &&
		gitStatus == GitFileState.Clean &&
		gitTrackingStatus == GitSyncState.Clean;

	public static string FmtNorm(bool isNormEmpty, GitFileState gitStatus, GitSyncState gitTrackingStatus) =>
		(isNormEmpty, gitStatus == GitFileState.Clean, gitTrackingStatus == GitSyncState.Clean) switch
		{
			(true, _, _) => "already normalized",
			(false, false, _) => "git repo not clean",
			(false, true, false) => "git repo not in sync",
			_ => string.Empty
		};
}

