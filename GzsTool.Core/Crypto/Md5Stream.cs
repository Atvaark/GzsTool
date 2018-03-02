using System.IO;
using System.Security.Cryptography;

namespace GzsTool.Core.Crypto
{
    public class Md5Stream : Stream
    {
        private readonly CryptoStream _stream;

        private readonly MD5 _md5;

        public Md5Stream(Stream stream)
        {
            _md5 = MD5.Create();
            _stream = new CryptoStream(
                stream,
                _md5,
                CryptoStreamMode.Write);
        }

        public override void Flush()
        {
            _stream.Flush();
            _stream.FlushFinalBlock();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public byte[] Hash
        {
            get { return _md5.Hash; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _md5.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}