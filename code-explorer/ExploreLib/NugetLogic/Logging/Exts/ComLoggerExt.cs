using ExploreLib.NugetLogic.Logging.Loggers.Base;
using NuGet.Common;

namespace ExploreLib.NugetLogic.Logging.Exts;

static class ComLoggerExt
{
	public static ILogger MakeNugetLogger(this IComLogger comLogger) => new NugetLogger(comLogger);


	private class NugetLogger : LoggerBase
	{
		private readonly IComLogger comLogger;

		public NugetLogger(IComLogger comLogger)
		{
			this.comLogger = comLogger;
		}

		public override void Log(ILogMessage msg) => L(msg.Level, $"[{msg.Time:HH:mm:ss.fff}] {msg.Message}");
		public override Task LogAsync(ILogMessage msg) => throw new NotImplementedException();

		private void L(LogLevel logLevel, string s) => comLogger.Log(logLevel, $"[NUGET] - {s}");
	}
}