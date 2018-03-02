using System;
using System.Collections.Generic;
using System.IO;

namespace GzsTool.Core.Common.Interfaces
{
    public interface IDirectory : IFileSystemEntry
    {
        IEnumerable<IFileSystemEntry> Entries { get; }
        byte[] ReadFile(string filePath);
        Stream ReadFileStream(string filePath);
        void WriteFile(string filePath, Func<Stream> fileContentStream);
    }
}
