using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GzsTool.Common.Interfaces;

namespace GzsTool.Common
{
    public class FileSystemDirectory : IDirectory
    {
        private readonly string _baseDirectoryPath;
        private readonly string _name;

        public FileSystemDirectory(string baseDirectoryPath)
        {
            _baseDirectoryPath = baseDirectoryPath;
            _name = Path.GetFileName(baseDirectoryPath);
        }

        public byte[] ReadFile(string filePath)
        {
            string inputFilePath = Path.Combine(_baseDirectoryPath, filePath);
            using (FileStream input = new FileStream(inputFilePath, FileMode.Open))
            {
                byte[] data = new byte[input.Length];
                input.Read(data, 0, data.Length);
                return data;
            }
        }

        public void WriteFile(string filePath, Lazy<Stream> fileContentStream)
        {
            string outputFilePath = Path.Combine(_baseDirectoryPath, filePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            using (FileStream output = new FileStream(outputFilePath, FileMode.Create))
            {
                fileContentStream.Value.CopyTo(output);
            }
        }
        
        public IEnumerable<IFileSystemEntry> Entries
        {
            get { return GetEntries(); }
        }

        public string Name
        {
            get { return _name; }
        }

        private IEnumerable<IFileSystemEntry> GetEntries()
        {
            List<IFileSystemEntry> entries = new List<IFileSystemEntry>();
            DirectoryInfo baseDirectoryInfo = new DirectoryInfo(_baseDirectoryPath);
            entries.AddRange(baseDirectoryInfo.GetDirectories().Select(d => new FileSystemDirectory(d.FullName)));
            entries.AddRange(baseDirectoryInfo.GetFiles().Select(f => new FileSystemFile(f.FullName)));
            return entries.OrderBy(e => e.Name);
        }
    }
}
