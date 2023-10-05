using System;
using System.Reflection;

namespace Compendium.Attributes
{
    public class AttributeData<TAttribute> where TAttribute : Attribute
    {
        public MemberInfo Member { get; }
        public Type Type { get; }

        public TAttribute Attribute { get; }

        public object MemberHandle { get; }
        public object[] Data { get; }

        public bool IsMember => Member != null;

        public AttributeData(MemberInfo member, Type type, TAttribute attribute, object memberHandle, object[] data)
        {
            Member = member;
            Type = type;
            Attribute = attribute;
            MemberHandle = memberHandle;
            Data = data;
        }

        public AttributeData(Type type, TAttribute attribute, object[] data)
        {
            Type = type;
            Attribute = attribute;
            Data = data;
        }
    }
}