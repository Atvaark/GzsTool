using System;
using System.IO;

namespace GzsTool.Core.Crypto
{
    public class Decrypt1Stream : Stream
    {
        private readonly Stream _input;
        
        private readonly int _size;

        private readonly int _version;

        private readonly uint _hashLow;

        private readonly StreamMode _streamMode;

        private readonly ulong _seed;

        private readonly uint _seedLow;

        private readonly uint _seedHigh;

        private int _position;

        public Decrypt1Stream(Stream input, int version, int size, byte[] dataHash, uint hashLow, StreamMode streamMode)
        {
            _input = input;
            _version = version;
            _size = size;
            _hashLow = hashLow;
            _streamMode = streamMode;
            _seed = BitConverter.ToUInt64(dataHash, (int)(hashLow % 2) * 8);
            _seedLow = (uint)_seed & 0xFFFFFFFF;
            _seedHigh = (uint)(_seed >> 32);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
            {
                throw new NotSupportedException();
            }

            if (count > _size - _position)
            {
                count = _size - _position;
            }

            int bytesRead = _input.Read(buffer, offset, count);
            if (bytesRead != 0)
            {
                Decrypt1(buffer);
            }

            _position += bytesRead;
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
            {
                throw new NotSupportedException();
            }

            Decrypt1(buffer);
            _input.Write(buffer, offset, count);
            _position += count;
        }

        public override bool CanRead
        {
            get
            {
                return _streamMode == StreamMode.Read;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _streamMode == StreamMode.Write;
            }
        }

        public override long Length
        {
            get
            {
                return _size;
            }
        }

        public override long Position
        {
            get
            {
                return _position;
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        private void Decrypt1(byte[] data)
        {
            uint[] decryptionTable =
            {
                0xBB8ADEDB,
                0x65229958,
                0x08453206,
                0x88121302,
                0x4C344955,
                0x2C02F10C,
                0x4887F823,
                0xF3818583
            };

            int blocks = data.Length / sizeof(ulong);
            if (_version != 2)
            {
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int offset1Absolute = offset1 + _position;

                    int index = (int)(2 * ((_hashLow + offset1Absolute / 11) % 4));
                    uint u1 = BitConverter.ToUInt32(data, offset1) ^ decryptionTable[index];
                    uint u2 = BitConverter.ToUInt32(data, offset2) ^ decryptionTable[index + 1];
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, data, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, data, offset2, sizeof(uint));
                }

                int remaining = data.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int offsetAbsolute = offset + _position;

                    int index = (int)(2 * ((_hashLow + (offsetAbsolute - (offsetAbsolute % sizeof(long))) / 11) % 4));
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte)((xorMask >> (8 * decryptionIndex)) & 0xff);
                    byte b1 = (byte)(data[offset] ^ xorMaskByte);
                    data[offset] = b1;
                }
            }
            else
            {
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int offset1Absolute = offset1 + _position;

                    int index = 2 * (int)((_hashLow + _seed + (ulong)(offset1Absolute / 11)) % 4);
                    uint u1 = BitConverter.ToUInt32(data, offset1) ^ decryptionTable[index] ^ _seedLow;
                    uint u2 = BitConverter.ToUInt32(data, offset2) ^ decryptionTable[index + 1] ^ _seedHigh;
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, data, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, data, offset2, sizeof(uint));
                }

                int remaining = data.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int offsetBlock = offset - (offset % sizeof(long));
                    int offsetBlockAbolute = offsetBlock + _position;

                    int index = 2 * (int)((_hashLow + _seed + (ulong)(offsetBlockAbolute / 11)) % 4);
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte)((xorMask >> (8 * (decryptionIndex % 4))) & 0xff);
                    uint seedMask = decryptionIndex < 4 ? _seedLow : _seedHigh;
                    byte seedByte = (byte)((seedMask >> (8 * (decryptionIndex % 4))) & 0xff);
                    data[offset] = (byte)(data[offset] ^ (byte)(xorMaskByte ^ seedByte));
                }
            }
        }
    }
}