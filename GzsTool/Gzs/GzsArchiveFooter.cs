using System.IO;
using System.Text;

namespace GzsTool.Gzs
{
    public class GzsArchiveFooter
    {
        public int ArchiveEntryCount { get; set; }
        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }
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
                Unknown1 = reader.ReadInt32();
                Unknown2 = reader.ReadInt32();
                Unknown3 = reader.ReadInt32();
                FooterSize = reader.ReadInt32();
            }
        }
    }
}
