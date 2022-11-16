<Query Kind="Program">
  <NuGetReference>LibGit2Sharp</NuGetReference>
  <Namespace>LibGit2Sharp</Namespace>
</Query>

void Main()
{
	var folder = @"C:\Dev_Nuget\Libs\PowBasics";
	ApiGit.GetStatus(folder).Dump();
	ApiGit.GetTrackingStatus(folder).Dump();
	
	/*var repo = new Repository(folder);
	var aheadBy = repo.Head.TrackingDetails.AheadBy;
	var behindBy = repo.Head.TrackingDetails.BehindBy;
	$"aheadBy :{aheadBy}".Dump();
	$"behindBy:{behindBy}".Dump();*/
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

public enum GitStatus
{
	None,
	PendingChanges,
	Clean,
}

public enum GitTrackingStatus
{
	None,
	Ahead,
	Behind,
	Clean
}


public static class GitEnumUtils
{
	public static string Fmt(this GitStatus e) => e switch
	{
		GitStatus.None => "_",
		GitStatus.PendingChanges => "pending changes",
		GitStatus.Clean => "clean",
		_ => throw new ArgumentException()
	};

	public static string Fmt(this GitTrackingStatus e) => e switch
	{
		GitTrackingStatus.None => "_",
		GitTrackingStatus.Ahead => "ahead",
		GitTrackingStatus.Behind => "behind",
		GitTrackingStatus.Clean => "in sync",
		_ => throw new ArgumentException()
	};
	
	public static bool IsNormEnabled(bool isNormEmpty, GitStatus gitStatus, GitTrackingStatus gitTrackingStatus) =>
		!isNormEmpty &&
		gitStatus == GitStatus.Clean &&
		gitTrackingStatus == GitTrackingStatus.Clean;

	public static string FmtNorm(bool isNormEmpty, GitStatus gitStatus, GitTrackingStatus gitTrackingStatus) =>
		(isNormEmpty, gitStatus == GitStatus.Clean, gitTrackingStatus == GitTrackingStatus.Clean) switch
		{
			(true, _, _) => "already normalized",
			(false, false, _) => "git repo not clean",
			(false, true, false) => "git repo not in sync",
			_ => string.Empty
		};
}



public static class ApiGit
{
	public static GitStatus GetStatus(string folder) => IsRepo(folder) switch
	{
		false => GitStatus.None,
		true => new Repository(folder).RetrieveStatus(new StatusOptions()).All(e => e.State == FileStatus.Ignored) switch
		{
			true => GitStatus.Clean,
			false => GitStatus.PendingChanges
		}
	};

	public static GitTrackingStatus GetTrackingStatus(string folder)
	{
		GitTrackingStatus GetValidTrackingStatus()
		{
			var details = new Repository(folder).Head.TrackingDetails;
			return (details.AheadBy, details.BehindBy) switch
			{
				(not null and not 0, null or 0) => GitTrackingStatus.Ahead,
				(null or 0, not null and not 0) => GitTrackingStatus.Behind,
				(null or 0, null or 0) => GitTrackingStatus.Clean,
				_ => throw new ArgumentException()
			};
		}
		
		return IsRepo(folder) switch
		{
			false => GitTrackingStatus.None,
			true => GetValidTrackingStatus()
		};
	}
	
	public static IgnoreSet GetIgnoreSetOpt(string folder) => IsRepo(folder) switch
	{
		true => GetIgnoreSet(folder),
		false => IgnoreSet.Empty,
	};
	
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
}
