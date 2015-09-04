using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;

namespace GzsTool.Pftxs
{
    [XmlType("PftxsFile")]
    public class PftxsFile : ArchiveFile
    {
        public PftxsFile()
        {
            Files = new List<PftxsFtexFile>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Entries")]
        public List<PftxsFtexFile> Files { get; set; }

        [XmlIgnore]
        public int Size { get; set; }

        [XmlIgnore]
        public int FileCount { get; set; }

        public static PftxsFile ReadPftxsFile(Stream input)
        {
            PftxsFile pftxsFile = new PftxsFile();
            pftxsFile.Read(input);
            return pftxsFile;
        }

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
                // TODO: Lookup name of the file.
                string name = Guid.NewGuid().ToString();
                foreach (var entry in file.Entries.Select((value, i) => new {e = value, i}))
                {
                    string extension = entry.i == 0 ? ".ftex" : ".1.ftexs";
                    yield return new FileDataStreamContainer
                    {
                        DataStream = () => new MemoryStream(entry.e.Data), FileName = name + extension
                    };
                }
            }
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            throw new NotImplementedException();
        }
    }
}
