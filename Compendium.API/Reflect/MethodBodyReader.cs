using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

using helpers.Extensions;
using helpers;

namespace Compendium.Reflect
{
    public class MethodBodyReader
    {
        private static readonly OpCode[] _oneByteCodes;
        private static readonly OpCode[] _twoByteCodes;

        static MethodBodyReader()
        {
            _oneByteCodes = new OpCode[0xe1];
            _twoByteCodes = new OpCode[0x1f];

            typeof(OpCodes).ForEachField(field =>
            {
                if (!field.IsStatic || field.FieldType != typeof(OpCode))
                    return;

                var code = field.GetValue(null).As<OpCode>();

                if (code.OpCodeType is OpCodeType.Nternal)
                    return;

                if (code.Size is 1)
                    _oneByteCodes[code.Value] = code;
                else
                    _twoByteCodes[code.Value & 0xff] = code;
            });
        }

        private MethodBase _method;
        private MethodBody _body;
        private Module _module;
        private ByteBuffer _ilBuffer;

        private Type[] _typeArgs;
        private Type[] _methodArgs;

        private ParameterInfo[] _params;

        private IList<LocalVariableInfo> _locals;
        private List<Instruction> _instructions = new List<Instruction>();

        private Instruction _instruction;

        public MethodBodyReader(MethodBase method)
        {
            _method = method;
            _body = method.GetMethodBody();

            if (_body == null)
                throw new ArgumentException();

            var bytes = _body.GetILAsByteArray();
            
            if (bytes == null)
                throw new ArgumentException();

            if (!(method is ConstructorInfo))
                _methodArgs = method.GetGenericArguments();

            if (method.DeclaringType != null)
                _typeArgs = method.DeclaringType.GetGenericArguments();

            _params = method.GetParameters();
            _locals = _body.LocalVariables;
            _module = method.Module;
            _ilBuffer = new ByteBuffer(bytes);
        }

        public void ReadInstructions()
        {
            while (_ilBuffer.Position < _ilBuffer.Size)
            {
                CreateInstruction();
                ReadInstruction();

                _instructions.Add(_instruction);
            }
        }

        public void CreateInstruction()
        {
            var previous = _instruction;
            _instruction = new Instruction(_ilBuffer.Position, ReadOpCode());

            if (previous != null)
            {
                _instruction.Previous = previous;
                previous.Next = _instruction;
            }
        }

        public void ReadInstruction()
        {
            switch (_instruction.Code.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineSwitch:
                    {
                        var length = _ilBuffer.ReadInt32();

                        var branches = new int [length];
                        var offsets = new int [length];

                        for (int i = 0; i < length; i++)
                            offsets[i] = _ilBuffer.ReadInt32();

                        for (int i = 0; i < length; i++)
                            branches[i] = _ilBuffer.Position + offsets[i];

                        _instruction.Operand = branches;
                        break;
                    }
                case OperandType.ShortInlineBrTarget:
                    _instruction.Operand = _ilBuffer.Position - (sbyte)_ilBuffer.ReadByte();
                    break;
                case OperandType.InlineBrTarget:
                    _instruction.Operand = _ilBuffer.Position - _ilBuffer.ReadInt32();
                    break;
                case OperandType.ShortInlineI:
                    if (_instruction.Code == OpCodes.Ldc_I4_S)
                        _instruction.Operand = (sbyte)_ilBuffer.ReadByte();
                    else
                        _instruction.Operand = _ilBuffer.ReadByte();
                    break;
                case OperandType.InlineI:
                    _instruction.Operand = _ilBuffer.ReadInt32();
                    break;
                case OperandType.ShortInlineR:
                    _instruction.Operand = _ilBuffer.ReadSingle();
                    break;
                case OperandType.InlineR:
                    _instruction.Operand = _ilBuffer.ReadDouble();
                    break;
                case OperandType.InlineI8:
                    _instruction.Operand = _ilBuffer.ReadInt64();
                    break;
                case OperandType.InlineSig:
                    _instruction.Operand = _module.ResolveSignature(_ilBuffer.ReadInt32());
                    break;
                case OperandType.InlineString:
                    _instruction.Operand = _module.ResolveString(_ilBuffer.ReadInt32());
                    break;
                case OperandType.InlineTok:
                    _instruction.Operand = _module.ResolveMember(_ilBuffer.ReadInt32(), _typeArgs, _methodArgs);
                    break;
                case OperandType.InlineType:
                    _instruction.Operand = _module.ResolveType(_ilBuffer.ReadInt32(), _typeArgs, _methodArgs);
                    break;
                case OperandType.InlineMethod:
                    _instruction.Operand = _module.ResolveMethod(_ilBuffer.ReadInt32(), _typeArgs, _methodArgs);
                    break;
                case OperandType.InlineField:
                    _instruction.Operand = _module.ResolveField(_ilBuffer.ReadInt32(), _typeArgs, _methodArgs);
                    break;
                case OperandType.ShortInlineVar:
                    _instruction.Operand = GetVariable(_ilBuffer.ReadByte());
                    break;
                case OperandType.InlineVar:
                    _instruction.Operand = GetVariable(_ilBuffer.ReadInt16());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private object GetVariable(int index)
        {
            if (TargetsLocalVariable(_instruction.Code))
                return GetLocalVariable(index);
            else
                return GetParameter(index);
        }

        private LocalVariableInfo GetLocalVariable(int index)
            => _locals[index];

        private ParameterInfo GetParameter(int index)
        {
            if (!_method.IsStatic)
                index--;

            return _params[index];
        }

        private OpCode ReadOpCode()
        {
            var code = _ilBuffer.ReadByte();
            return code != 0xfe
              ? _oneByteCodes[code]
              : _twoByteCodes[_ilBuffer.ReadByte()];
        }

        private static bool TargetsLocalVariable(OpCode opcode)
            => opcode.Name.Contains("loc");

        public static List<Instruction> GetInstructions(MethodBase method)
        {
            var reader = new MethodBodyReader(method);
            reader.ReadInstructions();
            return reader._instructions;
        }
    }
}
