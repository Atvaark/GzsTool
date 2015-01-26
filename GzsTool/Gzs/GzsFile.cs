using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;

namespace GzsTool.Gzs
{
    [XmlType("GzsFile")]
    public class GzsFile : ArchiveFile
    {
        public GzsFile()
        {
            Entries = new List<GzsEntry>();
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlArray("Entries")]
        public List<GzsEntry> Entries { get; private set; }

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
            input.Seek(16*footer.EntryBlockOffset, SeekOrigin.Begin);
            for (int i = 0; i < footer.ArchiveEntryCount; i++)
            {
                Entries.Add(GzsEntry.ReadGzArchiveEntry(input));
            }
        }

        public IEnumerable<FileDataContainer> ExportFiles(Stream input)
        {
            return Entries.Select(gzsEntry => gzsEntry.Export(input));
        }

        public void Write(Stream output, string inputDirectory)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            foreach (var gzsEntry in Entries)
            {
                gzsEntry.WriteData(output, inputDirectory);
                // TODO: Write the key instead of 8 zeros.
                output.AlignWrite(16, 0x00);
            }

            var entryBlockOffset = (int) (output.Position/16);

            foreach (var gzsEntry in Entries)
            {
                gzsEntry.CalculateHash();
                gzsEntry.Write(output);
            }

            uint sizeSum = (uint) Entries.Sum(e => e.Size);
            writer.Write(sizeSum);
            output.AlignWrite(16, 0x00);

            GzsArchiveFooter footer = new GzsArchiveFooter
            {
                ArchiveEntryCount = Entries.Count,
                EntryBlockOffset = entryBlockOffset
            };

            footer.Write(output);
        }
    }
}
