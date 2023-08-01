﻿using System;

namespace Compendium.Round
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RoundStateChangedAttribute : Attribute
    {
        public RoundState[] TargetStates { get; } = Array.Empty<RoundState>();

        public RoundStateChangedAttribute(params RoundState[] applicableStates)
        {
            TargetStates = applicableStates;
        }
    }
}