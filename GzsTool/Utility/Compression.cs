using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace GzsTool.Utility
{
    internal static class Compression
    {
        internal static byte[] Inflate(byte[] buffer)
        {
            InflaterInputStream inflaterStream = new InflaterInputStream(new MemoryStream(buffer));
            MemoryStream outputStream = new MemoryStream();
            inflaterStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        internal static byte[] Deflate(byte[] buffer)
        {
            MemoryStream compressedBufferStream = new MemoryStream();
            DeflaterOutputStream deflaterStream = new DeflaterOutputStream(compressedBufferStream);
            deflaterStream.Write(buffer, 0, buffer.Length);
            deflaterStream.Close();
            return compressedBufferStream.ToArray();
        }
    }
}