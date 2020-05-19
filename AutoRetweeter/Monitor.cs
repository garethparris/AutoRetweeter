// <copyright file="Monitor.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tweetinvi;
using Tweetinvi.Models;

namespace Prime23.AutoRetweeter
{
    public sealed class Monitor
    {
        private readonly ILogger<Monitor> logger;
        private readonly MonitorSettings monitorSettings;

        public Monitor(
            ILogger<Monitor> logger,
            MonitorSettings monitorSettings,
            IOptions<TwitterSettings> twitterSettings)
        {
            this.logger = logger;
            this.monitorSettings = monitorSettings;

            var apiKeys = twitterSettings.Value;
            Auth.SetUserCredentials(apiKeys.ConsumerKey, apiKeys.ConsumerSecret, apiKeys.AccessToken, apiKeys.AccessTokenSecret);
        }

        internal void Start()
        {
            var tweets = Timeline.GetHomeTimeline(maximumTweets: 50);

            foreach (var tweet in tweets)
            {
                this.logger.LogTrace(tweet.Text);

                if (tweet.PossiblySensitive)
                {
                    this.logger.LogDebug("Possibly sensitive tweet, ignoring.");
                    continue;
                }

                this.ProcessLikes(tweet);
                this.ProcessRetweets(tweet);
            }
        }

        private void ProcessLikes(ITweet tweet)
        {
            if (!this.monitorSettings.LikeUsers.Contains(tweet.CreatedBy.UserIdentifier.IdStr))
            {
                return;
            }

            foreach (var hashTag in this.monitorSettings.LikeHashTags
                .Where(hashTag => tweet.Hashtags.Exists(ht => ht.Text.Equals(hashTag, StringComparison.OrdinalIgnoreCase))))
            {
                this.logger.LogInformation("Matching user ({0}) and hashtag ({1}), LIKE!", tweet.CreatedBy.UserIdentifier.ScreenName, hashTag);
                Tweet.FavoriteTweet(tweet);
                break;
            }
        }

        private void ProcessRetweets(ITweet tweet)
        {
            if (tweet.IsRetweet)
            {
                this.logger.LogDebug("This is already a retweet, ignoring.");
                return;
            }

            if (!this.monitorSettings.RetweetUsers.Contains(tweet.CreatedBy.UserIdentifier.IdStr))
            {
                return;
            }

            foreach (var hashTag in this.monitorSettings.RetweetHashTags
                .Where(hashTag => tweet.Hashtags.Exists(ht => ht.Text.Equals(hashTag, StringComparison.OrdinalIgnoreCase))))
            {
                this.logger.LogInformation("Matching user ({0}) and hashtag ({1}), RETWEET!", tweet.CreatedBy.UserIdentifier.ScreenName, hashTag);
                Tweet.PublishRetweet(tweet);
                break;
            }
        }
    }
}