// <copyright file="Program.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Prime23.AutoRetweeter.Models;

using Serilog;

namespace Prime23.AutoRetweeter
{
    internal sealed class Program
    {
        private static readonly AutoResetEvent WaitHandle = new AutoResetEvent(false);

        private static IConfiguration Configuration { get; set; }

        private static void ConfigureLogging(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            services.AddLogging(
                logging =>
                {
                    logging.AddSerilog();
                });
        }

        private static void ConfigureScopes(IServiceCollection services)
        {
            services
                .AddOptions()
                .AddSingleton(Configuration)
                .AddSingleton<RateLimitHandler>()
                .AddSingleton<MonitorSettings>()
                .AddSingleton<Monitor>();
        }

        private static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            if (EnvironmentHelper.IsDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets<TwitterSettings>();
            }

            Configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureLogging(services);
            ConfigureScopes(services);

            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger.LogInformation("Press CTRL+C to Exit");

            Task.Run(
                () =>
                {
                    var monitor = serviceProvider.GetService<Monitor>();

                    while (true)
                    {
                        monitor.ProcessTimeline();
                    }
                });

            Console.CancelKeyPress += (_, __) =>
            {
                logger.LogInformation("Exit");
                WaitHandle.Set();
            };

            WaitHandle.WaitOne();
        }
    }
}