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
            var userGroups = configuration.GetSection("UserGroups").Get<UserGroup[]>();
            var tagGroups = configuration.GetSection("TagGroups").Get<TagGroup[]>();
            var actions = configuration.GetSection("Actions").Get<ActionGroup[]>();

            this.appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
            this.twitterSettings = configuration.GetSection("Twitter").Get<TwitterSettings>();

            var retweetSettings = configuration.GetSection("Retweet").Get<RetweetSettings>();
            var likeSettings = configuration.GetSection("Like").Get<LikeSettings>();

            this.RetweetHashTags = SplitToList(retweetSettings.HashTags);
            this.RetweetUsers = SplitToList(retweetSettings.Users);

            this.LikeHashTags = SplitToList(likeSettings.HashTags);
            this.LikeUsers = SplitToList(likeSettings.Users);
        }

        public int BatchSize => this.appSettings.BatchSize;

        public IList<string> LikeHashTags { get; }

        public IList<string> LikeUsers { get; }

        public int PostBatchProcessingDelayInSeconds => this.appSettings.PostBatchProcessingDelayInSeconds;

        public IList<string> RetweetHashTags { get; }

        public IList<string> RetweetUsers { get; }

        public TwitterSettings twitterSettings { get; }

        private static List<string> SplitToList(string value)
        {
            return value.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}