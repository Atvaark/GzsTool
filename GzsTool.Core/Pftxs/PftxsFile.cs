using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Utility;

namespace GzsTool.Core.Pftxs
{
    [XmlType("PftxsFile")]
    public class PftxsFile : ArchiveFile
    {
        private const long FtexHeaderSize = 16;
        private const long TexlHeaderSize = 16;

        public PftxsFile()
        {
            Files = new List<PftxsFtexFile>();
        }

        [XmlArray("Entries")]
        public List<PftxsFtexFile> Files { get; set; }

        [XmlIgnore]
        public int Size { get; set; }

        [XmlIgnore]
        public int FileCount { get; set; }
        
        public override void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            int pftxsMagicNumber = reader.ReadInt32(); // PFTXS
            int unknown1 = reader.ReadInt32();
            int unknown2 = reader.ReadInt32();
            int unknown3 = reader.ReadInt32();

            int texlistMagicNumber = reader.ReadInt32(); // TEXL
            Size = reader.ReadInt32();
            FileCount = reader.ReadInt32();
            int unknown4 = reader.ReadInt32();

            for (int i = 0; i < FileCount; i++)
            {
                PftxsFtexFile pftxsFtexFile = new PftxsFtexFile();
                pftxsFtexFile.Read(input);
                Files.Add(pftxsFtexFile);
            }
        }

        public override IEnumerable<FileDataStreamContainer> ExportFiles(Stream input)
        {
            foreach (var file in Files)
            {
                foreach (var entry in file.Entries)
                {
                    var localEntry = entry;
                    yield return new FileDataStreamContainer
                    {
                        DataStream = () => new MemoryStream(localEntry.Data),
                        FileName = Hashing.NormalizeFilePath(entry.FilePath)
                    };
                }
            }
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            long ftexHeaderPosition = output.Position;
            output.Position += FtexHeaderSize;
            long texlHeaderPosition = output.Position;
            output.Position += TexlHeaderSize;
            foreach (var file in Files)
            {
                file.WriteData(writer, inputDirectory);
            }

            long endPosition = output.Position;
            output.Position = ftexHeaderPosition;
            writer.Write(0x58544650); // PFTX
            writer.Write(0x40000000);
            writer.Write(0x00000010);
            writer.Write(0x00000001);

            output.Position = texlHeaderPosition;
            writer.Write(0x4C584554); // TEXL
            writer.Write(Convert.ToUInt32(endPosition - texlHeaderPosition)); // Size
            writer.Write(Convert.ToUInt32(Files.Count));
            
            output.Position = endPosition;
        }
    }
}
