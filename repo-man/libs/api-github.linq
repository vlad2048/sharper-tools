<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0\LINQPadExtras.dll</Reference>
  <NuGetReference>Octokit</NuGetReference>
  <Namespace>Octokit</Namespace>
  <Namespace>LINQPadExtras</Namespace>
</Query>

#load "..\cfg"
#load ".\api-common"

void Main()
{
	
}


public static class ApiGithub
{
	public static bool DoesRepoExist(string name) => Client.Repository.GetAllForCurrent().Result.Any(e => e.Name == name);
	
	public static void CreateRepo(SlnNfo sln)
	{
		Con.Start("Creating ", sln.Name, " GitHub repo");
		if (DoesRepoExist(sln.Name)) throw new ArgumentException("GitHub repo already exists");
		var isRepoInited = Directory.GetDirectories(sln.Folder, ".git").Any();

		if (!File.Exists(sln.ReadmeFile)) File.WriteAllText(sln.ReadmeFile, $"# {sln.Name}");

		void RunGitCmd(params string[] args) => Con.RunIn("git", sln.Folder, args);
		void RunGitCmdLeaveOpen(params string[] args) => Con.RunInLeaveOpen("git", sln.Folder, args);

		if (!isRepoInited)
			RunGitCmd("init");
		RunGitCmd("add", "*");
		RunGitCmdLeaveOpen("status");
		var res = Util.ReadLine("Are you happy with the file list ? ('y' to continue otherwise the script will exit)").ToLowerInvariant().Trim();
		if (res != "y") Environment.Exit(1);

		RunGitCmd("commit", "-m", "Initial commit");

		Client.Repository.Create(new NewRepository(sln.Name)).Wait();
		RunGitCmd("remote", "add", "origin", sln.GitHubUrl);
		RunGitCmd("push", "-u", "origin", "master");
		Con.AddArtifact(sln.GitHubUrl);
		Con.EndSuccess();
	}
	
	public static void DeleteRepo(string name)
	{
		var str = Util.ReadLine($"Are you sure you want to delete the github repo '{name}' ? (y/n)").ToLowerInvariant().Trim();
		if (str != "y") return;
		Client.Repository.Delete(Cfg.GitHub.Owner, name).Wait();
	}


	// ***********
	// * Private *
	// ***********
	private static readonly Lazy<GitHubClient> client = new(() => new GitHubClient(new ProductHeaderValue(Cfg.GitHub.ProductHeaderValue))
	{
		Credentials = new Octokit.Credentials(Cfg.GitHub.Token)
	});
	private static GitHubClient Client => client.Value;
}