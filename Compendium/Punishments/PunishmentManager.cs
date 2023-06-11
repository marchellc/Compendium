using Compendium.Attributes;

using helpers;
using helpers.Extensions;

using System.Collections.Generic;

namespace Compendium.Punishments
{
    public static class PunishmentManager
    {
        private static readonly HashSet<IPunishmentHandler> m_Handlers = new HashSet<IPunishmentHandler>();

        public static IReadOnlyCollection<IPunishmentHandler> Handlers => m_Handlers;

        [InitOnLoad]
        public static void Initialize()
        {

        }

        public static THandler AddHandler<THandler>() where THandler : class, IPunishmentHandler
        {
            var handler = GetHandler<THandler>();
            if (handler is null)
            {
                handler = Reflection.Instantiate<THandler>();
                m_Handlers.Add(handler);
                return handler;
            }
            else
            {
                return handler;
            }
        }

        public static THandler GetHandler<THandler>() where THandler : class, IPunishmentHandler
        {
            if (m_Handlers.TryGetFirst<THandler>(out var handler))
                return handler;

            return default;
        }
    }
}