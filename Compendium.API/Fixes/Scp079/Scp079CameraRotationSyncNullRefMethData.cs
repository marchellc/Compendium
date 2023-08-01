using helpers;

using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Subroutines;

using System;
using System.Reflection;

namespace Compendium.Fixes
{
    public struct Scp079CameraRotationSyncNullRefMethData
    {
        public MethodInfo _processSendRpcMeth;
        public IntPtr _processSendRpcPtr;
        public Action<bool> _processSendRpcDel;
        public uint _netId;

        public Scp079CameraRotationSyncNullRefMethData(Scp079CameraRotationSync scp079CameraRotationSync)
        {
            _netId = scp079CameraRotationSync._owner.netId;
            _processSendRpcMeth = typeof(ScpSubroutineBase).GetMethod("ServerSendRpc", Reflection.AllFlags, null, new Type[] { typeof(bool) }, null);
            _processSendRpcPtr = _processSendRpcMeth.MethodHandle.GetFunctionPointer();
            _processSendRpcDel = (Action<bool>)Activator.CreateInstance(typeof(Action<bool>), scp079CameraRotationSync, _processSendRpcPtr);
        }
    }
}