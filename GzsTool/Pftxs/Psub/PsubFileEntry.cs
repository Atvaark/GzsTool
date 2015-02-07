using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace GzsTool.Pftxs.Psub
{
    [XmlType("Entry", Namespace = "Psub")]
    public class PsubFileEntry
    {
        public const int PsubFileEntrySize = 8;

        [XmlIgnore]
        public int Offset { get; set; }

        [XmlIgnore]
        public int Size { get; set; }

        [XmlIgnore]
        public byte[] Data { get; set; }

        [XmlAttribute("FileName")]
        public string FileName { get; set; }

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
