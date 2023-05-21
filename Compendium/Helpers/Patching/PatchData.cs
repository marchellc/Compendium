using helpers;
using helpers.Extensions;

using System;
using System.Linq;
using System.Reflection;

namespace Compendium.Helpers.Patching
{
    [LogSource("Patch Data")]
    public struct PatchData
    {
        public MethodInfo Replacement;
        public MethodInfo Target;
        public MethodInfo Proxy;
        public PatchType? Type;
        public string Name;

        public bool IsPatched => Proxy != null;

        public PatchData WithReplacement(Type type, string methodName, params Type[] paramFilter)
        {
            Replacement = paramFilter != null && paramFilter.Length > 0 ? type.GetMethod(methodName, paramFilter) : type.GetMethod(methodName);
            return this;
        }

        public PatchData WithTarget(Type type, string methodName, params Type[] paramFilter)
        {
            Target = paramFilter != null && paramFilter.Length > 0 ? type.GetMethod(methodName, paramFilter) : type.GetMethod(methodName);
            return this;
        }

        public PatchData WithTarget(Delegate target)
        {
            Target = target.Method;
            return this;
        }

        public PatchData WithReplacement(Delegate replacement)
        {
            Replacement = replacement.Method;
            return this;
        }

        public PatchData WithReplacement(MethodInfo replacement)
        {
            Replacement = replacement;
            return this;
        }

        public PatchData WithGenericTarget(Type type, string methodName, params Type[] paramFilter)
        {
            Target = paramFilter != null && paramFilter.Length > 0 ? type.GetMethods().First(x => x.Name == methodName && x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).Match(paramFilter)) : type.GetMethods().First(x => x.Name == methodName && x.IsGenericMethod);
            return this;
        }

        public PatchData WithGenericTarget(Type type, string methodName, GenericParameterAttributes genericParameterAttributes, params Type[] paramFilter)
        {
            foreach (var method in type.GetMethods())
            {
                if (method.Name != methodName) continue;

                var genericMethod = method.GetGenericMethodDefinition();
                if (!genericMethod.ContainsGenericParameters) continue;

                var genericArgs = genericMethod.GetGenericArguments();
                var args = genericMethod.GetParameters();

                if (paramFilter != null && paramFilter.Length > 0 && !args.Select(x => x.ParameterType).Match(paramFilter)) continue;
                if (!Reflection.IsConstraint(genericMethod, genericParameterAttributes)) continue;

                Target = genericMethod;
                break;
            }

            return this;
        }

        public PatchData WithTarget(MethodInfo method)
        {
            Target = method;
            return this;
        }

        public PatchData WithName(string name)
        {
            Name = name;
            return this;
        }

        public PatchData WithType(PatchType type)
        {
            Type = type;
            return this;
        }

        public bool IsValid() => Replacement != null && Target != null && Type.HasValue;

        public PatchData FixType()
        {
            if (Type.HasValue) return this;

            switch (Replacement.Name)
            {
                case "Prefix":
                    Type = PatchType.Prefix;
                    break;

                case "Postfix":
                    Type = PatchType.Postfix;
                    break;

                case "Finalizer":
                    Type = PatchType.Finalizer;
                    break;

                case "Transpiler":
                    Type = PatchType.Transpiler;
                    break;

                default:
                    Plugin.Warn($"Failed to recognize patch type automatically!");
                    break;
            }

            return this;
        }

        public PatchData FixName() { FixType(); if (string.IsNullOrEmpty(Name)) Name = $"{(Type.HasValue ? Type.Value.ToString() : "Unknown Type")} Patch {Target.DeclaringType.FullName}::{Target.Name} by {Replacement.DeclaringType.FullName}::{Replacement.Name}"; return this; }
        public static PatchData New() => new PatchData();
    }
}