using System;

namespace Compendium.State
{
    [Flags]
    public enum StateFlags
    {
        RemoveOnDeath,
        RemoveOnRoleChange,
        RemoveOnDamage,
        DisableUpdate,
    }
}