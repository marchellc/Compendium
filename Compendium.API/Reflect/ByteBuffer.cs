using System;

namespace Compendium.Reflect
{
    public class ByteBuffer
    {
        private byte[] _buffer;
        private int _pos;

        public int Position
        {
            get => _pos;
            set => _pos = value;
        }

        public int Size
        {
            get => _buffer.Length;
        }

        public ByteBuffer(byte[] buffer)
            => _buffer = buffer;

        public byte ReadByte()
        {
            CheckCanRead(1);
            return _buffer[_pos++];
        }

        public short ReadInt16()
        {
            CheckCanRead(2);
            var @short = (short)(_buffer[_pos] + (_buffer[_pos + 1] << 8));
            _pos += 2;
            return @short;
        }

        public int ReadInt32()
        {
            CheckCanRead(4);
            var @int = _buffer [_pos] + (_buffer [_pos + 1] << 8) + (_buffer [_pos + 2] << 16) + (_buffer [_pos + 3] << 24);
            _pos += 4;
            return @int;
        }

        public long ReadInt64()
        {
            CheckCanRead(8);
            var @long = _buffer [_pos] + (_buffer [_pos + 1] << 8) + (_buffer [_pos + 2] << 16) + (_buffer [_pos + 3] << 24) + (_buffer [_pos + 4] << 32) + (_buffer [_pos + 5] << 40) + (_buffer [_pos + 6] << 48) + (_buffer [_pos + 7] << 56);
            _pos += 8;
            return @long;
        }

        public float ReadSingle()
        {
            CheckCanRead(4);
            var single = BitConverter.ToSingle(_buffer, _pos);
            _pos += 4;
            return single;
        }

        public double ReadDouble()
        {
            CheckCanRead(8);
            var @double = BitConverter.ToDouble(_buffer, _pos);
            _pos += 8;
            return @double;
        }

        private void CheckCanRead(int count)
        {
            if (_pos + count > _buffer.Length)
                throw new ArgumentOutOfRangeException();
        }
    }
}