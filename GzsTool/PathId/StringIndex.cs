using System.IO;
using System.Text;

namespace GzsTool.PathId
{
    internal class StringIndex
    {
        public uint Offset { get; set; }

        public static StringIndex ReadStringIndex(Stream input)
        {
            StringIndex stringIndex = new StringIndex();
            stringIndex.Read(input);
            return stringIndex;
        }

        private void Read(Stream input)
        {
            BigEndianBinaryReader reader = new BigEndianBinaryReader(input, Encoding.Default, true);
            Offset = reader.ReadUInt32();
        }
    }
}
