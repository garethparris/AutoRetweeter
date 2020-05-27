// <copyright file="MonitorSettings.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Configuration;

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

        public MonitorSettings(IConfiguration configuration)
        {
            var tagGroups = configuration.GetSection("TagGroups").Get<TagGroup[]>();
            var tagGroupLookup = new Dictionary<string, List<string>>();
            foreach (var tagGroup in tagGroups)
            {
                var hashTags = SplitToList(tagGroup.Tags.ToLower());
                tagGroupLookup.Add(tagGroup.Group, hashTags);
            }

            var userGroups = configuration.GetSection("UserGroups").Get<UserGroup[]>();
            this.ActionLookup = new Dictionary<string, TagGroupAction>();

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
                    this.ActionLookup.Add(userName.ToLower(), tagGroupAction);
                }
            }

            this.appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            this.TwitterSettings = configuration.GetSection("Twitter").Get<TwitterSettings>();
        }

        public IDictionary<string, TagGroupAction> ActionLookup { get; }

        public int BatchSize => this.appSettings.BatchSize;

        public int PostBatchProcessingDelayInSeconds => this.appSettings.PostBatchProcessingDelayInSeconds;

        public TwitterSettings TwitterSettings { get; }

        private static List<string> SplitToList(string value)
        {
            return value.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}