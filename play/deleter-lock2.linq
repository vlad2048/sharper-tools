<Query Kind="Program" />

public static FileStream fs = null!;

void Main()
{
	const string folder = @"C:\tmp\folder-lock\fold_0";
	
	var file = Path.Combine(folder, "lock2.txt");
	
	fs = File.OpenWrite(file);
}

