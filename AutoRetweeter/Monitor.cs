// <copyright file="Monitor.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Prime23.AutoRetweeter
{
    public sealed class Monitor
    {
        private const int TweetBatchCount = 20;
        private const int NoTweetsRetryDelayInSeconds = 60;

        private readonly ILogger<Monitor> logger;
        private readonly MonitorSettings monitorSettings;

        private long lastProcessedTweetId;
        private long sinceId;

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

        internal void CheckRateLimits(object sender, QueryBeforeExecuteEventArgs args)
        {
            var queryRateLimits = RateLimit.GetQueryRateLimit(args.QueryURL);

            // Some methods are not RateLimited. Invoking such a method will result in the queryRateLimits to be null
            if (queryRateLimits == null)
            {
                return;
            }

            if (queryRateLimits.Remaining > 0)
            {
                // We have enough resource to execute the query
                return;
            }

            // Wait for RateLimits to be available
            this.logger.LogInformation("Waiting for RateLimits until: {0}", queryRateLimits.ResetDateTime.ToLongTimeString());
            Thread.Sleep((int)queryRateLimits.ResetDateTimeInMilliseconds);
        }

        internal void ProcessTimeline()
        {
            var tweets = this.GetLatestTweets();
            if (tweets.Count == 0)
            {
                var retryDelay = DateTime.UtcNow.AddSeconds(NoTweetsRetryDelayInSeconds);
                this.logger.LogInformation("No new tweets yet, will retry at: {0}", retryDelay.ToLongTimeString());

                Thread.Sleep(NoTweetsRetryDelayInSeconds * 1000);
                return;
            }

            this.logger.LogDebug("Downloaded {0} new tweets...", tweets.Count);

            foreach (var tweet in tweets)
            {
                this.lastProcessedTweetId = tweet.Id;

                if (tweet.Id > this.sinceId)
                {
                    this.sinceId = tweet.Id;
                }

                if (tweet.PossiblySensitive)
                {
                    this.logger.LogDebug("Tweet ID: {0} is possibly sensitive, ignoring.", tweet.Id);
                    continue;
                }

                this.logger.LogDebug("Processing tweet ID: {0}", tweet.Id);

                this.ProcessLikes(tweet);
                this.ProcessRetweets(tweet);
            }

            if (tweets.Count < TweetBatchCount)
            {
                // we've come to the end of the processing batch, reset
                this.lastProcessedTweetId = 0;
            }
        }

        private ICollection<ITweet> GetLatestTweets()
        {
            var homeTimelineParameters = new HomeTimelineParameters
            {
                MaximumNumberOfTweetsToRetrieve = TweetBatchCount, ExcludeReplies = true
            };

            if (this.lastProcessedTweetId > 0)
            {
                homeTimelineParameters.MaxId = this.lastProcessedTweetId - 1;
            }

            if (this.sinceId > 0)
            {
                homeTimelineParameters.SinceId = this.sinceId;
            }

            var tweets = Timeline.GetHomeTimeline(homeTimelineParameters);
            return tweets != null ? tweets.ToList() : new List<ITweet>();
        }

        private void ProcessLikes(ITweet tweet)
        {
            if (!this.monitorSettings.LikeUsers.Any(
                name => name.Equals(tweet.CreatedBy.UserIdentifier.ScreenName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            foreach (var hashTag in this.monitorSettings.LikeHashTags
                .Where(hashTag => tweet.Hashtags.Exists(ht => ht.Text.Equals(hashTag, StringComparison.OrdinalIgnoreCase))))
            {
                this.logger.LogInformation(
                    "Tweet ID: {0} matches user ({1}) and hashtag ({2}), LIKE!",
                    tweet.Id,
                    tweet.CreatedBy.UserIdentifier.ScreenName,
                    hashTag);
                var success = Tweet.FavoriteTweet(tweet.Id);
                break;
            }
        }

        private void ProcessRetweets(ITweet tweet)
        {
            if (tweet.IsRetweet)
            {
                this.logger.LogDebug("Tweet ID: {0} is a retweet, ignoring.", tweet.Id);
                return;
            }

            if (!this.monitorSettings.RetweetUsers.Any(
                name => name.Equals(tweet.CreatedBy.UserIdentifier.ScreenName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            foreach (var hashTag in this.monitorSettings.RetweetHashTags
                .Where(hashTag => tweet.Hashtags.Exists(ht => ht.Text.Equals(hashTag, StringComparison.OrdinalIgnoreCase))))
            {
                this.logger.LogInformation(
                    "Tweet ID: {0} matches user ({1}) and hashtag ({2}), RETWEET!",
                    tweet.Id,
                    tweet.CreatedBy.UserIdentifier.ScreenName,
                    hashTag);
                var retweet = Tweet.PublishRetweet(tweet.Id);
                break;
            }
        }
    }
}