using Mono.Cecil;

namespace ExploreLib.Structs;

public class Meth
{
	public MethodDefinition Def { get; }

	public string Name { get; }
	public string Ret { get; }
	public MethParam[] Params { get; }
	public string ParamsStr { get; }
	public string FullStr { get; }

	public Meth(MethodDefinition def)
	{
		Def = def;

		Name = Def.GetName();
		Ret = Def.GetRet();
		Params = Def.GetParams();
		(ParamsStr, FullStr) = this.GetParamsAndFullStr();
	}
}