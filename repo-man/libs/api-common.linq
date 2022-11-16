<Query Kind="Program" />

void Main()
{
	
}

public record PrjNfo(string File)
{
	public string Folder => Path.GetDirectoryName(File)!;
	public string Name => Path.GetFileNameWithoutExtension(File);
	public string NameLower => Name.ToLowerInvariant();
	public string NugetUrl => $"https://www.nuget.org/packages/{Name}/";
}

public record SlnNfo(string Folder)
{
	public string Name => Path.GetFileNameWithoutExtension(Folder);
	public string ReadmeFile => Path.Combine(Folder, "README.md");
	public string DirectoryBuildPropsFile => Path.Combine(Folder, "Directory.Build.props");
	public string GitHubUrl => $"https://github.com/vlad2048/{Name}.git";
}



public static class ApiCommon
{
	public static DateTime GetFolderLastTimestamp(string folder, params string[] excludeDirs)
	{
		static DateTime MaxTime(params DateTime[] times) => times.Max();

		var (dirNfos, fileNfos) = GetAllFoldersAndFilesRecursively(folder, excludeDirs);
		var timestamp = MaxTime(
			dirNfos.Select(e => e.LastWriteTime).MaxTime(),
			fileNfos.Select(e => e.LastWriteTime).MaxTime()
		);
		return timestamp;
	}


	private static (DirectoryInfo[], FileInfo[]) GetAllFoldersAndFilesRecursively(string rootFolder, string[] excludeDirs)
	{
		var dirList = new List<DirectoryInfo>
		{
			new DirectoryInfo(rootFolder)
		};
		var fileList = new List<FileInfo>();

		void Recurse(string folder)
		{
			fileList.AddRange(Directory.GetFiles(folder).Select(e => new FileInfo(e)));
			var subFolders = Directory.GetDirectories(folder).Where(e => !excludeDirs.Contains(Path.GetFileName(e)));
			dirList.AddRange(subFolders.Select(e => new DirectoryInfo(e)));
			foreach (var subFolder in subFolders)
				Recurse(subFolder);
		}
		Recurse(rootFolder);
		return (dirList.ToArray(), fileList.ToArray());
	}
	private static DateTime MaxTime(this IEnumerable<DateTime> times)
	{
		var list = times.ToList();
		if (list.Count == 0) return DateTime.MinValue;
		var t = list[0];
		foreach (var time in times.Skip(1))
			if (time > t)
				t = time;
		return t;
	}
}