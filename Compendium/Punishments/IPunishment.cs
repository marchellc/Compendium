using System;

namespace Compendium.Punishments
{
    public interface IPunishment
    {
        string Id { get; set; }

        string IssuerId { get; set; }
        string TargetId { get; set; }

        int[] Reason { get; set; }

        DateTime IssuedAt { get; set; }
        DateTime EndsAt { get; set; }
    }
}