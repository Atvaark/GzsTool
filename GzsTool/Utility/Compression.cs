using System.IO;
using Ionic.Zlib;

namespace GzsTool.Utility
{
    internal static class Compression
    {
        internal static byte[] Uncompress(byte[] buffer)
        {
            return ZlibStream.UncompressBuffer(buffer);
        }

        internal static byte[] Compress(byte[] buffer)
        {
            using (var output = new MemoryStream())
            {
                using (Stream compressor = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression))
                {
                    compressor.Write(buffer, 0, buffer.Length);
                }
                return output.ToArray();
            }
        }
    }
}