using System;
using System.IO;

namespace GzsTool.Core.Common
{
    public class FileDataStreamContainer
    {
        public string FileName { get; set; }
        public Func<Stream> DataStream { get; set; }
    }
}
