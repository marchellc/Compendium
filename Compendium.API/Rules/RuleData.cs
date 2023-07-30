using System;

namespace Compendium.Rules
{
    public class RuleData
    {
        public TimeSpan[] StrikeTimes { get; set; } = new TimeSpan[10];

        public string Name { get; set; }
        public string Text { get; set; }

        public double Number { get; set; }
    }
}