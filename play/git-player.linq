<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>LibGit2Sharp</NuGetReference>
  <NuGetReference>PowBasics</NuGetReference>
  <NuGetReference>PowTrees</NuGetReference>
  <NuGetReference>PowTrees.LINQPad</NuGetReference>
  <Namespace>LibGit2Sharp</Namespace>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>PowBasics.StringsExt</Namespace>
  <Namespace>PowTrees.Algorithms</Namespace>
  <Namespace>PowTrees.LINQPad</Namespace>
</Query>

internal const string RepoFolder = @"C:\tmp\mini-repo";
internal static readonly TimeSpan Delay = TimeSpan.Zero;

void Main()
{
	//Test();return;
	//Run(Workflows.Single_Easy);
	//Run(Workflows.Single_NeedsMergeToSolveConflict);
	Run(Workflows.Single_Rebase);
	//Run(Workflows.Complex_Rebase);
	
	Util.Cmd("git", "log --all --decorate --oneline --graph --date-order");
	
	//git.Repo.Head.Tip.MakeTree().Map(e => e.MessageShort.Truncate(15)).Display().Dump();
}

void Test()
{
	//Util.Cmd("git", "log --all --decorate --oneline --graph --date-order");
	//var git = new Git(RepoFolder);
	//var repo = git.Repo;
	var repo = new Repository(RepoFolder);
	var head = repo.Head;
	
	
	head.GetType().Name.Dump();
	
	//var tip = head.Tip;
	//tip.GetMasterLine().Select(e => $"[{e.Sha.Truncate(7)}]-{e.MessageShort}").Dump();
	
}

static class TreeUtils
{
	public static Commit[] GetMasterLine(this Commit commit)
	{
		var list = new List<Commit>();
		while (true)
		{
			list.Add(commit);
			if (commit.Parents.Any())
				commit = commit.Parents.First();
			else
				break;
		}
		return list.ToArray();
	}
}


/*
https://en.wikipedia.org/wiki/Box-drawing_character



			│
			■───╮
│     		│   │
■───╮ 		m2  │
│   │ 		│ ╭─■
m2  │ 		│ │ │
│   c1		├─╯ c1
m1  │ 		m1  │
│   c0		│   c0
│   │ 		│   │
m0──╯ 		m0──╯
│     		│
⬤     		⬤



*/




static class Workflows
{
	//	│     
	//	■───╮ 
	//	│   │ 
	//	m2  │ 
	//	│   c1
	//	m1  │ 
	//	│   c0
	//	│   │ 
	//	m0──╯ 
	//	│     
	//	⬤     
	public static void Single_Easy(Git git)
	{
									git.Branch("cool");
									git.Commit("c0");
		git.Checkout("master");
		git.Commit("m1");
									git.Checkout("cool");
									git.Commit("c1");
		git.Checkout("master");
		git.Commit("m2");
									git.Checkout("cool");
									git.MergeInto("master");
	}
	
	//	│
	//	■───╮
	//	│   │
	//	m2  │
	//  │ ╭─■
	//  │ │ │
	//	├─╯ c1
	//	m1  │
	//	│   c0
	//	│   │
	//	m0──╯
	//	│
	//	⬤
	public static void Single_NeedsMergeToSolveConflict(Git git)
	{
									git.Branch("cool");
									git.Commit("c0");
		git.Checkout("master");
		git.Commit("m1");
									git.Checkout("cool");
									git.Commit("c1");
									
		git.Checkout("master");
		git.MergeInto("cool");
									
		git.Checkout("master");
		git.Commit("m2");
									git.Checkout("cool");
									git.MergeInto("master");
	}
	
	public static void Single_Rebase(Git git)
	{
									git.Branch("cool");
									git.Commit("c0");
		git.Checkout("master");
		git.Commit("m1");
									git.Checkout("cool");
									git.Commit("c1");
		git.Checkout("master");
		git.Commit("m2");
									git.Checkout("cool");
		
		git.Rebase("master");
		git.Checkout("master");
		git.Rebase("cool");
	}
	

	//	    a3*
	//	    │   b1
	//	m2  │   │
	//  │   a2  │
	//  │   │   b0
	//	│   a1──╯
	//	m1  │
	//	│   a0
	//	m0──╯
	//	│
	//	⬤
	public static void Complex_Rebase(Git git)
	{
		void Com(string branchName, string str)
		{
			if (branchName != git.CurBranchName) git.Checkout(branchName);
			git.Commit(str);
		}
		
		git.Branch("aaa");
		
		Com("aaa"   , "a0");
		Com("master", "m1");
		Com("aaa"   , "a1");
		
		git.Branch("bbb");
		
		Com("bbb"   , "b0");
		Com("aaa"   , "a2");
		Com("master", "m2");
		Com("bbb"   , "b1");
		Com("aaa"   , "a3");
		
		//git.Rebase("master");
		git.Checkout("bbb");
		git.MergeInto("aaa");
		git.MergeInto("master");
	}
}



void Run(Action<Git> action)
{
	var git = new Git(RepoFolder);
	git.Commit("init");
	git.Commit("m0");
	action(git);
}



static object ToDump(object o) => o switch
{
	Signature e => e.Name,
	ObjectId e => e.Sha.Truncate(7),
	Commit e => new
	{
		Sha = e.Sha.Truncate(7),
		Id = e.Id,
		Message = e.Message,
		Autor = e.Author,
		Parents = e.Parents,
		//Tree = e.Tree,
	},
	/*Commit e => new
	{
		Id = e.Sha,
		Msg = e.Message,
		User = e.Author.Name,
	},*/
	_ => o
};



static class Ids
{
	public static readonly Signature Vlad = new("vlad", "vlad@gmail.com", DateTimeOffset.Now);
	public static readonly Signature Erik = new("erik", "erik@gmail.com", DateTimeOffset.Now);
}

class Git
{
	private readonly string rootFolder;
	private int commitIdx;
	private int fileIdx;

	public Repository Repo { get; }
	
	public string CurBranchName => Repo.Head.FriendlyName;
	
	public Branch GetBranch(string branchName) =>
		Repo.Branches
			.First(e => e.FriendlyName == branchName);

	public Git(string rootFolder)
	{
		this.rootFolder = rootFolder;
		FileUtils.EmptyFolder(rootFolder);
		Directory.SetCurrentDirectory(rootFolder);
		Repository.Init(rootFolder);
		Repo = new Repository(rootFolder);
	}
	
	public void AddFile()
	{
		var file = Path.Combine(rootFolder, $"file_{fileIdx}.txt");
		File.WriteAllText(file, $"file_{fileIdx}");
		fileIdx++;
		Commands.Stage(Repo, "*");
	}

	public void Commit(string str, Signature? sig = null)
	{
		sig ??= Ids.Vlad;
		AddFile();
		Repo.Commit($"[{commitIdx}] {str}", sig, sig, new CommitOptions
		{
			AllowEmptyCommit = true,
		});
		commitIdx++;
		Thread.Sleep(Delay);
	}

	public void Branch(string branchName)
	{
		var branch = Repo.CreateBranch(branchName);
		Checkout(branchName);
		Thread.Sleep(Delay);
	}

	public void Checkout(string branchName) => Commands.Checkout(Repo, branchName);
	
	public void MergeInto(string branchDst, Signature? sig = null)
	{
		sig ??= Ids.Vlad;
		var branchSrc = CurBranchName;
		if (branchDst == branchSrc) throw new ArgumentException($"Cannot merge a branch into itself ({branchSrc})");
		Checkout(branchDst);
		Repo.Merge(branchSrc, sig, new MergeOptions
		{
			FastForwardStrategy = FastForwardStrategy.Default,
		});
		Thread.Sleep(Delay);
	}
	
	public void Rebase(string branchDst, string? branchSrc = null, Signature? sig = null)
	{
		sig ??= Ids.Vlad;
		var committer = new Identity(sig.Name, sig.Email);
		
		Branch? Get(string? s) => s switch
		{
			not null => GetBranch(s),
			null => null
		};
		
		//				(upstream)	(branch)
		// git rebase	branchDst	branchSrc
		var rebaseResult = Repo.Rebase.Start(
			branch:		Get(branchSrc),
			upstream:	GetBranch(branchDst),
			onto:		null, // defaults to upstream
			committer,
			new RebaseOptions
			{
				
			}
		);
	}
}



static class GitUtils
{
	public static TNod<Commit> MakeTree(this Commit rootCommit)
	{
		TNod<Commit> Recurse(Commit c) => Nod.Make(c, c.Parents.Select(Recurse));
		return Recurse(rootCommit);
	}
}



static class FileUtils
{
	public static void EmptyFolder(string folder)
	{
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		foreach (var file in Directory.GetFiles(folder)) Util.Cmd("del", $"\"{file}\"");
		foreach (var dir in Directory.GetDirectories(folder)) Util.Cmd("rmdir", $"/s /q \"{dir}\"");
	}
}