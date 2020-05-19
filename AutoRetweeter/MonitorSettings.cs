// <copyright file="MonitorSettings.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Options;

namespace Prime23.AutoRetweeter
{
    public sealed class MonitorSettings
    {
        private static readonly char[] DelimiterChars =
        {
            ' ', ',', ';', '\t'
        };

        public MonitorSettings(
            IOptions<RetweetSettings> retweetSettings,
            IOptions<LikeSettings> likeSettings)
        {
            this.RetweetHashTags = SplitToList(retweetSettings.Value.HashTags);
            this.RetweetUsers = SplitToList(retweetSettings.Value.Users);

            this.LikeHashTags = SplitToList(likeSettings.Value.HashTags);
            this.LikeUsers = SplitToList(likeSettings.Value.Users);
        }

        public IList<string> LikeHashTags { get; }

        public IList<string> LikeUsers { get; }

        public IList<string> RetweetHashTags { get; }

        public IList<string> RetweetUsers { get; }

        private static List<string> SplitToList(string value)
        {
            return value.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}