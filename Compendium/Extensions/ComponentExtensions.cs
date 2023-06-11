using UnityEngine;

namespace Compendium.Extensions
{
    public static class ComponentExtensions
    {
        public static bool TryGetComponentAllLayers<TComponent>(this GameObject component, out TComponent result) where TComponent : Component
        {
            if (component.TryGetComponent(out result))
                return true;

            result = component.GetComponentInParent<TComponent>();

            if (result != null)
                return true;

            result = component.GetComponentInChildren<TComponent>();
            return result != null;
        }
    }
}