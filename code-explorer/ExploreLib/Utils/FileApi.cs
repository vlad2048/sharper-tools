namespace ExploreLib.Utils;

class FileApi
{
	private readonly string rootFolder;

	public string NugetDepTreeCacheFile => Path.Combine(rootFolder, "nuget-deptree-cache.json");

	public FileApi(string rootFolder)
	{
		this.rootFolder = rootFolder;
	}
}