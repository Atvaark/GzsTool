namespace GzsTool.Common.Interfaces
{
    public interface IFile : IFileSystemEntry
    {
        byte[] Content { get; }
    }
}
