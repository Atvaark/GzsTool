using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;

namespace GzsTool.Qar
{
    public class QarFile : ArchiveFile
    {
        public string Name { get; set; }
        public uint Flags { get; set; }

        [XmlArray("Entries")]
        public List<QarEntry> Entries { get; private set; }

        public static QarFile ReadQarFile(FileStream input)
        {
            QarFile qarFile = new QarFile();
            qarFile.Read(input);
            return qarFile;
        }

        public override void Read(Stream input)
        {
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;

            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            uint magicNumber = reader.ReadUInt32(); // SQAR
            Flags = reader.ReadUInt32() ^ xorMask1;
            uint fileCount = reader.ReadUInt32() ^ xorMask2;
            uint unknownCount = reader.ReadUInt32() ^ xorMask3;
            uint unknown3 = reader.ReadUInt32() ^ xorMask4;
            uint unknown4 = reader.ReadUInt32() ^ xorMask1;
            uint unknown5 = reader.ReadUInt32() ^ xorMask1;
            uint unknown6 = reader.ReadUInt32() ^ xorMask2;

            int shift = (Flags & 0x800) > 0 ? 12 : 10;

            byte[] sectionsData = reader.ReadBytes((int)(8 * fileCount));
            ulong[] sections = DecryptSectionList(fileCount, sectionsData);
            byte[] unknownSectionData = reader.ReadBytes((int)(16 * unknownCount));

            List<QarEntry> entries = new List<QarEntry>();
            foreach (var section in sections)
            {
                ulong sectionOffset = section >> 40 << shift;
                reader.BaseStream.Position = (long)sectionOffset;

                var entry = new QarEntry();
                entry.Read(reader);
                entries.Add(entry);
            }
            Entries = entries;
        }

        private static ulong[] DecryptSectionList(uint fileCount, byte[] sections)
        {
            uint[] decryptionTable = 
            {
                0x41441043,
                0x11C22050,
                0xD05608C3,
                0x532C7319,
                //0x97CB3127,
                //0xC3A5C85C,
                //0xBE98F273,
                //0xB492B66F
            };

            ulong[] result = new ulong[fileCount];
            for (int i = 0; i < result.Length; i += 1)
            {
                int offset1 = i * sizeof(ulong);
                int offset2 = i * sizeof(ulong) + sizeof(uint);
                uint i1 = BitConverter.ToUInt32(sections, offset1);
                uint i2 = BitConverter.ToUInt32(sections, offset2);
                int decryptIndex1 = (i + (offset1 / 5)) % 4;
                int decryptIndex2 = (i + (offset2 / 5)) % 4;
                i1 ^= decryptionTable[decryptIndex1];
                i2 ^= decryptionTable[decryptIndex2];
                result[i] = (ulong)i2 << 32 | i1;
            }
            return result;
        }

        public override IEnumerable<FileDataStreamContainer> ExportFiles(Stream input)
        {
            return Entries.Select(gzsEntry => gzsEntry.Export(input));
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            throw new System.NotImplementedException();
        }
    }
}
