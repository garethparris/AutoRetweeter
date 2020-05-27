// <copyright file="AppSettings.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

namespace Prime23.AutoRetweeter.Models
{
    public sealed class AppSettings
    {
        public int BatchSize { get; set; }

        public int PostBatchProcessingDelayInSeconds { get; set; }
    }
}