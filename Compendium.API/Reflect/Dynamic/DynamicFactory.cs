using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;

using helpers.Extensions;

namespace Compendium.Reflect.Dynamic
{
    public delegate object DynamicMethodDelegate(object target, object[] args);

    public class DynamicMethodDelegateFactory
    {
        public static readonly Dictionary<MethodInfo, MethodInfo> MethodCache = new Dictionary<MethodInfo, MethodInfo>();
        public static readonly Dictionary<MethodInfo, DynamicMethodDelegate> DelegateCache = new Dictionary<MethodInfo, DynamicMethodDelegate>();

        public static string GetMethodName(MethodInfo method)
            => MethodCache.TryGetValue(method, out var actual) ? actual.ToLogName() : method.ToLogName();

        public static DynamicMethodDelegate Create(MethodInfo method)
        {
            if (DelegateCache.TryGetValue(method, out var del))
                return del;

            var parameters = method.GetParameters();
            var paramCount = parameters.Length;
            var argTypes = new Type[] { typeof(object), typeof(object[]) };
            var dynamicMethod = new DynamicMethod(method.ToLogName(), typeof(object), argTypes, typeof(DynamicMethodDelegateFactory));
            var il = dynamicMethod.GetILGenerator();

            var okLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Ldc_I4, paramCount);
            il.Emit(OpCodes.Beq, okLabel);
            il.Emit(OpCodes.Newobj, typeof(TargetParameterCountException).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Throw);
            il.MarkLabel(okLabel);

            if (!method.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            var i = 0;

            while (i < paramCount)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                var paramType = parameters[i].ParameterType;

                if (paramType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, paramType);

                i++;
            }

            if (method.IsFinal)
                il.Emit(OpCodes.Call, method);
            else
                il.Emit(OpCodes.Callvirt, method);

            if (method.ReturnType != typeof(void))
            {
                if (method.ReturnType.IsValueType)
                    il.Emit(OpCodes.Box, method.ReturnType);
            }
            else
                il.Emit(OpCodes.Ldnull);

            il.Emit(OpCodes.Ret);

            del = (DynamicMethodDelegate)dynamicMethod.CreateDelegate(typeof(DynamicMethodDelegate));
            MethodCache[del.Method] = method;
            return del;
        }

    }
}