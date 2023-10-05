using Compendium.Charts;
using Compendium.HttpServer;
using Compendium.PlayerData;
using Compendium.Staff;

using Grapevine;

using helpers;
using helpers.Time;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace Compendium.HttpApi
{
    [RestResource]
    public class StaffApi
    {
        [RestRoute("Get", "/api/staff/activity")]
        public async Task StaffActivityAsync(IHttpContext context)
        {
            if (!context.TryAccess())
                return;

            var sb = Pools.PoolStringBuilder();
            var list = StaffActivity._storage.Data.OrderByDescending(x => x.TwoWeeks);

            list.For((_, data) => sb.AppendLine($">- {(PlayerDataRecorder.TryQuery(data.UserId, false, out var record) ? $"{record.NameTracking.LastValue} ({record.UserId})" : data.UserId)}      |    {TimeSpan.FromSeconds(data.TwoWeeks).UserFriendlySpan()}"));

            await context.Response.SendResponseAsync(sb.ReturnStringBuilderValue());
        }

        [RestRoute("Get", "/api/staff/activity_chart")]
        public async Task StaffActivityChartAsync(IHttpContext context)
        {
            if (!context.TryAccess())
                return;

            var set = new List<KeyValuePair<string, int>>();
            var list = StaffActivity._storage.Data.OrderByDescending(x => x.TwoWeeks);

            list.For((_, data) => set.Add(new KeyValuePair<string, int>($"{(PlayerDataRecorder.TryQuery(data.UserId, false, out var record) ? $"{record.NameTracking.LastValue} ({record.UserId})" : data.UserId)}", Mathf.RoundToInt((float)TimeSpan.FromSeconds(data.TwoWeeks).TotalHours))));

            var bytes = ChartBuilder.GetChart("Aktivita", set);

            await context.Response.SendResponseAsync(bytes);
        }
    }
}