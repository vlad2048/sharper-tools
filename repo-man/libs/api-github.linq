<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>Octokit</NuGetReference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>Octokit</Namespace>
  <Namespace>LINQPadExtras.Scripting_Batcher</Namespace>
</Query>

#load "..\cfg"
#load ".\api-common"

void Main()
{
	var sln = new SlnNfo(@"C:\tmp\GitTest");
	void Check() => $"Repo exists: {ApiGithub.DoesRepoExist(sln.Name)}".Dump();
	Check();
	return;
	
	var gitFolder = Path.Combine(sln.Folder, ".git");
	if (ApiGithub.DoesRepoExist(sln.Name)) ApiGithub.DeleteRepoAndGitFolder(sln.Name, sln.Folder);
	//if (Directory.Exists(gitFolder)) Directory.Delete(gitFolder, true);
	return;
		
	Check();
	ApiGithub.CreateRepo(sln);
	Check();
}


public static class ApiGithub
{
	public static bool DoesRepoExist(string name) => Client.Repository.GetAllForCurrent().Result.Any(e => e.Name == name);
	
	public static void CreateRepo(SlnNfo sln)
	{
		if (DoesRepoExist(sln.Name)) throw new ArgumentException("GitHub repo already exists");
		
		var gitUrl = $"https://github.com/vlad2048/{sln.Name}.git";

		Batcher.Run(
			$"Creating {sln.Name} GitHub repo",
			cmd =>
			{
				var gitFolder = Path.Combine(sln.Folder, ".git");
				var isRepoInited = Directory.Exists(gitFolder);
				if (isRepoInited)
					cmd.Cancel("A git repo already exists in this folder");
				if (!File.Exists(sln.ReadmeFile)) File.WriteAllText(sln.ReadmeFile, $"# {sln.Name}");

				cmd.Cd(sln.Folder);
				void RunGitCmd(params string[] args) => cmd.Run("git", args);
				void RunGitCmdLeaveOpen(params string[] args) => cmd.RunLeaveOpen("git", args);

				if (!isRepoInited)
					RunGitCmd("init");
				RunGitCmd("add", "*");
				RunGitCmdLeaveOpen("status");
				
				cmd.AskConfirmation(
					"Are you happy with the initial list of files ?",
					onCancel: () => DeleteFolderTweakPermsIfNeeded(gitFolder)
				);
				
				RunGitCmd("commit", "-m", "Initial commit");

				Client.Repository.Create(new NewRepository(sln.Name)).Wait();
				RunGitCmd("remote", "add", "origin", gitUrl);
				RunGitCmd("push", "-u", "origin", "master");
				cmd.AddArtifact(gitUrl);
			}
		);
	}
	
	public static void DeleteRepoAndGitFolder(string name, string folder)
	{
		DeleteRepo(name);
		var gitFolder = Path.Combine(folder, ".git");
		DeleteFolderTweakPermsIfNeeded(gitFolder);
	}
	
	private static void DeleteRepo(string name)
	{
		//var str = Util.ReadLine($"Are you sure you want to delete the github repo '{name}' ? (y/n)").ToLowerInvariant().Trim(); if (str != "y") return;
		if (name != "GitTest") throw new ArgumentException("As a safety measure, you can only delete a repo named GitTest");
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
	
	private static void DeleteFolderTweakPermsIfNeeded(string folder)
	{
		void Recurse(string f)
		{
			var files = Directory.GetFiles(f);
			var dirs = Directory.GetDirectories(f);
			foreach (var file in files) File.SetAttributes(file, FileAttributes.Normal);
			foreach (var dir in dirs) Recurse(dir);
		}
		Recurse(folder);
		Directory.Delete(folder, true);
	}
}