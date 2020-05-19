// <copyright file="TwitterSettings.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

namespace Prime23.AutoRetweeter
{
    public sealed class TwitterSettings
    {
        public string AccessToken { get; set; }

        public string AccessTokenSecret { get; set; }

        public string ConsumerKey { get; set; }

        public string ConsumerSecret { get; set; }
    }
}