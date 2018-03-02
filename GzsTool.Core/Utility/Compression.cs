using System.IO;
using Ionic.Zlib;

namespace GzsTool.Core.Utility
{
    internal static class Compression
    {
        internal static Stream UncompressStream(Stream stream)
        {
            return new ZlibStream(stream, CompressionMode.Decompress, false);
        }
        
        internal static Stream CompressStream(Stream stream)
        {
            return new ZlibStream(stream, CompressionMode.Compress, CompressionLevel.BestCompression, true);
        }
    }
}