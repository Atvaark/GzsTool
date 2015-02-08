using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common.Interfaces;
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

        [XmlIgnore]
        public string FileName { get; set; }

        [XmlIgnore]
        public long DataOffset { get; set; }

        [XmlElement("PsubFile")]
        public PsubFile PsubFile { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

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

        public byte[] ReadData(Stream input)
        {
            input.Position = DataOffset;
            byte[] result = new byte[FileSize];
            input.Read(result, 0, FileSize);
            return result;
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

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            byte[] data = inputDirectory.ReadFile(FilePath);
            FileSize = data.Length;
            output.Write(data, 0, data.Length);
        }

        public void WritePsubFile(Stream output, IDirectory inputDirectory)
        {
            PsubFile.Write(output, inputDirectory);
        }
    }
}
