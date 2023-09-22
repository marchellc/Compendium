using Mirror;

using PlayerRoles;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

using helpers.Attributes;
using helpers;

namespace Compendium
{
    public static class Mirror
    {
        private static volatile Dictionary<Type, MethodInfo> _writers = new Dictionary<Type, MethodInfo>();

        private static volatile Dictionary<string, ulong> _syncVars = new Dictionary<string, ulong>();
        private static volatile Dictionary<string, string> _rpcMatrix = new Dictionary<string, string>();

        [Load]
        private static void Load()
        {
            try
            {
                var assembly = typeof(RoleTypeId).Assembly;
                var generatedClass = assembly.GetType("Mirror.GeneratedNetworkCode");

                new Thread(() =>
                {
                    foreach (var method in typeof(NetworkWriterExtensions).GetMethods().Where(x => !x.IsGenericMethod && (x.GetParameters()?.Length == 2)))
                    {
                        var paramType = method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType;
                        _writers[paramType] = method;
                    }

                    foreach (var method in generatedClass.GetMethods().Where(x => !x.IsGenericMethod && (x.GetParameters()?.Length == 2) && (x.ReturnType == typeof(void))))
                    {
                        var paramType = method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType;
                        _writers[paramType] = method;
                    }

                    foreach (var serializerClass in assembly.GetTypes().Where(x => x.Name.EndsWith("Serializer")))
                    {
                        foreach (var method in serializerClass.GetMethods().Where(x => (x.ReturnType == typeof(void)) && x.Name.StartsWith("Write")))
                        {
                            var paramType = method.GetParameters().First(x => x.ParameterType != typeof(NetworkWriter)).ParameterType;
                            _writers[paramType] = method;
                        }
                    }

                    foreach (var property in assembly.GetTypes().SelectMany(x => x.GetProperties()).Where(m => m.Name.StartsWith("Network")))
                    {
                        var setMethod = property.GetSetMethod();

                        if (setMethod is null)
                            continue;

                        var methodBody = setMethod.GetMethodBody();

                        if (methodBody is null)
                            continue;

                        var bytecodes = methodBody.GetILAsByteArray();

                        if (!_syncVars.ContainsKey($"{property.ReflectedType.Name}.{property.Name}"))
                            _syncVars.Add($"{property.ReflectedType.Name}.{property.Name}", bytecodes[bytecodes.LastIndexOf((byte)OpCodes.Ldc_I8.Value) + 1]);
                    }

                    foreach (var method in assembly.GetTypes()
                        .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        .Where(m => m.GetCustomAttributes(typeof(ClientRpcAttribute), false).Length > 0 || m.GetCustomAttributes(typeof(TargetRpcAttribute), false).Length > 0))
                    {
                        var methodBody = method.GetMethodBody();

                        if (methodBody is null)
                            continue;

                        var bytecodes = methodBody.GetILAsByteArray();

                        if (!_rpcMatrix.ContainsKey($"{method.ReflectedType.Name}.{method.Name}"))
                            _rpcMatrix.Add($"{method.ReflectedType.Name}.{method.Name}", method.Module.ResolveString(BitConverter.ToInt32(bytecodes, bytecodes.IndexOf((byte)OpCodes.Ldstr.Value) + 1)));
                    }

                    Plugin.Info($"Mirror Networking loaded! writers={_writers.Count} syncVars={_syncVars.Count} rpc={_rpcMatrix.Count}");
                }).Start();
            }
            catch (Exception ex)
            {
                Plugin.Error("Failed to load Mirror!");
                Plugin.Error(ex);
            }
        }

        public static void SendFakeSyncVar(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string propertyName, object value)
        {
            if (behaviorOwner is null)
                behaviorOwner = ReferenceHub.HostHub.networkIdentity;

            var writer = NetworkWriterPool.Get();
            var writer2 = NetworkWriterPool.Get();

            MakeCustomSyncWriter(behaviorOwner, targetType, null, CustomSyncVarGenerator, writer, writer2);

            target.connectionToClient.Send(new EntityStateMessage
            {
                netId = behaviorOwner.netId,
                payload = writer.ToArraySegment(),
            });

            NetworkWriterPool.Return(writer);
            NetworkWriterPool.Return(writer2);

            void CustomSyncVarGenerator(NetworkWriter targetWriter)
            {
                targetWriter.WriteULong(_syncVars[$"{targetType.Name}.{propertyName}"]);
                _writers[value.GetType()]?.Invoke(null, new object[2] { targetWriter, value });
            }
        }


        public static void ResyncSyncVar(this NetworkIdentity behaviorOwner, Type targetType, string propertyName)
        {
            if (behaviorOwner is null)
                behaviorOwner = ReferenceHub.HostHub.networkIdentity;

            var behaviourComponent = behaviorOwner.gameObject.GetComponent(targetType);

            if (!(behaviourComponent is NetworkBehaviour networkBehaviour))
            {
                Plugin.Warn($"Attempted to re-synchronize variables of a behaviour not derived from NetworkBehaviour: '{targetType.FullName}'");
                return;
            }

            networkBehaviour.SetSyncVarDirtyBit(_syncVars[$"{targetType.Name}.{propertyName}"]);
        }

        public static void SendFakeTargetRpc(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, string rpcName, params object[] values)
        {
            if (behaviorOwner is null)
                behaviorOwner = ReferenceHub.HostHub.networkIdentity;

            var writer = NetworkWriterPool.Get();

            foreach (object value in values)
                _writers[value.GetType()].Invoke(null, new[] { writer, value });

            var msg = new RpcMessage()
            {
                netId = behaviorOwner.netId,
                componentIndex = (byte)GetComponentIndex(behaviorOwner, targetType),
                functionHash = (ushort)_rpcMatrix[$"{targetType.Name}.{rpcName}"].GetStableHashCode(),
                payload = writer.ToArraySegment(),
            };

            if (target.connectionToClient != null)
                target.connectionToClient.BufferRpc(msg, 0);
            else
                Plugin.Warn($"Failed to send fake RPC to {target.GetLogName(true)}: target's client connection is null!");

            NetworkWriterPool.Return(writer);
        }

        public static void SendFakeSyncObject(this ReferenceHub target, NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customAction)
        {
            if (behaviorOwner is null)
                behaviorOwner = ReferenceHub.HostHub.networkIdentity;

            var writer = NetworkWriterPool.Get();
            var writer2 = NetworkWriterPool.Get();

            MakeCustomSyncWriter(behaviorOwner, targetType, customAction, null, writer, writer2);

            target.networkIdentity.connectionToClient.Send(new EntityStateMessage() 
            { 
                netId = behaviorOwner.netId, 
                payload = writer.ToArraySegment() 
            });
            
            NetworkWriterPool.Return(writer);
            NetworkWriterPool.Return(writer2);
        }

        public static void EditNetworkObject(this NetworkIdentity identity, Action<NetworkIdentity> customAction)
        {
            customAction.Invoke(identity);

            var objectDestroyMessage = new ObjectDestroyMessage()
            {
                netId = identity.netId,
            };

            Hub.Hubs.ForEach(hub =>
            {
                hub.connectionToClient.Send(objectDestroyMessage);
                NetworkServer.SendSpawnMessage(identity, hub.connectionToClient);
            });
        }

        public static int GetComponentIndex(this NetworkIdentity identity, Type type)
            => Array.FindIndex(identity.NetworkBehaviours, (x) => x.GetType() == type);

        public static void MakeCustomSyncWriter(this NetworkIdentity behaviorOwner, Type targetType, Action<NetworkWriter> customSyncObject, Action<NetworkWriter> customSyncVar, NetworkWriter owner, NetworkWriter observer)
        {
            if (behaviorOwner is null)
                behaviorOwner = ReferenceHub.HostHub.networkIdentity;

            var value = ulong.MinValue;
            NetworkBehaviour behaviour = null;

            for (int i = 0; i < behaviorOwner.NetworkBehaviours.Length; i++)
            {
                if (behaviorOwner.NetworkBehaviours[i].GetType() == targetType)
                {
                    behaviour = behaviorOwner.NetworkBehaviours[i];
                    value = 1UL << (i & 31);
                    break;
                }
            }

            Compression.CompressVarUInt(owner, value);

            var position = owner.Position;
            owner.WriteByte(0);
            var position2 = owner.Position;

            if (customSyncObject != null)
                customSyncObject(owner);
            else
                behaviour.SerializeObjectsDelta(owner);

            customSyncVar?.Invoke(owner);

            var position3 = owner.Position;

            owner.Position = position;
            owner.WriteByte((byte)(position3 - position2 & 255));
            owner.Position = position3;

            if (behaviour.syncMode != SyncMode.Observers)
                observer.WriteBytes(owner.ToArraySegment().Array, position, owner.Position - position);
        }
    }
}