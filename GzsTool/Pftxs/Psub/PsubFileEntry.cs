using System.IO;
using System.Text;

namespace GzsTool.Pftxs.Psub
{
    public class PsubFileEntry
    {
        public const int PsubFileEntrySize = 8;
        public int Offset { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; }

        public static PsubFileEntry ReadPsubFileEntry(Stream input)
        {
            PsubFileEntry psubFileEntry = new PsubFileEntry();
            psubFileEntry.Read(input);
            return psubFileEntry;
        }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            Offset = reader.ReadInt32();
            Size = reader.ReadInt32();
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Offset);
            writer.Write(Size);
        }

        public void WriteData(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Data);
        }
    }
}
