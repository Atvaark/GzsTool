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
            return ZlibStream.CompressBuffer(buffer);
        }
    }
}