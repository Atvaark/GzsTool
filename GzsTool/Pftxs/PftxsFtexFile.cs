using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common.Interfaces;

namespace GzsTool.Pftxs
{
    [XmlType("Entry", Namespace = "Pftxs")]
    public class PftxsFtexFile
    {
        public const int HeaderSize = 32;
        
        [XmlIgnore]
        public string FileName { get; set; }

        [XmlIgnore]
        public long DataOffset { get; set; }
        
        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlArray("Entries")]
        public List<PftxsFtexsFileEntry> Entries { get; set; }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            long ftexBaseOffset = reader.BaseStream.Position;
            int magicNumber = reader.ReadInt32(); // FTEX
            int size = reader.ReadInt32();
            Hash = reader.ReadUInt64();
            int count = reader.ReadInt32();
            int unknown1 = reader.ReadInt32(); // 0
            int unknown2 = reader.ReadInt32(); // 0
            int unknown3 = reader.ReadInt32(); // 0

            Entries = new List<PftxsFtexsFileEntry>();
            for (int i = 0; i < count; i++)
            {
                PftxsFtexsFileEntry ftexsFileEntry = new PftxsFtexsFileEntry();
                ftexsFileEntry.Read(input);
                Entries.Add(ftexsFileEntry);
            }
            
            foreach (var entry in Entries)
            {
                reader.BaseStream.Position = ftexBaseOffset + entry.Offset;
                entry.Data = reader.ReadBytes(entry.Size);
            }
        }


        public void Write(Stream output)
        {
            throw new NotImplementedException();
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
