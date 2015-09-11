using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common.Interfaces;

namespace GzsTool.Pftxs
{
    [XmlType("EntryData", Namespace = "Pftxs")]
    public class PftxsFtexsFileEntry
    {
        public const int HeaderSize = 16;

        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlIgnore]
        public int Offset { get; set; }

        [XmlIgnore]
        public int Size { get; set; }

        [XmlIgnore]
        public byte[] Data { get; set; }


        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            Hash = reader.ReadUInt64();
            Offset = reader.ReadInt32();
            Size = reader.ReadInt32();
        }
        
        public void Write(BinaryWriter writer)
        {
            writer.Write(Hash);
            writer.Write(Offset);
            writer.Write(Size);
        }
    }
}