using ExploreLib.Loading;

namespace Play;

static class Program
{
	public static void Main()
	{
		var dllFile = @"C:\Users\vlad\.nuget\packages\nuget.protocol\6.4.0\lib\net5.0\NuGet.Protocol.dll";
		var typSet = TypSetLoader.Load(dllFile);
	}
}