using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers;

using System;
using System.Globalization;
using System.Collections.Generic;

using BetterCommands;
using BetterCommands.Permissions;

using System.Text;

using PluginAPI.Core;

using helpers.Time;

namespace Compendium.Rules
{
    public static class RuleSystem
    {
        private static SingleFileStorage<RuleData> _ruleStorage;

        public static IReadOnlyCollection<RuleData> Rules => _ruleStorage.Data;
        public static string Path => $"{Directories.ThisData}/SavedRules";

        [Reload]
        public static void Reload()
        {
            if (_ruleStorage is null)
            {
                _ruleStorage = new SingleFileStorage<RuleData>(Path);
                _ruleStorage.Load();

                Plugin.Info($"Loaded the rule database file.");
            }
            else
            {
                if (_ruleStorage.Path != Path)
                {
                    _ruleStorage.Save();
                    _ruleStorage = new SingleFileStorage<RuleData>(Path);
                    _ruleStorage.Load();

                    Plugin.Info($"Switched rule database file path to {Path}.");
                }
                else
                {
                    _ruleStorage.Load();
                    Plugin.Info($"Reloaded the rule database file.");
                }
            }
        }

        public static bool TryParseRules(string str, out RuleData[] rules)
        {
            var ruleList = new List<RuleData>();

            if (str.TrySplit(',', true, null, out var splits))
            {
                splits.ForEach(split =>
                {
                    if (TryGetRule(split, out var rule) && !ruleList.Contains(rule))
                        ruleList.Add(rule);
                    else if (double.TryParse(split, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var num))
                    {
                        if (TryGetRule(num, out rule) && !ruleList.Contains(rule))
                        {
                            ruleList.Add(rule);
                        }
                    }
                });
            }
            else
            {
                if (TryGetRule(str, out var rule) && !ruleList.Contains(rule))
                    ruleList.Add(rule);
                else if (double.TryParse(str, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var num))
                {
                    if (TryGetRule(num, out rule) && !ruleList.Contains(rule))
                    {
                        ruleList.Add(rule);
                    }
                }
            }

            rules = ruleList.ToArray();
            return rules.Any();
        }

        public static bool TryGetRule(double number, out RuleData ruleData)
            => Rules.TryGetFirst(r => r.Number == number, out ruleData);

        public static bool TryGetRule(string path, out RuleData ruleData)
            => Rules.TryGetFirst(r => r.Name == path, out ruleData);

        public static bool TryAddRule(double number, string name, string text, params TimeSpan[] strikes)
        {
            if (TryGetRule(name, out _))
                return false;

            if (TryGetRule(number, out _))
                return false;

            var data = new RuleData()
            {
                Name = name,
                Number = number,
                StrikeTimes = strikes,
                Text = text
            };

            _ruleStorage.Add(data);
            return true;
        }

        public static bool TryRemoveRule(double number)
        {
            if (TryGetRule(number, out var rule))
            {
                _ruleStorage.Remove(rule);
                return true;
            }

            return false;
        }

        public static bool TryRemoveRule(string name)
        {
            if (TryGetRule(name, out var rule))
            {
                _ruleStorage.Remove(rule);
                return true;
            }

            return false;
        }

        public static bool TryEditRule(double number, Action<RuleData, bool> edit)
        {
            if (TryGetRule(number, out var rule))
            {
                edit(rule, true);
                _ruleStorage.Save();
                return true;
            }
            else
            {
                edit(rule, false);
                return false;
            }
        }

        [Command("addrule", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("arule", "addr")]
        [Permission(PermissionLevel.Administrator)]
        [Description("Adds a rule.")]
        private static string AddRuleCommand(Player sender, string name, string text, double number, TimeSpan[] strikes)
        {
            if (TryAddRule(number, name, text, strikes))
            {
                return "Rule added succesfully!";
            }

            return "Failed to add that rule. Perhaps it already exists?";
        }

        [Command("deleterule", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("drule", "delr")]
        [Permission(PermissionLevel.Administrator)]
        [Description("Deletes a rule.")]
        private static string RemoveRuleCommand(Player sender, double number)
        {
            if (TryRemoveRule(number))
                return "Rule removed succesfully.";

            return "Failed to remove that rule. Perhaps it doesn't exist?";
        }

        [Command("listrules", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("lrules", "listr")]
        [Description("Lists all rules.")]
        private static string ListRulesCommmand(Player sender)
        {
            if (!Rules.Any())
                return "There aren't any rules to show.";

            var sb = new StringBuilder();

            sb.AppendLine($"Showing {Rules.Count} rule(s):");

            foreach (var rule in Rules)
            {
                sb.AppendLine(
                    $"$ {rule.Number} {rule.Name}\n" +
                    $"- {rule.Text}");

                sb.AppendLine($"Strikes:");

                for (int i = 0; i < rule.StrikeTimes.Length; i++)
                {
                    sb.AppendLine($"@ Strike {i + 1}: {rule.StrikeTimes[i].UserFriendlySpan()}"); 
                }
            }

            return sb.ToString();
        }

        [Command("viewrule", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [CommandAliases("vrule", "viewr")]
        [Description("Shows details of a certain rule.")]
        private static string ViewRuleCommand(Player sender, double ruleNumber)
        {
            if (TryGetRule(ruleNumber, out var rule))
            {
                var sb = new StringBuilder();

                sb.AppendLine(
                    $"$ Rule {rule.Number}: {rule.Name}\n" +
                    $" \"{rule.Text}\"");

                rule.StrikeTimes.For((i, span) =>
                {
                    sb.AppendLine($"@ Strike {i + 1}: {span.UserFriendlySpan()}");
                });
            }

            return "That rule does not exist.";
        }
    }
}
