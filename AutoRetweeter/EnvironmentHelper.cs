// <copyright file="EnvironmentHelper.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2020 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Diagnostics;

namespace Prime23.AutoRetweeter
{
    internal static class EnvironmentHelper
    {
        public static bool InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        public static bool IsDevelopment
        {
            get
            {
#if DEBUG
                return Debugger.IsAttached;
#else
                return false;
#endif
            }
        }
    }
}