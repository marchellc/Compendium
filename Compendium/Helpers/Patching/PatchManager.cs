using Compendium.Attributes;

using HarmonyLib;

using helpers;
using helpers.Random;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Helpers.Patching
{
    [LogSource("Patch Manager")]
    public static class PatchManager
    {
        private static readonly Dictionary<Type, object> _constructedInstances = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, PatchData[]> _installedPatches = new Dictionary<Type, PatchData[]>();

        public static readonly Type IPatchInterfaceType = typeof(IPatch);
        public static readonly Type PatchDataType = typeof(PatchData);
        public static readonly Type InstanceConstructorDelegateType = typeof(Func<object>);

        public static Harmony Harmony { get; private set; }

        [InitOnLoad(Priority = 255)]
        public static void PatchAll()
        {
            Plugin.OnUnloaded.Add(OnUnloadedHandler);

            Harmony = new Harmony(RandomGeneration.Default.GetReadableString(20));

            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (TryFindPatches(type, out var patches)) patches.ForEach(ApplyPatch);
                else Plugin.Debug($"No patches were found in {type.FullName}");
            }

            Plugin.Info($"Patching finished. Applied {_installedPatches.Count(x => x.Value.Count(y => y.Proxy != null) > 0)} types.");
        }

        public static void UnpatchAll()
        {

            foreach (var patchPair in _installedPatches)
            {
                if (patchPair.Value != null && patchPair.Value.Any())
                {
                    patchPair.Value.ForEach(UnapplyPatch);
                }
            }

            _installedPatches.Clear();
            _constructedInstances.Clear();

            Harmony = null;
        }

        public static void ApplyPatches(Type type) { if (TryFindPatches(type, out var patches)) patches.ForEach(ApplyPatch); }
        public static void ApplyPatches(params PatchData[] patches) => patches.ForEach(ApplyPatch);
        public static void ApplyPatch(PatchData patchData)
        {
            if (Harmony is null) return;

            patchData.FixName().FixType();

            if (!patchData.IsValid())
            {
                Plugin.Error($"Patch {patchData.Name} is invalid! Skipping ..");
                return;
            }

            Plugin.Debug($"Applying patch: {patchData.Name} ({patchData.Type.Value})");

            var sourceMethod = new HarmonyMethod(patchData.Replacement);

            MethodInfo proxy = null;

            switch (patchData.Type)
            {
                case PatchType.Prefix:
                    proxy = Harmony.Patch(patchData.Target, sourceMethod);
                    break;

                case PatchType.Postfix:
                    proxy = Harmony.Patch(patchData.Target, null, sourceMethod);
                    break;

                case PatchType.Transpiler:
                    proxy = Harmony.Patch(patchData.Target, null, null, sourceMethod);
                    break;

                case PatchType.Finalizer:
                    proxy = Harmony.Patch(patchData.Target, null, null, null, sourceMethod);
                    break;
            }

            if (proxy is null) Plugin.Error($"Failed to patch {patchData.Name}!");
            else Plugin.Info($"Succesfully patched {patchData.Name}!");

            patchData.Proxy = proxy;

            if (patchData.Proxy != null)
            {
                List<PatchData> installedList = null;

                if (!_installedPatches.TryGetValue(patchData.Replacement.DeclaringType, out var patches)) installedList = new List<PatchData>((_installedPatches[patchData.Replacement.DeclaringType] = Array.Empty<PatchData>()));
                else installedList = new List<PatchData>(patches);

                installedList.Add(patchData);
                _installedPatches[patchData.Replacement.DeclaringType] = installedList.ToArray();
            }
        }

        public static void UnapplyPatches(Type type) => _installedPatches[type].ForEach(UnapplyPatch);
        public static void UnapplyPatches(params PatchData[] patches) => patches.ForEach(UnapplyPatch);
        public static void UnapplyPatch(PatchData patchData)
        {
            if (Harmony is null) return;
            if (!patchData.IsPatched) return;
            if (!patchData.IsValid()) return;

            Harmony.Unpatch(patchData.Target, patchData.Proxy);
            patchData.Proxy = null;

            Log.Debug($"Unpatched {patchData.Name}");
        }

        public static bool TryFindPatches(Type type, out PatchData[] foundPatches)
        {
            var typeCollectedPatches = new List<PatchData>();

            if (type.IsAssignableFrom(IPatchInterfaceType))
            {
                if (TryGetOrCreateInstance(type, out var instance))
                {
                    if (instance is IPatch patchInterface)
                    {
                        foreach (var patchData in patchInterface.Patches)
                        {
                            patchData.FixName();
                            typeCollectedPatches.Add(patchData);
                            Plugin.Info($"Found patch: {patchData.Name}");
                        }
                    }
                }
            }

            foreach (var method in type.GetMethods())
            {
                if (method.TryGetAttribute<PatchAttribute>(out var patchAttribute))
                {
                    if (patchAttribute.Patch.HasValue)
                    {
                        var patchData = patchAttribute.Patch.Value.FixName();
                        if (patchData.Replacement is null) patchData.Replacement = method;
                        if (!patchData.IsValid())
                        {
                            Plugin.Warn($"Method {type.FullName}::{method.Name} has invalid patch data!");
                            continue;
                        }

                        typeCollectedPatches.Add(patchData);
                        Plugin.Info($"Found patch on method: {patchData.Name}");
                    }
                    else
                    {
                        Plugin.Warn($"Method {type.FullName}::{method.Name} is marked with the PatchAttribute, but does not have a valid PatchData instance!");
                        continue;
                    }
                }
            }

            Log.Debug($"Collected {typeCollectedPatches.Count} patches in {type.FullName}");

            foundPatches = typeCollectedPatches.ToArray();
            return !foundPatches.IsEmpty();
        }

        public static bool TryGetOrCreateInstance(Type type, out object instance)
        {
            if (_constructedInstances.TryGetValue(type, out instance)) return instance != null;
            if (TryGetInstanceCreator(type, out var constructor))
            {
                instance = constructor.Invoke();
                _constructedInstances[type] = instance;
                return instance != null;
            }

            return false;
        }

        public static bool TryGetInstanceCreator(Type type, out Func<object> instanceCreator)
        {
            var method = type.GetMethod("InstanceConstructor");
            if (method is null)
            {
                instanceCreator = null;
                return false;
            }

            try
            {
                instanceCreator = (Func<object>)Delegate.CreateDelegate(InstanceConstructorDelegateType, method);
                return instanceCreator != null;
            }
            catch
            {
                instanceCreator = null;
                return false;
            }
        }

        private static void OnUnloadedHandler() => UnpatchAll();
    }
}
