using System.IO;
using System.Linq;
using System.Text;

namespace GzsTool
{
    internal static class ExtensionMethods
    {
        internal static void Skip(this BinaryReader reader, int count)
        {
            reader.BaseStream.Skip(count);
        }

        internal static void Skip(this Stream stream, int count)
        {
            stream.Seek(count, SeekOrigin.Current);
        }

        internal static void WriteZeros(this BinaryWriter writer, int count)
        {
            byte[] zeros = new byte[count];
            writer.Write(zeros);
        }

        internal static string ReadString(this BinaryReader binaryReader, int count)
        {
            return new string(binaryReader.ReadChars(count));
        }

        internal static string ReadNullTerminatedString(this BinaryReader reader)
        {
            StringBuilder builder = new StringBuilder();
            char nextCharacter;
            while ((nextCharacter = reader.ReadChar()) != 0)
            {
                builder.Append(nextCharacter);
            }
            return builder.ToString();
        }

        internal static void WriteNullTerminatedString(this BinaryWriter writer, string text)
        {
            byte[] data = Encoding.Default.GetBytes(text + '\0');
            writer.Write(data, 0, data.Length);
        }

        internal static void AlignRead(this Stream input, int alignment)
        {
            long alignmentRequired = input.Position%alignment;
            if (alignmentRequired > 0)
                input.Position += alignment - alignmentRequired;
        }

        internal static void AlignWrite(this Stream output, int alignment, byte data)
        {
            long alignmentRequired = output.Position%alignment;
            if (alignmentRequired > 0)
            {
                byte[] alignmentBytes = Enumerable.Repeat(data, (int) (alignment - alignmentRequired)).ToArray();
                output.Write(alignmentBytes, 0, alignmentBytes.Length);
            }
        }
    }
}
