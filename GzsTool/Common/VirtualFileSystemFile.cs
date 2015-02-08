using System;
using System.IO;
using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    public class VirtualFileSystemFile : IFile
    {
        private readonly string _name;
        private readonly Lazy<Stream> _dataStream;

        public VirtualFileSystemFile(string name, Lazy<Stream> dataStream)
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
            get { return _dataStream.Value.ToArray(); }
        }
    }
}
