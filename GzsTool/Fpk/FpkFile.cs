using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;

namespace GzsTool.Fpk
{
    [XmlType("FpkFile")]
    public class FpkFile : ArchiveFile
    {
        public FpkFile()
        {
            Entries = new List<FpkEntry>();
            References = new List<FpkReference>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("FpkType")]
        public FpkType FpkType { get; set; }

        [XmlArray("Entries")]
        public List<FpkEntry> Entries { get; private set; }

        [XmlArray("References")]
        public List<FpkReference> References { get; private set; }

        public static FpkFile ReadFpkFile(Stream input)
        {
            FpkFile fpkFile = new FpkFile();
            fpkFile.Read(input);
            return fpkFile;
        }

        public override void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            uint magicNumber1 = reader.ReadUInt32(); // foxf
            ushort magicNumber2 = reader.ReadUInt16(); // pk
            FpkType = (FpkType) reader.ReadByte(); // ' ' or 'd'
            byte magicNumber3 = reader.ReadByte(); // s
            ushort magicNumber4 = reader.ReadUInt16(); // te
            uint fileSize = reader.ReadUInt32();
            reader.Skip(18);
            uint magicNumber5 = reader.ReadUInt32(); // 2
            uint fileCount = reader.ReadUInt32();
            uint referenceCount = reader.ReadUInt32();
            reader.Skip(4);

            for (int i = 0; i < fileCount; i++)
            {
                Entries.Add(FpkEntry.ReadFpkEntry(input));
            }

            for (int i = 0; i < referenceCount; i++)
            {
                References.Add(FpkReference.ReadFpkReference(input));
            }
        }

        public override IEnumerable<FileDataStreamContainer> ExportFiles(Stream input)
        {
            return Entries.Select(fpkEntry => fpkEntry.Export(input));
        }

        public override void Write(Stream output, AbstractDirectory inputDirectory)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            const int headerSize = 48;
            int indicesSize = 48*Entries.Count;
            int referenceSize = 16*References.Count;

            long startPosition = output.Position;
            output.Position += headerSize + indicesSize + referenceSize;

            foreach (var fpkEntry in Entries)
            {
                fpkEntry.WriteFilePath(output);
            }
            foreach (var fpkReference in References)
            {
                fpkReference.WriteFilePath(output);
            }
            output.AlignWrite(16, 0x00);

            foreach (var fpkEntry in Entries)
            {
                fpkEntry.WriteData(output, inputDirectory);
                output.AlignWrite(16, 0x00);
            }

            uint fileSize = (uint) output.Position;

            output.Position = startPosition;

            writer.Write(0x66786f66); // foxf
            writer.Write((ushort) 0x6B70); //pk
            writer.Write((byte) FpkType);
            writer.Write((byte) 0x73); // s
            writer.Write((ushort) 0x6574); // te
            writer.Write(fileSize);
            writer.WriteZeros(18);
            writer.Write(0x00000002);
            writer.Write(Entries.Count);
            writer.Write(References.Count);
            writer.WriteZeros(4);

            foreach (var fpkEntry in Entries)
            {
                fpkEntry.Write(output);
            }
            foreach (var fpkReference in References)
            {
                fpkReference.Write(output);
            }
        }
    }
}
