using Compendium.Comparison;

using helpers;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Attributes
{
    public static class AttributeRegistryEvents
    {
        public static event Action<Attribute, Type, MemberInfo, object> OnAttributeAdded;
        public static event Action<Attribute, Type, MemberInfo, object> OnAttributeRemoved;

        static AttributeRegistryEvents()
        {
            OnAttributeAdded += (attr, type, member, handle) => Plugin.Info($"Added attribute '{attr.GetType().FullName}': '{(member?.ToLogName() ?? type.FullName)}'!");
            OnAttributeRemoved += (attr, type, member, handle) => Plugin.Info($"Removed attribute '{attr.GetType().FullName}': '{(member?.ToLogName() ?? type.FullName)}'!");
        }

        internal static void FireAdded(Attribute attribute, Type type, MemberInfo member, object handle)
            => OnAttributeAdded?.Invoke(attribute, type, member, handle);

        internal static void FireRemoved(Attribute attribute, Type type, MemberInfo member, object handle)
            => OnAttributeRemoved?.Invoke(attribute, type, member, handle);
    }

    public static class AttributeRegistry<TAttribute> where TAttribute : Attribute
    {
        private static readonly List<AttributeData<TAttribute>> _list = new List<AttributeData<TAttribute>>();

        public static IReadOnlyList<AttributeData<TAttribute>> Attributes { get; private set; } = _list.AsReadOnly();

        public static Func<Type, MemberInfo, TAttribute, object[]> DataGenerator { get; set; }

        public static void ForEachOfCondition(Func<object[], AttributeData<TAttribute>, bool> predicate, Action<AttributeData<TAttribute>> action, params object[] data)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                var attr = _list[i];

                if (predicate(data, attr))
                    action(attr);
            }
        }

        public static void ForEach(Action<AttributeData<TAttribute>> action)
        {
            for (int i = 0; i < _list.Count; i++)
                action(_list[i]);
        }

        public static void Register()
            => Register(Assembly.GetCallingAssembly());

        public static void Register(Assembly assembly)
            => assembly.ForEachType(t => Register(t, null));

        public static void Register(Type type, object handle)
        {
            if (type.TryGetAttribute<TAttribute>(out var typeAttribute)
                && !TryGetAttribute(type, out _))
            {
                var attr = new AttributeData<TAttribute>(type, typeAttribute, GenerateData(type, null, typeAttribute));
                _list.Add(attr);
                AttributeRegistryEvents.FireAdded(typeAttribute, type, null, handle);
            }

            type.ForEachField(f => Register(f, handle));
            type.ForEachMethod(m => Register(m, handle));
            type.ForEachProperty(p => Register(p, handle));

            Attributes = _list.AsReadOnly();
        }

        public static void Register(MemberInfo member, object handle)
        {
            if (member.TryGetAttribute<TAttribute>(out var memberAttribute)
                && !TryGetAttribute(member, handle, out _))
            {
                var attr = new AttributeData<TAttribute>(member, member.DeclaringType, memberAttribute, handle, GenerateData(member.DeclaringType, member, memberAttribute));
                _list.Add(attr);
                AttributeRegistryEvents.FireAdded(memberAttribute, member.DeclaringType, member, handle);
            }
        }

        public static void Unregister()
            => Unregister(Assembly.GetCallingAssembly());

        public static void Unregister(Assembly assembly)
            => assembly.ForEachType(t => Unregister(t, null));

        public static void Unregister(Type type, object handle)
        {
            var toRemoveList = _list.Where(x => !x.IsMember && x.Type == type && NullableObjectComparison.Compare(x.MemberHandle, handle));

            if (toRemoveList.Count() <= 0)
                return;

            toRemoveList.ForEach(attr =>
            {
                if (_list.Remove(attr))
                    AttributeRegistryEvents.FireRemoved(attr.Attribute, attr.Type, attr.Member, attr.MemberHandle);
            });

            type.ForEachField(f => Unregister(f, handle));
            type.ForEachMethod(m => Unregister(m, handle));
            type.ForEachProperty(p => Unregister(p, handle));

            Attributes = _list.AsReadOnly();
        }

        public static void Unregister(MemberInfo member, object handle)
        {
            var toRemoveList = _list.Where(x => x.IsMember && x.Member == member && NullableObjectComparison.Compare(x.MemberHandle, handle));

            if (toRemoveList.Count() <= 0)
                return;

            toRemoveList.ForEach(attr =>
            {
                if (_list.Remove(attr))
                    AttributeRegistryEvents.FireRemoved(attr.Attribute, attr.Type, attr.Member, attr.MemberHandle);
            });

            Attributes = _list.AsReadOnly();
        }

        public static bool TryGetAttribute(MemberInfo member, object handle, out TAttribute attribute)
        {
            if (_list.TryGetFirst(a => a.IsMember && a.Member == member && NullableObjectComparison.Compare(handle, a.MemberHandle), out var data))
            {
                attribute = data.Attribute;
                return true;
            }

            attribute = default;
            return false;
        }

        public static bool TryGetAttribute(Type type, out TAttribute attribute)
        {
            if (_list.TryGetFirst(a => !a.IsMember && a.Type == type, out var data))
            {
                attribute = data.Attribute;
                return true;
            }

            attribute = default;
            return false;
        }

        private static object[] GenerateData(Type type, MemberInfo member, TAttribute attribute)
        {
            if (DataGenerator is null)
                return null;

            return DataGenerator(type, member, attribute);
        }
    }
}