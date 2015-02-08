using System.IO;

namespace GzsTool.Common.Interfaces
{
    public interface IFile : IFileSystemEntry
    {
        byte[] Content { get; }
        Stream ContentStream { get; }
    }
}
