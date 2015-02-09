using System;
using System.IO;
using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    public class VirtualFileSystemFile : IFile
    {
        private readonly string _name;
        private Func<Stream> _dataStream;

        public VirtualFileSystemFile(string name, Func<Stream> dataStream)
        {
            _dataStream = dataStream;
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public byte[] Content
        {
            get { return _dataStream().ToArray(); }
        }

        public Stream ContentStream
        {
            get { return _dataStream(); }
        }

        public override string ToString()
        {
            return Name;
        }

        public void ResetStreamFactoryMethod()
        {
            // HACK: In case the dataStream contains a huge memory stream.
            _dataStream = () => new MemoryStream();
        }
    }
}
