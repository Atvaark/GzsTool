using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GzsTool.Gzs
{
    public class GzsFile
    {
        private readonly List<GzsArchiveEntry> _entries;

        public GzsFile()
        {
            _entries = new List<GzsArchiveEntry>();
        }

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
                _entries.Add(GzsArchiveEntry.ReadGzArchiveEntry(input));
            }
        }

        public void ExportFiles(FileStream input, string outputDirectory)
        {
            foreach (var entry in _entries)
            {
                entry.ExportFile(input, outputDirectory);
            }
        }
    }
}
