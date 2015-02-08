using System;
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
        public int DataSize { get; set; }

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
            DataSize = reader.ReadInt32();

            long position = input.Position;
            input.Position = FileNameOffset;
            FileName = reader.ReadNullTerminatedString();
            input.Position = position;
        }

        public Func<Stream> ReadDataLazy(Stream input)
        {
            return () =>
            {
                lock (input)
                {
                    return ReadData(input);
                }
            };
        }

        public Stream ReadData(Stream input)
        {
            input.Position = DataOffset;
            byte[] result = new byte[DataSize];
            input.Read(result, 0, DataSize);
            return new MemoryStream(result);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(FileNameOffset);
            writer.Write(DataSize);
        }

        public void WriteFileName(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.WriteNullTerminatedString(FileName);
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            byte[] data = inputDirectory.ReadFile(FilePath);
            DataSize = data.Length;
            output.Write(data, 0, data.Length);
        }

        public void WritePsubFile(Stream output, IDirectory inputDirectory)
        {
            PsubFile.Write(output, inputDirectory);
        }
    }
}
