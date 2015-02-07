using System;

namespace GzsTool.Common
{
    internal class VirtualFileSystemDirectory : AbstractDirectory
    {
        public override byte[] ReadFile(string relativeFilePath)
        {
            throw new NotImplementedException();
        }
    }
}
