﻿// <copyright file="Program.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Diagnostics;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using Tweetinvi;

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
                .AddSingleton<MonitorSettings>()
                .AddSingleton<Monitor>();
        }

        private static void ConfigureSettings(IServiceCollection services)
        {
            services.AddOptions()
                .Configure<TwitterSettings>(Configuration.GetSection("Twitter"))
                .Configure<RetweetSettings>(Configuration.GetSection("Retweet"))
                .Configure<LikeSettings>(Configuration.GetSection("Like"))
                .Configure<AppSettings>(Configuration.GetSection("AppSettings"));
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

            ConfigureSettings(services);
            ConfigureLogging(services);
            ConfigureScopes(services);

            var serviceProvider = services.BuildServiceProvider();
            var monitor = serviceProvider.GetService<Monitor>();

            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
            TweetinviEvents.QueryBeforeExecute += monitor.CheckRateLimits;

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            };
            
            while (true)
            {
                Console.WriteLine("Press CTRL+C to Exit");

                monitor.ProcessTimeline();
            }
        }
    }
}