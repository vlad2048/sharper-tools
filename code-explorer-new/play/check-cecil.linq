<Query Kind="Program">
  <Output>DataGrids</Output>
  <Reference>C:\Dev\sharper-tools\code-explorer-new\ExploreLib\Libs\ExploreLib\bin\Debug\net7.0\ExploreLib.dll</Reference>
  <NuGetReference>Mono.Cecil</NuGetReference>
  <Namespace>Mono.Cecil</Namespace>
  <Namespace>ExploreLib._1_DllFinding.Structs</Namespace>
  <Namespace>ExploreLib._2_DllReading</Namespace>
  <Namespace>ExploreLib._2_DllReading.Structs</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

static readonly string[] searchPaths =
{
	@"C:\Users\vlad\.nuget\packages\powrxvar\0.0.15\lib\net7.0",
	@"C:\Dev\sharper-tools\code-explorer-new\ExploreLib\Play\CheckLib\bin\Debug\net7.0",
};
static DllNfo GetDll(string name) => (
	from searchPath in searchPaths
	let file = Path.Combine(searchPath, $"{name}.dll")
	where File.Exists(file)
	select DllNfo.FromFile(file)
).First();

void Main()
{
	var types = GetDll("PowRxVar").Read();
	
	//types.Select(e => e.FullName).Dump();return;
	
	//var t0 = types.Get("Cls");
	//var t1 = types.Get("Cls2");
	//Util.Dif(t0, t1, 2, true).Dump();
	
	var t1 = types.WhereToArray(e => e.FullName.Contains("RoVar`1"))[1].Ref;
	var t2 = types.WhereToArray(e => e.FullName.Contains("RwVar`1"))[1].Ref;
	var t3 = types.WhereToArray(e => e.FullName.Contains("RwDispBase"))[1].Ref;
	
	var def = t2;
	def.FullName.Dump();
	def.BaseType.FullName.Dump();
	
	t3.FullName.Dump();
	
	/*var exp = t2.BaseType;
	var act = t1;

	$"{t2.FullName} : {exp.FullName}".Dump();
	
	exp.FullName.Dump();
	act.FullName.Dump();*/
}

/*public static object ToDump(object o) => o switch
{
	TypeDefinition e => $"{e.Name} ({e.At})"
}*/
static class Ext
{
	public static TypeDef Get(this TypeDef[] types, string name) => types.First(e => e.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));
}



void OnStart() =>
	Util.HtmlHead.AddStyles(@"
		body {
			font-family: consolas;
		}
	");
