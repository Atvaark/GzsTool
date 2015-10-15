using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;

namespace GzsTool.Core.Sbp
{
    [XmlType("SbpFile")]
    public class SbpFile : ArchiveFile
    {
        [XmlArray("Entries")]
        public List<SbpEntry> Entries { get; set; }
        
        public override void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true);
            uint magic = reader.ReadUInt32();
            byte fileCount = reader.ReadByte();
            ushort headerSize = reader.ReadUInt16();
            byte padding = reader.ReadByte();

            List<SbpEntry> entries = new List<SbpEntry>();
            string entityName = Path.GetFileNameWithoutExtension(Name);
            for (int i = 0; i < fileCount; i++)
            {
                var entry = new SbpEntry();
                entry.FileName = entityName;
                entry.Read(reader);
                entries.Add(entry);
            }
            input.AlignRead(16);
            Entries = entries;
        }

        public override IEnumerable<FileDataStreamContainer> ExportFiles(Stream input)
        {
            return Entries.Select(sbpEntry => sbpEntry.Export(input));
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true);

            const int sbpHeaderSize = 8;
            int entityHeaderSize = Entries.Count * SbpEntry.HeaderSize;
            int headerSize = sbpHeaderSize + entityHeaderSize;

            long headerPosition = output.Position;
            output.Position += headerSize;
            output.AlignWrite(16, 0x00);

            foreach (var entry in Entries)
            {
                entry.WriteData(output, inputDirectory);
                output.AlignWrite(16, 0x00);
            }

            long endPosition = output.Position;

            output.Position = headerPosition;
            writer.Write(0x4C504253); // SBPL
            writer.Write(Convert.ToByte(Entries.Count));
            writer.Write(Convert.ToUInt16(headerSize));
            writer.Write((byte)0x00);
            
            foreach (var entry in Entries)
            {
                entry.Write(output);
            }

            output.Position = endPosition;
        }
    }
}