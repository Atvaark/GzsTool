using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;

namespace GzsTool.Sbp
{
    [XmlType("SbpFile")]
    public class SbpFile : ArchiveFile
    {
        [XmlArray("Entries")]
        public List<SbpEntry> Entries { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        public static SbpFile ReadSbpFile(FileStream input, string fileName)
        {
            SbpFile sbpFile = new SbpFile();
            sbpFile.Name = fileName;
            sbpFile.Read(input);
            return sbpFile;
        }

        public override void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true);
            uint magic = reader.ReadUInt32();
            byte fileCount = reader.ReadByte();
            ushort headerSize = reader.ReadUInt16();
            byte padding = reader.ReadByte();
            
            List<SbpEntry> entries = new List<SbpEntry>();
            for (int i = 0; i < fileCount; i++)
            {
                var entry = new SbpEntry();
                entry.FileName = Name;
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
            throw new System.NotImplementedException();
        }
    }
}