using ExploreLib.NugetLogic.Logging.Loggers.Base;
using NuGet.Common;

namespace ExploreLib.NugetLogic.Logging.Loggers;

public class NullComLogger : IComLogger
{
	public void Log(LogLevel logLevel, string msg) { }
}