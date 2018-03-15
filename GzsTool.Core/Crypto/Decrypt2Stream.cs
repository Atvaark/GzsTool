using System;
using System.IO;
using System.Linq;

namespace GzsTool.Core.Crypto
{
    public class Decrypt2Stream : Stream
    {
        private readonly Stream _input;

        private readonly int _size;
        
        private readonly uint _key;

        private readonly StreamMode _streamMode;

        private int _position;

        private uint _blockKey;
        
        public Decrypt2Stream(Stream input, int size, uint key, StreamMode streamMode)
        {
            _input = input;
            _size = size;
            _streamMode = streamMode;
            _key = 278 * key;
            _blockKey = key | ((key ^ 25974) << 16);
        }
        
        public override void Flush()
        {
            _input.Flush();
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
                Decrypt2(buffer, bytesRead);
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
            
            Decrypt2(buffer, count);
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

        private unsafe void Decrypt2(byte[] input, int size)
        {
            if (size > input.Length)
            {
                throw new ArgumentException();
            }

            byte[] output = input.ToArray();
            fixed (byte* pDestBase = output, pSrcBase = input)
            {
                uint* pDest = (uint*)pDestBase;
                uint* pSrc = (uint*)pSrcBase;
                
                for (; size >= 64; size -= 64)
                {
                    uint j = 16;
                    do
                    {
                        *pDest = _blockKey ^ *pSrc;
                        _blockKey = _key + 48828125 * _blockKey;

                        --j;
                        pDest++;
                        pSrc++;
                    } while (j > 0);
                }

                for (; size >= 16; pSrc += 4)
                {
                    *pDest = _blockKey ^ *pSrc;
                    uint v7 = _key + 48828125 * _blockKey;
                    *(pDest + 1) = v7 ^ *(pSrc + 1);
                    uint v8 = _key + 48828125 * v7;
                    *(pDest + 2) = v8 ^ *(pSrc + 2);
                    uint v9 = _key + 48828125 * v8;
                    *(pDest + 3) = v9 ^ *(pSrc + 3);

                    _blockKey = _key + 48828125 * v9;
                    size -= 16;
                    pDest += 4;
                }

                for (; size >= 4; pSrc++)
                {
                    *pDest = _blockKey ^ *pSrc;

                    _blockKey = _key + 48828125 * _blockKey;
                    size -= 4;
                    pDest++;
                }

                // The final 0-3 bytes aren't encrypted
            }

            Buffer.BlockCopy(output, 0, input, 0, input.Length);
        }
    }
}