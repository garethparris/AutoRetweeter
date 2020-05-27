// <copyright file="Monitor.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Prime23.AutoRetweeter
{
    public sealed class Monitor
    {
        private readonly ILogger<Monitor> logger;
        private readonly MonitorSettings monitorSettings;

        private long? currentBatchLowestTweetId;
        private long highestTweetId;

        public Monitor(ILogger<Monitor> logger, MonitorSettings monitorSettings, RateLimitHandler rateLimitHandler)
        {
            this.logger = logger;
            this.monitorSettings = monitorSettings;

            var apiKeys = monitorSettings.twitterSettings;
            Auth.SetUserCredentials(apiKeys.ConsumerKey, apiKeys.ConsumerSecret, apiKeys.AccessToken, apiKeys.AccessTokenSecret);

            rateLimitHandler.Initialize();
        }

        private static string GetHashtags(ITweet tweet)
        {
            return string.Join(",", tweet.Hashtags.Select(ht => ht.Text));
        }

        internal void ProcessTimeline()
        {
            var tweets = this.GetLatestTweets();
            if (tweets.Count == 0)
            {
                if (this.ResetAfterProcessingBatch())
                {
                    return;
                }

                var retryDelay = DateTime.UtcNow.AddSeconds(this.monitorSettings.PostBatchProcessingDelayInSeconds);

                this.logger.LogInformation(
                    "No new tweets yet, will retry in {0} seconds at: {1}",
                    this.monitorSettings.PostBatchProcessingDelayInSeconds,
                    retryDelay.ToLongTimeString());

                Thread.Sleep(this.monitorSettings.PostBatchProcessingDelayInSeconds * 1000);

                return;
            }

            this.logger.LogInformation("Downloaded {0} new tweets...", tweets.Count);

            foreach (var tweet in tweets)
            {
                if (!this.currentBatchLowestTweetId.HasValue ||
                    this.currentBatchLowestTweetId.Value > tweet.Id)
                {
                    this.currentBatchLowestTweetId = tweet.Id;
                }

                if (this.highestTweetId < tweet.Id)
                {
                    this.highestTweetId = tweet.Id;
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
        }

        private ICollection<ITweet> GetLatestTweets()
        {
            var homeTimelineParameters = new HomeTimelineParameters
            {
                MaximumNumberOfTweetsToRetrieve = this.monitorSettings.BatchSize, ExcludeReplies = false, IncludeEntities = true
            };

            if (this.currentBatchLowestTweetId.HasValue)
            {
                homeTimelineParameters.MaxId = this.currentBatchLowestTweetId.Value - 1;
            }

            if (this.highestTweetId > 0)
            {
                homeTimelineParameters.SinceId = this.highestTweetId;
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

        private bool ResetAfterProcessingBatch()
        {
            if (!this.currentBatchLowestTweetId.HasValue)
            {
                return false;
            }

            this.logger.LogInformation("Processing batch completed.");

            this.currentBatchLowestTweetId = null;
            return true;
        }
    }
}