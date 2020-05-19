// <copyright file="Program.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Tweetinvi;

namespace Prime23.AutoRetweeter
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (IsDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets<TwitterSettings>();
            }

            Configuration = builder.Build();

            var serviceProvider = new ServiceCollection()
                .Configure<TwitterSettings>(Configuration.GetSection("Twitter"))
                .Configure<RetweetSettings>(Configuration.GetSection("Retweet"))
                .Configure<LikeSettings>(Configuration.GetSection("Like"))
                .AddOptions()
                .AddSingleton<TimeLineMonitor>()
                .BuildServiceProvider();

            var monitor = serviceProvider.GetService<TimeLineMonitor>();
            monitor.Start();
        }

        private static bool IsDevelopment
        {
            get
            {
 #if DEBUG
                return System.Diagnostics.Debugger.IsAttached;
#else
                return false;
#endif
            }
        }

        public static IConfiguration Configuration { get; set; }
    }
}