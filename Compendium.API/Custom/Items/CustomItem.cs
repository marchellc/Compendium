using System.Collections.Generic;

namespace Compendium.Custom.Items
{
    public class CustomItem<THandler> : CustomItemBase where THandler : CustomItemHandlerBase, new()
    {
        private readonly HashSet<THandler> _handlers = new HashSet<THandler>();

        public new IReadOnlyCollection<THandler> Handlers => _handlers;

        internal override void OnHandlerCreated(CustomItemHandlerBase customItem)
        {
            if (customItem is THandler handler)
                _handlers.Add(handler);

            base.OnHandlerCreated(customItem);
        }

        internal override void OnHandlerDestroyed(CustomItemHandlerBase customItem)
        {
            if (customItem is THandler handler)
                _handlers.Remove(handler);

            base.OnHandlerDestroyed(customItem);
        }

        internal override void ClearHandlers()
        {
            base.ClearHandlers();
            _handlers.Clear();
        }

        internal override CustomItemHandlerBase CreateHandler()
        {
            var handler = new THandler();
            SetupHandler(handler);
            return handler;
        }
    }
}