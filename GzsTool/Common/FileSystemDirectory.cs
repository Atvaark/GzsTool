using System.IO;

namespace GzsTool.Common
{
    internal class FileSystemDirectory : AbstractDirectory
    {
        private readonly string _baseDirectory;

        public FileSystemDirectory(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        public override byte[] ReadFile(string relativeFilePath)
        {
            string inputFilePath = Path.Combine(_baseDirectory, relativeFilePath);
            using (FileStream input = new FileStream(inputFilePath, FileMode.Open))
            {
                byte[] data = new byte[input.Length];
                input.Read(data, 0, data.Length);
                return data;
            }
        }
    }
}
