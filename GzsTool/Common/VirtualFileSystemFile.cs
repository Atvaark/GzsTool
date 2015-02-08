using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    public class VirtualFileSystemFile : IFile
    {
        private readonly byte[] _content;
        private readonly string _name;

        public VirtualFileSystemFile(string name, byte[] content)
        {
            _name = name;
            _content = content;
        }

        public string Name
        {
            get { return _name; }
        }

        public byte[] Content
        {
            get { return _content; }
        }
    }
}
