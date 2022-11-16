<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <NuGetReference>PowRxVar</NuGetReference>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Reactive.Linq</Namespace>
</Query>

#load "..\libs\api-git"

void Main()
{
	var folder = @"C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras";
	var watcher = new FolderWatcher(folder);
	watcher.WhenChange.Subscribe(e => e.Dump());
	Util.ReadLine();
}


public class FolderWatcher : IDisposable
{
	private static readonly TimeSpan DebounceTime = TimeSpan.FromMilliseconds(500);
	private readonly Disp d = new();
	public void Dispose() => d.Dispose();
	
	public IObservable<string> WhenChange { get; }
	
	public FolderWatcher(string folder)
	{
		var ignoreSet = ApiGit.GetIgnoreSetOpt(folder);
		
		var watcher = new FileSystemWatcher(folder)
		{
			IncludeSubdirectories = true,
			NotifyFilter = NotifyFilters.LastWrite
		}.D(d);
		WhenChange = Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Changed")
			.Select(e => e.EventArgs)
			.Where(e => e.Name != null)
			.Select(e => e.FullPath)
			.Where(IsRelPathRelevant)
			.Where(ignoreSet.IsNotIgnored)
			.Throttle(DebounceTime);
		watcher.EnableRaisingEvents = true;
	}
	
	private static bool IsRelPathRelevant(string relPath) =>
		!relPath.Contains(@"\obj") &&
		!relPath.Contains(@"\bin") &&
		!relPath.Contains(@"\.vs")
		;
}




