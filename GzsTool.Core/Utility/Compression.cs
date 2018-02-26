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