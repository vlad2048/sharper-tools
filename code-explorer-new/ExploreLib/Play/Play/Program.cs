using System.Diagnostics;
using ExploreLib._1_DllFinding;
using ExploreLib._1_DllFinding.Structs;
using ExploreLib._2_DllReading;
using ExploreLib._3_DllGraphing;
using ExploreLib.Utils;
using PowBasics.EqualityCode;

namespace Play;

static class Program
{
	class Rec
	{
		public int Num { get; }
		public string[] Names { get; }
	}

	public static void Main()
	{
		//var name = ExprTreeUtils.GetName<Rec>(e => e.Num);
		var name = ExprTreeUtils.GetName<Rec>(e => e.Names.Length);

		var abc = 123;

		/*var dllPowRxVar = DllNfo.FromFile(@"C:\Dev_Nuget\Libs\PowRxVar\Libs\PowRxVar\bin\Debug\net7.0\PowRxVar.dll");
		var dllReactive = DllNfo.FromFile(@"C:\Users\vlad\.nuget\packages\system.reactive\5.0.0\lib\net5.0\System.Reactive.dll");
		var types = DllReader.Read(dllReactive);
		var graphs = types.BuildGraphs();

		var ts = types.Where(e => e.FullName.StartsWith("System.Reactive.Linq")).ToArray();


		var abc = 123;*/

		//var dll = @"C:\Dev_Nuget\Libs\PowRxVar\Libs\PowRxVar\bin\Debug\net7.0\PowRxVar.dll";
		//var graphs = DllReader.Read(new DllNfo("PowRxVar", Version.Parse("1.0.0"), dll, 0)).BuildGraphs();
		//var g = graphs.InterfaceRoots[0];

		/*var cnt = 0;
		var dll =
			DllFinder.Dlls
				.MaxBy(dll =>
				{
					var res = DllReader.Read(dll).BuildGraphs().InterfaceRoots.SafeMax(e => e.Count());
					cnt++;
					return res;
				});

		Console.WriteLine(dll.Name);*/
	}

	private static int SafeMax<T>(this T[] arr, Func<T, int> f)
	{
		return arr.Length switch
		{
			0 => 0,
			_ => arr.Max(f)
		};
	}
}