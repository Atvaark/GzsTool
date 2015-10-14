using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;

namespace GzsTool.Core.Sbp
{
    [XmlType("Entry", Namespace = "Sbp")]
    public class SbpEntry
    {
        public const int HeaderSize = 12;

        [XmlAttribute("Name")]
        public string FileName { get; set; }

        [XmlIgnore]
        public string Magic { get; set; }

        [XmlIgnore]
        public uint Offset { get; set; }

        [XmlIgnore]
        public int Size { get; set; }
        
        public void Read(BinaryReader reader)
        {
            Magic = reader.ReadString(4); // bnk, stp or sab
            Offset = reader.ReadUInt32();
            Size = reader.ReadInt32();

            FileName = FileName + "." + Magic.TrimEnd('\0');
        }

        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = FileName
            };
            return fileDataStreamContainer;
        }

        private Func<Stream> ReadDataLazy(Stream input)
        {
            return () =>
            {
                lock (input)
                {
                    return ReadData(input);
                }
            };
        }

        private Stream ReadData(Stream input)
        {
            byte[] data = new byte[Size];
            input.Position = Offset;
            input.Read(data, 0, Size);
            return new MemoryStream(data);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes(Magic));
            writer.Write(Offset);
            writer.Write(Size);
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            Magic = Path.GetExtension(FileName).Trim('.') + '\0';
            Offset = (uint)output.Position;
            byte[] data = inputDirectory.ReadFile(FileName);
            Size = data.Length;
            output.Write(data, 0, data.Length);
        }
    }
}