using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common.Interfaces;

namespace GzsTool.Pftxs.Psub
{
    [XmlType("Entry", Namespace = "Psub")]
    public class PsubFileEntry
    {
        public const int PsubFileEntrySize = 8;

        [XmlIgnore]
        public int DataOffset { get; set; }

        [XmlIgnore]
        public int DataSize { get; set; }

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
            DataOffset = reader.ReadInt32();
            DataSize = reader.ReadInt32();
        }

        public Lazy<Stream> ReadDataLazy(Stream input)
        {
            return new Lazy<Stream>(
                () =>
                {
                    lock (input)
                    {
                        return ReadData(input);
                    }
                });
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
            writer.Write(DataOffset);
            writer.Write(DataSize);
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            byte[] data = inputDirectory.ReadFile(FilePath);
            DataSize = data.Length;
            output.Write(data, 0, data.Length);
        }
    }
}
