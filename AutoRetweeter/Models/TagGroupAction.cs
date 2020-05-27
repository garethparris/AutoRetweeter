// <copyright file="TagGroupAction.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System.Collections.Generic;

namespace Prime23.AutoRetweeter.Models
{
    public sealed class TagGroupAction
    {
        public TagGroupAction(List<string> hashTags, bool like, bool retweet)
        {
            this.HashTags = hashTags;
            this.Like = like;
            this.Retweet = retweet;
        }

        public List<string> HashTags { get; }

        public bool Like { get; }

        public bool Retweet { get; }
    }
}