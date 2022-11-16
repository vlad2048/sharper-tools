using NuGet.Common;

namespace ExploreLib.NugetLogic.Logging.Loggers.Base;

public interface IComLogger
{
	void Log(LogLevel logLevel, string msg);
}