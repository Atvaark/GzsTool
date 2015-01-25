using System.IO;
using System.Text;

namespace GzsTool.Gzs
{
    public class GzsArchiveFooter
    {
        public const int Size = 20;
        public int ArchiveEntryCount { get; set; }
        public int MagicNumber1 { get; set; }
        public int EntryBlockOffset { get; set; }
        public int MagicNumber2 { get; set; }
        public int FooterSize { get; set; }

        public static GzsArchiveFooter ReadGzArchiveFooter(Stream input)
        {
            GzsArchiveFooter gzsArchiveFooter = new GzsArchiveFooter();
            gzsArchiveFooter.Read(input);
            return gzsArchiveFooter;
        }

        public void Read(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, true))
            {
                ArchiveEntryCount = reader.ReadInt32();
                MagicNumber1 = reader.ReadInt32();
                EntryBlockOffset = reader.ReadInt32();
                MagicNumber2 = reader.ReadInt32();
                FooterSize = reader.ReadInt32();
            }
        }

        public void Write(FileStream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(ArchiveEntryCount);
            writer.Write(0x71610000);
            writer.Write(EntryBlockOffset);
            writer.Write(0x00000000);
            writer.Write(Size);
        }
    }
}
