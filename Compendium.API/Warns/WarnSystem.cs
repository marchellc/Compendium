using BetterCommands;

using Compendium.PlayerData;

using helpers.Attributes;
using helpers.Events;
using helpers.Extensions;
using helpers.IO.Storage;
using helpers.Pooling.Pools;
using helpers.Time;
using helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Compendium.Generation;
using Compendium.Constants;

namespace Compendium.Warns
{
    public static class WarnSystem
    {
        private static SingleFileStorage<WarnData> _warnStorage;

        public static IReadOnlyCollection<WarnData> Warns => _warnStorage.Data;

        public static EventProvider OnWarnIssued { get; } = new EventProvider();
        public static EventProvider OnWarnRemoved { get; } = new EventProvider();

        [Load]
        [Reload]
        public static void Load()
        {
            if (_warnStorage != null)
            {
                _warnStorage.Reload();
                return;
            }

            _warnStorage = new SingleFileStorage<WarnData>($"{Directories.ThisData}/SavedWarns");
            _warnStorage.Load();

            Plugin.Info($"Warn System loaded.");
        }

        [Unload]
        public static void Unload()
        {
            if (_warnStorage != null)
                _warnStorage.Save();

            _warnStorage = null;

            Plugin.Info($"Warn System unloaded.");
        }

        public static WarnData[] ListIssuedWarns(PlayerDataRecord target, string filter = null)
        {
            var query = Query(filter);

            if (!query.Any())
                return null;

            return query.Where(q => q.Issuer == target.Id).ToArray();
        }

        public static WarnData[] ListReceivedWarns(PlayerDataRecord target, string filter = null)
        {
            var query = Query(filter);

            if (!query.Any())
                return null;

            return query.Where(q => q.Target == target.Id).ToArray();
        }

        public static WarnData[] Query(string filter = null)
        {
            if (filter is null || filter is "*")
                return Warns.ToArray();

            return Warns.Where(w => 
                       w.Reason.ToLower().Contains(filter.ToLower()) 
                    || w.Reason.Split(' ').Any(x => x.ToLowerInvariant().GetSimilarity(filter.ToLowerInvariant()) >= 0.8)).ToArray();
        }

        public static bool Remove(string id)
        {
            var toRemove = ListPool<WarnData>.Pool.Get();

            foreach (var w in Warns)
            {
                if (w.Id == id)
                    toRemove.Add(w);
            }

            if (!toRemove.Any())
            {
                ListPool<WarnData>.Pool.Push(toRemove);
                return false;
            }

            toRemove.ForEach(w => _warnStorage.Remove(w));

            ListPool<WarnData>.Pool.Push(toRemove);
            return true;
        }

        public static WarnData Issue(PlayerDataRecord issuer, PlayerDataRecord target, string reason)
        {
            var warn = new WarnData
            {
                Id = UniqueIdGeneration.Generate(7),

                IssuedAt = TimeUtils.LocalTime,

                Issuer = issuer is null ? "Server" : issuer.Id,
                Target = target.Id,

                Reason = reason
            };

            _warnStorage.Add(warn);
            OnWarnIssued.Invoke(warn, issuer, target);

            if (Plugin.Config.WarnSettings.Announce)
            {
                issuer.TryInvokeHub(issuerHub =>
                {
                    issuerHub.Hint(Colors.LightGreen(
                        $"<b>Hráči <color={Colors.RedValue}>{target.NameTracking.LastValue}</color> bylo uděleno varování</b>\n" +
                        $"<color={Colors.GreenValue}>{reason}</color>"), 10f);
                });

                target.TryInvokeHub(targetHub =>
                {
                    targetHub.Broadcast(Colors.LightGreen(
                        $"<b>Obdržel jsi varování!</b>\n" +
                        $"<b><color={Colors.RedValue}>{reason}</color>"), 10);
                });
            }

            return warn;
        }

        [Command("warns", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Retrieves a list of warns for a specific player.")]
        private static string ListWarnsCommand(ReferenceHub sender, PlayerDataRecord target, string filter = "*")
        {
            var warns = ListReceivedWarns(target, filter);

            if (warns is null || !warns.Any())
                return "There aren't any warns matching your search.";

            warns = warns.OrderBy(w => TimeUtils.LocalTime - w.IssuedAt).ToArray();

            var sb = new StringBuilder();

            sb.AppendLine($"Found {warns.Length} warn(s):");

            warns.For((i, w) =>
            {
                string issuer = "Unknown Issuer";

                if (w.Issuer is "Server")
                    issuer = "Server";
                else if (PlayerDataRecorder.TryQuery(w.Issuer, false, out var record) && record.NameTracking.LastValue != null)
                    issuer = record.NameTracking.LastValue;

                sb.AppendLine($"[{i + 1}] {w.Id}: {w.Reason} [{issuer}] ({w.IssuedAt.ToString("F")})");
            });

            return sb.ToString();
        }

        [Command("warn", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Issues a warn.")]
        public static string IssueWarnCommand(ReferenceHub sender, PlayerDataRecord target, string reason)
        {
            var warn = Issue(PlayerDataRecorder.GetData(sender), target, reason);

            if (warn is null)
                return "Failed to issue that warn.";

            return $"Issued warn with ID {warn.Id} and reason {warn.Reason} to {target.NameTracking.LastValue}";
        }

        [Command("delwarn", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Deletes a warn.")]
        public static string RemoveWarnCommand(ReferenceHub sender, string id)
        {
            if (!Remove(id))
                return $"Failed to find a warn with ID {id}";

            return $"Removed warn with ID {id}";
        }
    }
}