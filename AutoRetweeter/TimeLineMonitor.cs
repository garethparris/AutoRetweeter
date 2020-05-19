// <copyright file="TimeLineMonitor.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Options;

using Tweetinvi;
using Tweetinvi.Models;

namespace Prime23.AutoRetweeter
{
    public sealed class TimeLineMonitor
    {
        private static readonly char[] DelimiterChars =
        {
            ' ', ',', ';', '\t'
        };

        private readonly List<string> likeHashTags;
        private readonly List<string> likeUsers;
        private readonly List<string> retweetHashTags;
        private readonly List<string> retweetUsers;

        public TimeLineMonitor(
            IOptions<TwitterSettings> twitterSettings,
            IOptions<RetweetSettings> retweetSettings,
            IOptions<LikeSettings> likeSettings)
        {
            var apiKeys = twitterSettings.Value;
            Auth.SetUserCredentials(apiKeys.ConsumerKey, apiKeys.ConsumerSecret, apiKeys.AccessToken, apiKeys.AccessTokenSecret);

            this.retweetHashTags = SplitToList(retweetSettings.Value.HashTags);
            this.retweetUsers = SplitToList(retweetSettings.Value.Users);

            this.likeHashTags = SplitToList(likeSettings.Value.HashTags);
            this.likeUsers = SplitToList(likeSettings.Value.Users);
        }

        private static List<string> SplitToList(string value)
        {
            return value.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public void Start()
        {
            var tweets = Timeline.GetHomeTimeline(maximumTweets: 50);

            foreach (var tweet in tweets)
            {
                if (tweet.PossiblySensitive)
                {
                    Console.WriteLine("Possibly sensitive tweet, ignoring.");
                    continue;
                }

                this.ProcessLikes(tweet);
                this.ProcessRetweets(tweet);
            }
        }

        private void ProcessLikes(ITweet tweet)
        {
            if (!this.likeUsers.Contains(tweet.CreatedBy.UserIdentifier.IdStr))
            {
                return;
            }

            if (tweet.Hashtags.Any(
                hashTag =>
                    this.likeHashTags.Exists(ht => ht.Equals(hashTag.Text, StringComparison.OrdinalIgnoreCase))))
            {
                Console.WriteLine("Matching user and hashtag, LIKE!");
                Tweet.FavoriteTweet(tweet);
            }
        }

        private void ProcessRetweets(ITweet tweet)
        {
            if (tweet.IsRetweet)
            {
                Console.WriteLine("This is already a retweet, ignoring.");
                return;
            }

            if (!this.retweetUsers.Contains(tweet.CreatedBy.UserIdentifier.IdStr))
            {
                return;
            }

            if (tweet.Hashtags.Any(
                hashTag =>
                    this.retweetHashTags.Exists(ht => ht.Equals(hashTag.Text, StringComparison.OrdinalIgnoreCase))))
            {
                Console.WriteLine("Matching user and hashtag, RETWEET!");
                Tweet.PublishRetweet(tweet);
            }
        }
    }
}