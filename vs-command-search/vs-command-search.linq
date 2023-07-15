<Query Kind="Program">
  <Namespace>PowRxVar</Namespace>
</Query>

#load "..\code-explorer-new\libs\ui"

void Main()
{
	var cmds = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, "commands.txt"));
	
	UI.DisplayList(cmds, e => e)
		.WithSearch(e => e)
		.WithPaging(32)
		.Build(D)
		.Dump();
}

