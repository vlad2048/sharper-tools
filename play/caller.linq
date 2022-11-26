<Query Kind="Program">
  <NuGetReference>PowRxVar</NuGetReference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>System.Runtime.CompilerServices</Namespace>
</Query>

#load ".\libs\caller-lib"


void Main()
{
	var a = V.Make(5);
	"____________".Dump();
	Sub.Check();
	"____________".Dump();
	Sub.Nested();
}


public static class V
{
	public static IRwVar<T> Make<T>(
		T initVal,
		[CallerFilePath] string? dbgFile = null,
		[CallerLineNumber] int? dbgLine = null,
		[CallerMemberName] string? dbgMember = null
	)
	{
		dbgFile.Dump();
		dbgLine.Dump();
		dbgMember.Dump();
		
		//Directory.GetFiles(Path.GetDirectoryName(dbgFile)).Dump();
		Util.CurrentQueryPath.Dump();
		
		return Var.Make(initVal);
	}
}