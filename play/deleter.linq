<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

void Main()
{
	const string folder = @"C:\tmp\folder-lock";
	
	//Con.Root.Dump();
	//Con.CheckForFolderLocks(folder);
	//Con.DeleteFolder(folder);
	
	GetFolderLockers(folder).Dump();
}

public record ProcNfo(
	int Id,
	string Exe,
	string ExeFolder,
	string? Title
);

public static ProcNfo[] GetFolderLockers(string folder) =>
	LockFinder.WhoIsLockingFolder(folder)
		.SelectToArray(proc =>
		{
			var exeFilename = proc.MainModule?.FileName;
			var (exe, exeFolder) = exeFilename switch
			{
				null => ("", ""),
				not null => (Path.GetFileName(exeFilename), Path.GetDirectoryName(exeFilename) ?? "")
			};
			
			return new ProcNfo(
				proc.Id,
				exe,
				exeFolder,
				proc.MainWindowTitle
			);
		}
		);















