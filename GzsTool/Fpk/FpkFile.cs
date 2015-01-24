using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace GzsTool.Fpk
{
    [XmlRoot("FpkFile")]
    public class FpkFile
    {
        public FpkFile()
        {
            Entries = new List<FpkEntry>();
            References = new List<FpkReference>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

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

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);

            uint magicNumber1 = reader.ReadUInt32(); // foxf
            uint magicNumber2 = reader.ReadUInt32(); // pk_x pk_p
            uint magicNumber3 = reader.ReadUInt32(); // 3s__ 63__
            reader.Skip(24);
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

        public void ExportEntries(string outputDirectory)
        {
            foreach (var entry in Entries)
            {
                string fileName = entry.GetFpkEntryFileName();
                string outputPath = Path.Combine(outputDirectory, fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using (FileStream output = new FileStream(outputPath, FileMode.Create))
                {
                    output.Write(entry.Data, 0, entry.Data.Length);
                }
            }
        }
    }
}
