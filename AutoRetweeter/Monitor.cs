// <copyright file="Monitor.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

using Prime23.AutoRetweeter.Models;

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

            var apiKeys = monitorSettings.TwitterSettings;
            Auth.SetUserCredentials(apiKeys.ConsumerKey, apiKeys.ConsumerSecret, apiKeys.AccessToken, apiKeys.AccessTokenSecret);

            rateLimitHandler.Initialize();
        }

        private static string GetHashtags(ITweet tweet)
        {
            return string.Join(",", tweet.Hashtags.Select(ht => ht.Text)).ToLower();
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

                var screenName = tweet.CreatedBy.UserIdentifier.ScreenName.ToLower();
                var hashTags = GetHashtags(tweet);

                if (tweet.PossiblySensitive)
                {
                    this.logger.LogWarning("Tweet ID: {0} by {1} is possibly sensitive, ignoring.", tweet.Id, screenName);
                    continue;
                }

                if (!this.monitorSettings.ActionLookup.TryGetValue(screenName, out var tagGroupAction))
                {
                    continue;
                }

                this.logger.LogDebug("Tweet ID: {0} matches user ({1})", tweet.Id, screenName);

                if (tagGroupAction.HashTags.Count == 0)
                {
                    this.ProcessAction(tagGroupAction, tweet, screenName);
                }
                else
                {
                    foreach (var hashTag in tagGroupAction.HashTags.Where(hashTags.Contains))
                    {
                        this.ProcessAction(tagGroupAction, tweet, screenName, hashTag);

                        break;
                    }
                }
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

        private void ProcessAction(TagGroupAction tagGroupAction, ITweet tweet, string screenName, string hashTag = null)
        {
            if (tagGroupAction.Like)
            {
                if (string.IsNullOrEmpty(hashTag))
                {
                    this.logger.LogInformation(
                        "Tweet ID: {0} matches user ({1}), LIKE!",
                        tweet.Id,
                        screenName);
                }
                else
                {
                    this.logger.LogInformation(
                        "Tweet ID: {0} matches user ({1}) and hashtag ({2}), LIKE!",
                        tweet.Id,
                        screenName,
                        hashTag);
                }

                Tweet.FavoriteTweet(tweet.Id);
            }

            if (!tagGroupAction.Retweet)
            {
                return;
            }

            if (tweet.IsRetweet)
            {
                this.logger.LogDebug("Tweet ID: {0} by {1} is a retweet, ignoring.", tweet.Id, screenName);
            }
            else
            {
                if (string.IsNullOrEmpty(hashTag))
                {
                    this.logger.LogInformation(
                        "Tweet ID: {0} matches user ({1}), RETWEET!",
                        tweet.Id,
                        screenName);
                }
                else
                {
                    this.logger.LogInformation(
                        "Tweet ID: {0} matches user ({1}) and hashtag ({2}), RETWEET!",
                        tweet.Id,
                        screenName,
                        hashTag);
                }

                Tweet.PublishRetweet(tweet.Id);
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