using CommandSystem;

using helpers.Extensions;

using System;
using System.Collections.Generic;

namespace Compendium.Helpers.Commands
{
    public static class CommandHelper
    {
        public static string[] EmptyAliases => Array.Empty<string>();

        public static Dictionary<string, string> UsageReplacements { get; } = new Dictionary<string, string>();

        public static bool ReturnUsage(this IUsageProvider command, out string response)
        {
            response = $"Missing arguments! Usage: {command.DisplayCommandUsage().ReplaceWithMap(UsageReplacements)}";
            return false;
        }

        public static void AddUsageReplacement(string original, string newString) => UsageReplacements[original] = newString;
    }
}