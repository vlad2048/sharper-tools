using ExploreLib.NugetLogic.Logging.Loggers.Base;
using NuGet.Common;

namespace ExploreLib.NugetLogic.Logging.Loggers;

public class ConComLogger : IComLogger
{
	private readonly LogLevel minLogLevel;
	private readonly object lockObj = new();

	public ConComLogger(LogLevel minLogLevel)
	{
		this.minLogLevel = minLogLevel;
	}

	public void Log(LogLevel logLevel, string msg)
	{
		if (logLevel < minLogLevel) return;
		/*lock (lockObj)
		{
			Console.WriteLine($"[{logLevel}] - {msg}");
		}*/
	}
}