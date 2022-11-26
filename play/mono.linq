<Query Kind="Program">
  <NuGetReference>Mono.Cecil</NuGetReference>
  <Namespace>Attr = Mono.Cecil.FieldAttributes</Namespace>
  <Namespace>Mono.Cecil</Namespace>
</Query>

void Main()
{
	var dllFile = @"C:\Dev\sharper-tools\code-explorer\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll";
	using var ass = AssemblyDefinition.ReadAssembly(dllFile);
	var module = ass.Modules.Single();
	
	var typ = module.Types.Single(e => e.Name == "TypKind");
	
	var req = Attr.Public | Attr.Static | Attr.Literal | Attr.HasDefault;
	var names = typ.Fields
		.Where(e => e.Attributes == req)
		.Select(e => e.Name)
		.ToArray();
	
	names.Dump();
	
	//typ.Dump();
}


static object ToDump(object o) => o switch
{
	ModuleDefinition => null,
	_ => o
};

