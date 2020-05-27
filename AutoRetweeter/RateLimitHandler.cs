// <copyright file="RateLimitHandler.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Threading;

using Microsoft.Extensions.Logging;

using Tweetinvi;
using Tweetinvi.Events;

namespace Prime23.AutoRetweeter
{
    public sealed class RateLimitHandler : IDisposable
    {
        private readonly ILogger<Monitor> logger;

        public RateLimitHandler(ILogger<Monitor> logger)
        {
            this.logger = logger;
        }

        internal void Initialize()
        {
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackOnly;
            TweetinviEvents.QueryBeforeExecute += this.CheckRateLimits;
        }

        private void CheckRateLimits(object sender, QueryBeforeExecuteEventArgs args)
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

        public void Dispose()
        {
            TweetinviEvents.QueryBeforeExecute -= this.CheckRateLimits;
        }
    }
}