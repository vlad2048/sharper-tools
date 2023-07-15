<Query Kind="Program">
  <NuGetReference>PowBasics</NuGetReference>
  <Namespace>PowBasics.ColorCode</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>PowBasics.ColorCode.Utils</Namespace>
</Query>

static string? file;

void Main()
{
	System.Threading.Thread.CurrentThread.Name = "Main";
	
	var wasOpen = file != null;
	file ??= Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}", "index.html").CreateFolderForFileIFN();
	file.Dump();
	
	var w = new TxtWriter();
	w.Log("First");
	w.Log("Second");
	w.Log("Interesting");
	
	w.Txt.RenderToHtml(file, typeof(C));
	if (!wasOpen) Utils.OpenHtml(file);
}

public static void S(int ms) => Thread.Sleep(ms);
public static class C
{
	public static readonly Color Thread = Color.Purple;
	public static readonly Color Time = Color.Yellow;
}

static class Utils
{
	public static void OpenHtml(string file)
	{
		var procNfo = new ProcessStartInfo
		{
			FileName = "chrome",
			Arguments = file,
			UseShellExecute = true,
		};
		Process.Start(procNfo);
	}
	
	public static void Log(this ITxtWriter w, string str)
	{
		w.Write(Thread, C.Thread);
		w.Write($"{Timestamp} ", C.Time);
		w.WriteLine(str, Color.DodgerBlue);
	}
	
	public static string CreateFolderForFileIFN(this string file)
	{
		var folder = Path.GetDirectoryName(file) ?? throw new ArgumentException();
		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		return file;
	}
	
	
	private static string Thread {
		get {
			var thread = System.Threading.Thread.CurrentThread;
			var name = thread.Name ?? "(unnamed)";
			if (name == ".NET ThreadPool Worker") name = "ThreadPool";
			name = name.PadRight(10);

			var str = $"{thread.ManagedThreadId}/{name}";
			return $"[{str}]";
		}
	}

	private static string Timestamp => $"[{DateTime.Now:HH:mm:ss.fffffff}]";
}


