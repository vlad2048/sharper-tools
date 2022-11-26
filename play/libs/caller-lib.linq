<Query Kind="Program">
  <Namespace>System.Runtime.CompilerServices</Namespace>
</Query>

void Main()
{
	
}



public static class Sub
{
	public static void Check(
		[CallerFilePath] string? dbgFile = null,
		[CallerLineNumber] int? dbgLine = null,
		[CallerMemberName] string? dbgMember = null
	)
	{
		dbgFile.Dump();
		dbgLine.Dump();
		dbgMember.Dump();
		
		Util.CurrentQueryPath.Dump();
	}
	
	public static void Nested() => Check();
}