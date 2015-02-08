using System.IO;
using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    public class FileSystemFile : IFile
    {
        private readonly string _path;

        public FileSystemFile(string path)
        {
            _path = path;
        }

        public string Name
        {
            get { return Path.GetFileName(_path); }
        }

        public byte[] Content
        {
            get { return File.ReadAllBytes(_path); }
        }

        public Stream ContentStream
        {
            get { return new FileStream(_path, FileMode.Open); }
        }
    }
}
