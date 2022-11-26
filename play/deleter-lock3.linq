<Query Kind="Program" />

public static FileStream fs = null!;

void Main()
{
	const string folder = @"C:\tmp\folder-lock\fold_1";
	
	var file = Path.Combine(folder, "lock.txt");
	
	fs = File.OpenWrite(file);
}

