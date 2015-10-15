using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Utility;

namespace GzsTool.Core.Pftxs
{
    [XmlType("Entry", Namespace = "Pftxs")]
    public class PftxsFtexFile
    {
        private const int HeaderSize = 32;
        
        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlArray("Entries")]
        public List<PftxsFtexsFileEntry> Entries { get; set; }

        public bool ShouldSerializeHash()
        {
            var firstEntry = Entries.FirstOrDefault();
            return firstEntry != null && !firstEntry.FileNameFound;
        }

        private void CalculateHash()
        {
            var firstEntry = Entries.FirstOrDefault();
            if (firstEntry != null)
            {
                Hash = firstEntry.Hash;
            }
        }

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
                PftxsFtexsFileEntry entry = new PftxsFtexsFileEntry();
                entry.Read(input);

                string name;
                entry.FileNameFound = Hashing.TryGetFileNameFromHash(entry.Hash, out name);
                entry.FilePath = name;
                Entries.Add(entry);
            }
            
            foreach (var entry in Entries)
            {
                reader.BaseStream.Position = ftexBaseOffset + entry.Offset;
                entry.Data = reader.ReadBytes(entry.Size);
            }
        }
        
        public void WriteData(BinaryWriter writer, IDirectory inputDirectory)
        {
            long ftexHeaderPosition = writer.BaseStream.Position;
            writer.BaseStream.Position += HeaderSize + Entries.Count * PftxsFtexsFileEntry.HeaderSize;
            
            foreach (var entry in Entries)
            {
                entry.CalculateHash();
                var data = inputDirectory.ReadFile(Hashing.NormalizeFilePath(entry.FilePath));
                entry.Offset = Convert.ToInt32(writer.BaseStream.Position - ftexHeaderPosition);
                entry.Size = Convert.ToInt32(data.Length);
                writer.Write(data);
            }
            CalculateHash();

            long endPosition = writer.BaseStream.Position;
            writer.BaseStream.Position = ftexHeaderPosition;

            writer.Write(Convert.ToUInt32(0x58455446)); // FTEX
            writer.Write(Convert.ToUInt32(endPosition - ftexHeaderPosition)); // Size
            writer.Write(Hash);
            writer.Write(Convert.ToUInt32(Entries.Count));
            writer.Write(0U);
            writer.Write(0U);
            writer.Write(0U);

            foreach (var entry in Entries)
            {
                entry.Write(writer);
            }

            writer.BaseStream.Position = endPosition;
        }
    }
}
