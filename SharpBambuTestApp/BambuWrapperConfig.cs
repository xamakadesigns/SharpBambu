using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpBambuTestApp
{
    internal class BambuWrapperConfig
    {
        public static ILogger Log { get; private set; } = Serilog.Log.ForContext<BambuWrapperConfig>();
        internal BambuWrapperConfig Load()
        {
            Log.Debug("Load appsettings.json and appsettings.secret.json");

            var configRoot = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.secret.json", optional: true)
                    .Build();

            // configure SeriLog globally, the static logger
            Serilog.Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .CreateLogger();

            Log = Serilog.Log.ForContext<BambuWrapperConfig>();

            var config = configRoot.AsEnumerable().ToDictionary(e => e.Key, e => e.Value);
            
            Username = config["network:username"] ?? "";
            Password = config["network:password"] ?? "";
            IpAddress = config["network:ipAddress"] ?? "";
            DeviceId = config["network:deviceId"] ?? "";

            var result = bool.TryParse(config["network:copyNetworkingDllFromBambuStudio"], out bool copyNetworkingDllFromBambuStudio);
            copyNetworkingDllFromBambuStudio = copyNetworkingDllFromBambuStudio;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(IpAddress))
                throw new Exception("Please edit 'appsttings.json' with the details from the printer's lan mode screen.");

            return this;
        }

        public string Username { get; private set; } = "";
        public string Password { get; private set; } = "";
        public string IpAddress { get; private set; } = "";
        public string DeviceId { get; private set; } = "";
        public bool copyNetworkingDllFromBambuStudio { get; private set; }
    }

    class CallerEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var skip = 3;
            while (true)
            {
                var stack = new StackFrame(skip);
                if (!stack.HasMethod())
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue("<unknown method>")));
                    return;
                }

                var method = stack.GetMethod();
                if (method.DeclaringType.Assembly != typeof(Log).Assembly)
                {
                    //var caller = $"{method.DeclaringType.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(pi => pi.ParameterType.FullName))})";
                    var caller = method.Name;
                    logEvent.AddPropertyIfAbsent(new LogEventProperty("Caller", new ScalarValue(caller)));
                    return;
                }

                skip++;
            }
        }
    }

    static class LoggerCallerEnrichmentConfiguration
    {
        public static LoggerConfiguration WithCaller(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<CallerEnricher>();
        }
    }
}
