using System.IO;
using System.Text;

namespace GzsTool.Fpk
{
    internal class FpkEntry
    {
        public byte[] Data { get; set; }
        public uint OffsetData { get; set; }
        public int SizeDaza { get; set; }
        public FpkString FileName { get; set; }
        public byte[] Md5Hash { get; set; }

        public static FpkEntry ReadFpkEntry(Stream input)
        {
            FpkEntry fpkEntry = new FpkEntry();
            fpkEntry.Read(input);
            return fpkEntry;
        }

        private void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            OffsetData = reader.ReadUInt32();
            reader.Skip(4);
            SizeDaza = reader.ReadInt32();
            reader.Skip(4);
            FileName = FpkString.ReadFpkString(input);
            Md5Hash = reader.ReadBytes(16);
            long endPosition = input.Position;
            input.Position = OffsetData;
            Data = reader.ReadBytes(SizeDaza);
            input.Position = endPosition;
        }
    }
}
