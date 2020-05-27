// <copyright file="Program.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Prime23.AutoRetweeter.Models;

using Serilog;

namespace Prime23.AutoRetweeter
{
    internal sealed class Program
    {
        private static IConfiguration Configuration { get; set; }

        private static bool IsDevelopment
        {
            get
            {
#if DEBUG
                return Debugger.IsAttached;
#else
                return false;
#endif
            }
        }

        private static void ConfigureLogging(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
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

            if (IsDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets<TwitterSettings>();
            }

            Configuration = builder.Build();

            var services = new ServiceCollection();
            ConfigureLogging(services);
            ConfigureScopes(services);

            var serviceProvider = services.BuildServiceProvider();
            var monitor = serviceProvider.GetService<Monitor>();

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            };

            Console.WriteLine("Press CTRL+C to Exit");

            while (true)
            {
                monitor.ProcessTimeline();
            }
        }
    }
}