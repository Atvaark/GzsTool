using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace GzsTool.Gzs
{
    [XmlRoot("GzsFile")]
    public class GzsFile
    {
        public GzsFile()
        {
            Entries = new List<GzsArchiveEntry>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Entries")]
        public List<GzsArchiveEntry> Entries { get; private set; }

        public static GzsFile ReadGzsFile(Stream input)
        {
            GzsFile gzsFile = new GzsFile();
            gzsFile.Read(input);
            return gzsFile;
        }

        private void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            input.Seek(-4, SeekOrigin.End);
            int footerSize = reader.ReadInt32();
            input.Seek(-20, SeekOrigin.Current);
            if (footerSize != 20)
                throw new Exception("Invalid gzs footer.");
            GzsArchiveFooter footer = GzsArchiveFooter.ReadGzArchiveFooter(input);
            input.Seek(16*footer.Unknown2, SeekOrigin.Begin);
            for (int i = 0; i < footer.ArchiveEntryCount; i++)
            {
                Entries.Add(GzsArchiveEntry.ReadGzArchiveEntry(input));
            }
        }

        public void ExportFiles(FileStream input, string outputDirectory)
        {
            foreach (var entry in Entries)
            {
                entry.ExportFile(input, outputDirectory);
            }
        }
    }
}
