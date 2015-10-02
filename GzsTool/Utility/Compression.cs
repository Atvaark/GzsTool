using System.IO;
using Ionic.Zlib;

namespace GzsTool.Utility
{
    internal static class Compression
    {
        internal static byte[] Inflate(byte[] buffer)
        {
            return ZlibStream.UncompressBuffer(buffer);
        }

        internal static byte[] Deflate(byte[] buffer)
        {
            using (Stream input = new MemoryStream(buffer))
            using (Stream zlibInput = new ZlibStream(input, CompressionMode.Compress, CompressionLevel.Default))
            {
                return zlibInput.ToArray();
            }
        }
    }
}