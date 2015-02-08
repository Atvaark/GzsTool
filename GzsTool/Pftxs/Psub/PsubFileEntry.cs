using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;

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

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

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

        public byte[] ReadData(Stream input)
        {
            input.Position = Offset;
            byte[] result = new byte[Size];
            input.Read(result, 0, Size);
            return result;
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Offset);
            writer.Write(Size);
        }

        public void WriteData(Stream output, AbstractDirectory inputDirectory)
        {
            byte[] data = inputDirectory.ReadFile(FilePath);
            Size = data.Length;
            output.Write(data, 0, data.Length);
        }
    }
}
