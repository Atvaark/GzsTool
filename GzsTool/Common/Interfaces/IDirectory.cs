using System;
using System.Collections.Generic;
using System.IO;

namespace GzsTool.Common.Interfaces
{
    public interface IDirectory : IFileSystemEntry
    {
        IEnumerable<IFileSystemEntry> Entries { get; }
        byte[] ReadFile(string filePath);
        void WriteFile(string filePath, Lazy<Stream> fileContentStream);
    }
}
