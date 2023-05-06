<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadExtras\Libs\LINQPadExtras\bin\Debug\net7.0-windows\LINQPadExtras.dll</Reference>
  <Namespace>LINQPadExtras</Namespace>
  <Namespace>LINQPadExtras.Utils</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
  <Namespace>LINQPadExtras.Scripting_LockChecker</Namespace>
</Query>

void Main()
{
	const string folder = @"C:\Dev_Nuget\Libs\LINQPadExtras\_infos\design";
	
	LockChecker.CheckFolders(folder).Dump();
}
