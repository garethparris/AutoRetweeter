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
        private const int NoTweetsRetryDelayInSeconds = 20;
        private const int TweetBatchCount = 200;

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

        private static string GetHashtags(ITweet tweet)
        {
            return string.Join(",", tweet.Hashtags.Select(ht => ht.Text));
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
                this.lastProcessedTweetId = 0;
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

                var screenName = tweet.CreatedBy.UserIdentifier.ScreenName;
                var hashTags = GetHashtags(tweet);

                if (tweet.PossiblySensitive)
                {
                    this.logger.LogWarning("Tweet ID: {0} by {1} is possibly sensitive, ignoring.", tweet.Id, screenName);
                    continue;
                }

                this.ProcessLikes(tweet.Id, screenName, hashTags);

                if (tweet.IsRetweet)
                {
                    this.logger.LogDebug("Tweet ID: {0} by {1} is a retweet, ignoring.", tweet.Id, screenName);
                    continue;
                }

                this.ProcessRetweets(tweet.Id, screenName, hashTags);
            }

            if (tweets.Count < TweetBatchCount)
            {
                this.logger.LogDebug("Tweet count {0} is less than batch size {1}, resetting max_id", tweets.Count, TweetBatchCount);

                // we've come to the end of the processing batch, reset
                this.lastProcessedTweetId = 0;
            }
        }

        private ICollection<ITweet> GetLatestTweets()
        {
            var homeTimelineParameters = new HomeTimelineParameters
            {
                MaximumNumberOfTweetsToRetrieve = TweetBatchCount, ExcludeReplies = true, IncludeEntities = true
            };

            if (this.lastProcessedTweetId > 0)
            {
                homeTimelineParameters.MaxId = this.lastProcessedTweetId - 1;
            }

            if (this.sinceId > 0)
            {
                homeTimelineParameters.SinceId = this.sinceId;
            }

            this.logger.LogDebug(
                "GetHomeTimeline: count = {0}, max_id = {1}, since_id = {2}",
                homeTimelineParameters.MaximumNumberOfTweetsToRetrieve,
                homeTimelineParameters.MaxId,
                homeTimelineParameters.SinceId);

            var tweets = Timeline.GetHomeTimeline(homeTimelineParameters);
            return tweets != null ? tweets.ToList() : new List<ITweet>();
        }

        private void ProcessLikes(long tweetId, string screenName, string hashTags)
        {
            this.logger.LogDebug("Processing likes    for tweet ID: {0} by {1}, hashtags: {2}", tweetId, screenName, hashTags);

            if (!this.monitorSettings.LikeUsers.Any(
                name => name.Equals(screenName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            this.logger.LogDebug("Tweet ID: {0} matches user ({1})", tweetId, screenName);

            foreach (var hashTag in this.monitorSettings.LikeHashTags
                .Where(hashTag => hashTags.Contains(hashTag, StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogInformation(
                    "Tweet ID: {0} matches user ({1}) and hashtag ({2}), LIKE!",
                    tweetId,
                    screenName,
                    hashTag);

                Tweet.FavoriteTweet(tweetId);
                break;
            }
        }

        private void ProcessRetweets(long tweetId, string screenName, string hashTags)
        {
            this.logger.LogDebug("Processing retweets for tweet ID: {0} by {1}, hashtags: {2}", tweetId, screenName, hashTags);

            if (!this.monitorSettings.RetweetUsers.Any(
                name => name.Equals(screenName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            this.logger.LogDebug("Tweet ID: {0} matches user ({1})", tweetId, screenName);

            foreach (var hashTag in this.monitorSettings.RetweetHashTags
                .Where(hashTag => hashTags.Contains(hashTag, StringComparison.OrdinalIgnoreCase)))
            {
                this.logger.LogInformation(
                    "Tweet ID: {0} matches user ({1}) and hashtag ({2}), RETWEET!",
                    tweetId,
                    screenName,
                    hashTag);

                Tweet.PublishRetweet(tweetId);
                break;
            }
        }
    }
}