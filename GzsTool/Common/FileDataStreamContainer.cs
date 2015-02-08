using System;
using System.IO;

namespace GzsTool.Common
{
    public class FileDataStreamContainer
    {
        public string FileName { get; set; }
        public Lazy<Stream> DataStream { get; set; }
    }
}
