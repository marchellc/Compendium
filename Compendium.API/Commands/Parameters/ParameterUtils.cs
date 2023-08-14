using Compendium.Commands.Attributes;

using helpers;
using helpers.Enums;
using helpers.Extensions;

using Interactables.Interobjects.DoorUtils;

using MapGeneration;

using Mirror;

using PluginAPI.Core.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Compendium.Commands.Parameters
{
    public static class ParameterUtils
    {
        private static readonly Dictionary<ParameterType, IParameterParser> _parsers = new Dictionary<ParameterType, IParameterParser>();

        private static readonly Dictionary<Type, Type> _proxy = new Dictionary<Type, Type>();
        private static readonly HashSet<Type> _clearTypes = new HashSet<Type>();

        public static bool BypassParser(this ParameterFlags flags)
            => flags.HasFlagFast(ParameterFlags.SenderHub) || flags.HasFlagFast(ParameterFlags.Sender) || flags.HasFlagFast(ParameterFlags.Context);

        public static bool BypassParser(this Parameter parameter)
            => parameter.Flags.BypassParser();

        public static bool IsRemainder(this Parameter parameter)
            => parameter.Flags.HasFlagFast(ParameterFlags.Remainder);

        public static bool IsMultiple(this Parameter parameter)
            => parameter.Flags.HasFlagFast(ParameterFlags.Multiple);

        public static bool IsOptional(this Parameter parameter)
            => parameter.Flags.HasFlagFast(ParameterFlags.Optional);

        public static bool TryRegisterParser<TParser>(ParameterType type) where TParser : IParameterParser, new()
        {
            if (TryGetParser(type, out _))
            {
                Plugin.Warn($"Tried registering an already existing parser (type: {type})!");
                return false;
            }

            return TryRegisterParser(type, new TParser());
        }

        public static bool TryRegisterParser(ParameterType type, IParameterParser parser)
        {
            if (parser is null)
            {
                Plugin.Warn($"Tried registering a null parameter parser!");
                return false;
            }

            if (!parser.TryValidate(type))
            {
                Plugin.Warn($"Tried registering mismatched parser!");
                return false;
            }

            if (TryGetParser(type, out _))
            {
                Plugin.Warn($"Tried registering an already existing parser (type: {type})!");
                return false;
            }

            _parsers[type] = parser;

            Plugin.Debug($"Registered parameter parser for type: {type} ({parser})");

            return true;
        }

        public static bool TryUnregisterParser<TParser>() where TParser : IParameterParser, new()
        {
            if (_parsers.TryGetFirst(p => p.Value != null && p.Value is TParser, out var parserPair))
                return TryUnregisterParser(parserPair.Key);

            Plugin.Warn($"Failed to find a registered parser of type {typeof(TParser).FullName}!");
            return false;
        }

        public static bool TryUnregisterParser(ParameterType type)
        {
            if (!TryGetParser(type, out _))
            {
                Plugin.Warn($"Tried unregistering parser for unregistered type {type}!");
                return false;
            }

            if (_parsers.Remove(type))
            {
                Plugin.Debug($"Unregistered parameter parser for type: {type}");
                return true;
            }

            Plugin.Warn($"Failed to remove parameter parser for type: {type} - unknown!");
            return false;
        }

        public static bool TryConvertParameters(ParameterInfo[] parameters, out Parameter[] convertedParameters)
        {
            Plugin.Debug($"Processing {parameters.Length} parameters ..");

            convertedParameters = new Parameter[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (!TryGetFlags(param, i, out var flags))
                {
                    Plugin.Warn($"Failed to process parameter flags of parameter name={param.Name} index={i} type={param.ParameterType.FullName}");
                    return false;
                }

                if (!TryGetParameterType(param.ParameterType, out var parameterType))
                {
                    Plugin.Warn($"Failed to process parameter type of parameter name={param.Name} index={i} type={param.ParameterType.FullName}");
                    return false;
                }

                if (!TryGetParser(parameterType, out var parser) && !flags.BypassParser())
                {
                    Plugin.Warn($"Failed to process parameter parser of parameter name={param.Name} index={i} type={param.ParameterType.FullName}");
                    return false;
                }

                TryProcessRestrictions(param, out var restrictions);

                convertedParameters[i] = new Parameter(param.Name, i, param.DefaultValue, param.ParameterType, parameterType, flags, restrictions, parser);

                Plugin.Debug($"Succesfully processed parameter name={param.Name} index={i} type={param.ParameterType.FullName} apiType={parameterType} parser={parser} flags=\"{flags}\" restrictions=\"{(restrictions.Any() ? string.Join(", ", restrictions.Select(r => r.GetType().FullName)) : "none")}\"");
            }

            Plugin.Debug($"Succesfully processed {convertedParameters.Length} / {parameters.Length} parameters.");
            return true;
        }

        internal static bool TryFixType(Type type, out Type fixedType)
        {
            if (_clearTypes.Contains(type))
            {
                fixedType = type;
                return true;
            }

            if (_proxy.TryGetValue(type, out fixedType))
                return true;

            if (Reflection.HasInterface<IPlayer>(type))
            {
                fixedType = (_proxy[type] = typeof(IPlayer));
                return true;
            }

            if (Reflection.HasInterface<DoorVariant>(type))
            {
                fixedType = (_proxy[type] = typeof(DoorVariant));
                return true;
            }

            if (type.IsArray)
            {
                fixedType = (_proxy[type] = typeof(Array));
                return true;
            }

            if (type.IsEnum)
            {
                fixedType = (_proxy[type] = typeof(Enum));
                return true;
            }

            if (Reflection.HasInterface<IParameterData>(type))
            {
                fixedType = (_proxy[type] = typeof(IParameterData));
                return true;
            }

            if (Reflection.HasInterface<ICommandContext>(type))
            {
                fixedType = (_proxy[type] = typeof(ICommandContext));
                return true;
            }

            if (Reflection.HasInterface<IDictionary>(type))
            {
                fixedType = (_proxy[type] = typeof(IDictionary));
                return true;
            }

            if (Reflection.HasInterface<IEnumerable>(type) && type != typeof(string))
            {
                fixedType = (_proxy[type] = typeof(IEnumerable));
                return true;
            }

            _clearTypes.Add(type);
            return true;
        }

        internal static bool TryGetParser(ParameterType type, out IParameterParser parser)
        {
            if (!_parsers.TryGetValue(type, out parser))
            {
                Plugin.Warn($"Failed to retrieve a parameter parser for type: {type}");
                return false;
            }

            return parser != null;
        }

        internal static bool TryProcessRestrictions(ParameterInfo info, out IParameterRestriction[] restrictions)
        {
            var attributes = info.GetCustomAttributes();
            var list = new List<IParameterRestriction>();

            attributes.ForEach(attr =>
            {
                if (attr is RestrictionAttribute restrictionAttribute)
                {
                    if (restrictionAttribute.Restriction is null)
                    {
                        Plugin.Warn($"Parameter restriction on parameter {info.Name} (type: {info.ParameterType.FullName}) is invalid (null)!");
                        return;
                    }

                    if (!restrictionAttribute.Restriction.IsValid(info.ParameterType))
                    {
                        Plugin.Warn($"Parameter restriction ({restrictionAttribute.Restriction.GetType().Name}) on parameter {info.Name} (type: {info.ParameterType.FullName}) is invalid (validation failed)!");
                        return;
                    }

                    list.Add(restrictionAttribute.Restriction);
                }
            });

            restrictions = list.ToArray();
            return restrictions.Any();
        }

        internal static bool TryGetParameterType(Type type, out ParameterType parameterType)
        {
            var origType = type;

            if (!TryFixType(type, out type))
            {
                parameterType = default;
                return false;
            }

            if (origType.IsNumericType())
            {
                parameterType = ParameterType.Number;
                return true;
            }

            if (origType == typeof(string))
            {
                parameterType = ParameterType.String;
                return true;
            }

            if (origType == typeof(bool))
            {
                parameterType = ParameterType.Boolean;
                return true;
            }

            if (origType == typeof(TimeSpan))
            {
                parameterType = ParameterType.TimeSpan;
                return true;
            }

            if (origType == typeof(DateTime))
            {
                parameterType = ParameterType.DateTime;
                return true;
            }

            if (type == typeof(IPlayer))
            {
                parameterType = ParameterType.Player;
                return true;
            }

            if (type == typeof(ICommandContext))
            {
                parameterType = ParameterType.Context;
                return true;
            }

            if (type == typeof(IDictionary))
            {
                parameterType = ParameterType.CollectionDictionary;
                return true;
            }

            if (type == typeof(IEnumerable))
            {
                parameterType = ParameterType.CollectionList;
                return true;
            }

            if (origType == typeof(ReferenceHub))
            {
                parameterType = ParameterType.Hub;
                return true;
            }

            if (type == typeof(Enum))
            {
                parameterType = ParameterType.Enum;
                return true;
            }

            if (type == typeof(Array))
            {
                parameterType = ParameterType.CollectionArray;
                return true;
            }

            if (type == typeof(DoorVariant))
            {
                parameterType = ParameterType.Door;
                return true;
            }

            if (origType == typeof(RoomIdentifier))
            {
                parameterType = ParameterType.Room;
                return true;
            }

            if (origType == typeof(GameObject))
            {
                parameterType = ParameterType.GameObject;
                return true;
            }

            if (origType == typeof(NetworkIdentity))
            {
                parameterType = ParameterType.NetworkIdentity;
                return true;
            }

            if (type == typeof(IParameterData))
            {
                var typeMethod = Reflection.Method(origType, "GetParameterType");

                if (typeMethod is null)
                {
                    Plugin.Warn($"Failed to find \"GetParameterType\" method in IParameterData interface: {origType.FullName}");

                    parameterType = default;
                    return false;
                }

                var result = typeMethod.Invoke(null, null);

                if (result is null)
                {
                    Plugin.Warn($"\"GetParameterType\" method in IParameterData (interface: {origType.FullName}) returned a null object!");

                    parameterType = default;
                    return false;
                }

                if (!(result is ParameterType pType))
                {
                    Plugin.Warn($"\"GetParameterType\" method in IParameterData (interface: {origType.FullName}) returned an object of invalid type! (expected: ParameterType, got: {result.GetType().FullName})");

                    parameterType = default;
                    return false;
                }

                parameterType = pType;
                return true;
            }

            Plugin.Warn($"Tried searching an unsupported type: {origType.FullName}");

            parameterType = default;
            return false;
        }

        // FLAG ENUM MANIPULATION

        // add flag: flags |= flagToAdd
        // add multiple flags: flags |= (flag1 | flag2)

        // remove flag: flags &= ~flagToRemove
        // remove multiple flags: flags &= ~(flag1 | flag2)

        internal static bool TryGetFlags(ParameterInfo info, int index, out ParameterFlags flags)
        {
            flags = default;

            try
            {
                if (info.IsOptional)
                    flags |= ParameterFlags.Optional;

                if (info.IsDefined(typeof(RemainderAttribute)))
                    flags |= ParameterFlags.Remainder;

                if (info.IsDefined(typeof(MultipleAttribute)))
                    flags |= ParameterFlags.Multiple;

                if (Reflection.HasInterface<ICommandContext>(info.ParameterType))
                    flags |= ParameterFlags.Context;

                if (Reflection.HasInterface<IPlayer>(info.ParameterType) && index == 0)
                    flags |= ParameterFlags.Sender;

                if (info.ParameterType == typeof(ReferenceHub) && index == 0)
                    flags |= ParameterFlags.SenderHub;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to retrieve flags of parameter {info.Name} (type: {info.ParameterType.FullName})!\n{ex}");
                return false;
            }

            Plugin.Debug($"Retrieved flags for parameter (name={info.Name} index={index} type={info.ParameterType.FullName}): {flags}");
            return true;
        }

        public static bool IsNumericType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
