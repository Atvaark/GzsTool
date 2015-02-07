using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Pftxs.Psub;

namespace GzsTool.Pftxs
{
    [XmlType("Entry")]
    public class PftxsFileEntry
    {
        public const int HeaderSize = 8;

        [XmlIgnore]
        public int FileNameOffset { get; set; }

        [XmlIgnore]
        public int FileSize { get; set; }

        [XmlAttribute("FileName")]
        public string FileName { get; set; }

        [XmlAttribute("Directory")]
        public string FileDirectory { get; set; }

        [XmlIgnore]
        public byte[] Data { get; set; }

        [XmlElement("PsubFile")]
        public PsubFile PsubFile { get; set; }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            FileNameOffset = reader.ReadInt32();
            FileSize = reader.ReadInt32();

            long position = input.Position;
            input.Position = FileNameOffset;
            FileName = reader.ReadNullTerminatedString();
            input.Position = position;
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(FileNameOffset);
            writer.Write(FileSize);
        }

        public void WriteFileName(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.WriteNullTerminatedString(FileName);
        }

        public void WriteData(Stream output)
        {
            output.Write(Data, 0, Data.Length);
        }

        public void WritePsubFile(Stream output)
        {
            PsubFile.Write(output);
        }
    }
}
