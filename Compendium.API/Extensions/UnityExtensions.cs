using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Compendium.Extensions
{
    public static class UnityExtensions
    {
        public static float DistanceSquared(this Vector3 a, Vector3 b) => (a - b).sqrMagnitude;
        public static float DistanceSquared(this GameObject a, GameObject b) => (a.transform.position - b.transform.position).sqrMagnitude;
        public static float DistanceSquared(this GameObject a, Vector3 b) => (a.transform.position - b).sqrMagnitude;
        public static float DistanceSquared(this Vector3 a, GameObject b) => (a - b.transform.position).sqrMagnitude;
        public static float DistanceSquared(this Component a, Component b) => (a.transform.position - b.transform.position).sqrMagnitude;
        public static float DistanceSquared(this Component a, Vector3 b) => (a.transform.position - b).sqrMagnitude;
        public static float DistanceSquared(this Vector3 a, Component b) => (a - b.transform.position).sqrMagnitude;
        public static float DistanceSquared(this Transform a, Transform b) => (a.position - b.position).sqrMagnitude;
        public static float DistanceSquared(this Transform a, Vector3 b) => (a.position - b).sqrMagnitude;

        public static float DistanceSquared(this Vector3 a, Transform b) => (a - b.transform.position).sqrMagnitude;

        public static bool IsWithinDistance(this Vector3 a, Vector3 b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this GameObject a, GameObject b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this GameObject a, Vector3 b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Vector3 a, GameObject b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Component a, Component b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Component a, Vector3 b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Vector3 a, Component b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Transform a, Transform b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Transform a, Vector3 b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;
        public static bool IsWithinDistance(this Vector3 a, Transform b, float maxDistance) => DistanceSquared(a, b) <= maxDistance * maxDistance;

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component =>
            gameObject != null ? gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>() : throw new ArgumentNullException(nameof(gameObject));

        public static T GetOrAddComponent<T>(this Component component) where T : Component => GetOrAddComponent<T>(component.gameObject);

        public static bool TryGet<TComponent>(this GameObject component, out TComponent result) where TComponent : Component
        {
            if (component.TryGetComponent(out result))
                return true;

            result = component.GetComponentInParent<TComponent>();

            if (result != null)
                return true;

            result = component.GetComponentInChildren<TComponent>();
            return result != null;
        }

        public static bool DestroyComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject is null || !gameObject.TryGet(out T component))
                return false;

            Object.Destroy(component);

            return true;
        }

        public static bool DestroyComponent<T>(this Component component) where T : Component
        {
            if (component is null || !component.gameObject.TryGet(out T c))
                return false;

            Object.Destroy(c);

            return true;
        }

        public static bool DestroyImmediate<T>(this GameObject gameObject) where T : Component
        {
            if (!gameObject.TryGet(out T component))
                return false;

            Object.DestroyImmediate(component);

            return true;
        }

        public static bool DestroyImmediate<T>(this Component component) where T : Component
        {
            if (!component.gameObject.TryGet(out T c))
                return false;

            Object.DestroyImmediate(c);

            return true;
        }
    }
}