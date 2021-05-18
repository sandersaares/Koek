using Koek;
using Microsoft.Extensions.Logging;
using System;

namespace Tests
{
    public abstract class KoekTests : BaseTestClass, IDisposable
    {
        public KoekTests()
        {
            _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            _logger = _loggerFactory.CreateLogger("");
        }

        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;

        public virtual void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}
