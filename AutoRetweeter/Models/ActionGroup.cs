// <copyright file="ActionGroup.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

namespace Prime23.AutoRetweeter.Models
{
    public sealed class ActionGroup
    {
        public string Like { get; set; }

        public string Retweet { get; set; }

        public string TagGroups { get; set; }

        public string UserGroup { get; set; }
    }
}