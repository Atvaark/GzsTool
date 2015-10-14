using System.IO;

namespace GzsTool.Core.Common.Interfaces
{
    public interface IFile : IFileSystemEntry
    {
        byte[] Content { get; }
        Stream ContentStream { get; }
    }
}
