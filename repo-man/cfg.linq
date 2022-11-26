<Query Kind="Program" />

public static class Cfg
{
	public static readonly string[] Solutions = {
		//@"C:\tmp\GitTest",
		@"C:\Dev\sharper-tools",
		@"C:\Dev_Nuget\Libs\PowBasics",
		@"C:\Dev_Nuget\Libs\PowMaybe",
		@"C:\Dev_Nuget\Libs\PowRxVar",
		@"C:\Dev_Nuget\Libs\LINQPadExtras",
		@"C:\Dev_Nuget\Libs\PowTrees",
		@"C:\Dev_Nuget\Libs\PowWeb",
		@"C:\Dev_Nuget\Libs\ParserLib",
		@"C:\Dev_Nuget\Libs\ImdbLib",
		@"C:\Dev_Nuget\Libs\WinFormsCtrlLibs",
	};
	
	public static class Git
	{
		public const string Name = "Vlad Niculescu";
		public const string Email = "vlad.nic@gmail.com";
	}
	
	public static class GitHub
	{
		public const string Owner = "vlad2048";
		public const string ProductHeaderValue = "repo-man";
		public const string Username = "vlad.nic@gmail.com";
		public static string Token => Util.GetPassword("github-token"); // Generate new token: https://github.com/settings/tokens/new
	}
	
	public static class Nuget
	{
		public const string GlobalPackageFolder = @"C:\Users\vlad\.nuget\packages";
		public const string LocalPackageFolder = @"C:\Dev_Nuget\packages";
		public const string RemoteRepoUrl = "https://api.nuget.org/v3/index.json";
		public static string ApiKey => Util.GetPassword("nuget");
	}
	
	public static class Tools
	{
		public const string VSCode = @"C:\Program Files\Microsoft VS Code\Code.exe";
	}
}