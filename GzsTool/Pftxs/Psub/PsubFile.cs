using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GzsTool.Pftxs.Psub
{
    public class PsubFile
    {
        private const int MagicNumber = 0x42555350; // PSUB
        private readonly List<PsubFileEntry> _entries;

        public PsubFile()
        {
            _entries = new List<PsubFileEntry>();
        }

        public IEnumerable<PsubFileEntry> Entries
        {
            get { return _entries; }
        }

        public static PsubFile ReadPsubFile(Stream input)
        {
            PsubFile psubFile = new PsubFile();
            psubFile.Read(input);
            return psubFile;
        }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            int magicNumber = reader.ReadInt32();
            int entryCount = reader.ReadInt32();
            for (int i = 0; i < entryCount; i++)
            {
                PsubFileEntry entry = PsubFileEntry.ReadPsubFileEntry(input);
                AddPsubFileEntry(entry);
            }
            input.AlignRead(16);
            foreach (var entry in Entries)
            {
                entry.Data = reader.ReadBytes(entry.Size);
                input.AlignRead(16);
            }
        }

        public void AddPsubFileEntry(PsubFileEntry entry)
        {
            _entries.Add(entry);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(MagicNumber);
            writer.Write(Entries.Count());
            long entryPosition = output.Position;
            output.Position += PsubFileEntry.PsubFileEntrySize*Entries.Count();
            output.AlignWrite(16, 0xCC);
            foreach (var entry in Entries)
            {
                entry.Offset = Convert.ToInt32(output.Position);
                entry.Size = entry.Data.Length;
                entry.WriteData(output);
                output.AlignWrite(16, 0xCC);
            }
            long endPosition = output.Position;
            output.Position = entryPosition;
            foreach (var entry in Entries)
            {
                entry.Write(output);
            }
            output.Position = endPosition;
        }
    }
}
