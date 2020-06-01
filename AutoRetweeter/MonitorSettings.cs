// <copyright file="MonitorSettings.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Prime23.AutoRetweeter.Models;

namespace Prime23.AutoRetweeter
{
    public sealed class MonitorSettings
    {
        private static readonly char[] DelimiterChars =
        {
            ' ', ',', ';', '\t'
        };
        private readonly AppSettings appSettings;
        private readonly ILogger<MonitorSettings> logger;

        public MonitorSettings(ILogger<MonitorSettings> logger, IConfiguration configuration)
        {
            this.logger = logger;
            var tagGroupLookup = GetTagGroups(configuration);
            this.ActionLookup = this.GetActionGroups(configuration, tagGroupLookup);

            this.appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

            this.TwitterSettings = this.GetTwitterSettings(configuration);
        }

        internal IDictionary<string, TagGroupAction> ActionLookup { get; }

        internal int BatchSize => this.appSettings.BatchSize;

        internal int PostBatchProcessingDelayInSeconds => this.appSettings.PostBatchProcessingDelayInSeconds;

        internal TwitterSettings TwitterSettings { get; }

        private static string GetStatus(string value)
        {
            return string.IsNullOrEmpty(value) ? "loaded ok" : "not loaded!";
        }

        private static Dictionary<string, List<string>> GetTagGroups(IConfiguration configuration)
        {
            var tagGroups = configuration.GetSection("TagGroups").Get<TagGroup[]>();
            var tagGroupLookup = new Dictionary<string, List<string>>();

            foreach (var tagGroup in tagGroups)
            {
                var hashTags = SplitToList(tagGroup.Tags.ToLower());
                tagGroupLookup.Add(tagGroup.Group, hashTags);
            }

            return tagGroupLookup;
        }

        private static List<string> SplitToList(string value)
        {
            return value.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private Dictionary<string, TagGroupAction> GetActionGroups(
            IConfiguration configuration,
            IReadOnlyDictionary<string, List<string>> tagGroupLookup)
        {
            var userGroups = configuration.GetSection("UserGroups").Get<UserGroup[]>();
            var actionLookup = new Dictionary<string, TagGroupAction>();

            foreach (var userGroup in userGroups)
            {
                var shouldLike = userGroup.Like.Equals("yes", StringComparison.OrdinalIgnoreCase);
                var shouldRetweet = userGroup.Retweet.Equals("yes", StringComparison.OrdinalIgnoreCase);

                if (!tagGroupLookup.TryGetValue(userGroup.TagGroup, out var hashTags))
                {
                    hashTags = new List<string>();
                }

                var tagGroupAction = new TagGroupAction(hashTags, shouldLike, shouldRetweet);

                var userNames = SplitToList(userGroup.Users);
                foreach (var userName in userNames)
                {
                    actionLookup.Add(userName.ToLower(), tagGroupAction);
                }
            }

            return actionLookup;
        }

        private TwitterSettings GetTwitterSettings(IConfiguration configuration)
        {
            TwitterSettings twitterSettings;

            if (EnvironmentHelper.InDocker)
            {
                twitterSettings = new TwitterSettings
                {
                    ConsumerKey = Environment.GetEnvironmentVariable("ConsumerKey"),
                    ConsumerSecret = Environment.GetEnvironmentVariable("ConsumerSecret"),
                    AccessToken = Environment.GetEnvironmentVariable("AccessToken"),
                    AccessTokenSecret = Environment.GetEnvironmentVariable("AccessTokenSecret")
                };
            }
            else
            {
                twitterSettings = configuration.GetSection("Twitter").Get<TwitterSettings>();
            }

            this.logger.LogInformation("Twitter Consumer Key {0}", GetStatus(twitterSettings.ConsumerKey));
            this.logger.LogInformation("Twitter Consumer Secret {0}", GetStatus(twitterSettings.ConsumerSecret));
            this.logger.LogInformation("Twitter Access Token {0}", GetStatus(twitterSettings.AccessToken));
            this.logger.LogInformation("Twitter Access Token Secret {0}", GetStatus(twitterSettings.AccessTokenSecret));

            return twitterSettings;
        }
    }
}