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
    [XmlType("QarFile")]
    public class QarFile : ArchiveFile
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Flags")]
        public uint Flags { get; set; }

        [XmlArray("Entries")]
        public List<QarEntry> Entries { get; set; }

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
            uint blockFileEnd = reader.ReadUInt32() ^ xorMask4;
            uint offsetFirstFile = reader.ReadUInt32() ^ xorMask1;
            uint unknown1 = reader.ReadUInt32() ^ xorMask1; // 1
            uint unknown2 = reader.ReadUInt32() ^ xorMask2; // 0

            // Determines the alignment block size.
            int blockShiftBits = (Flags & 0x800) > 0 ? 12 : 10;

            byte[] sectionsData = reader.ReadBytes((int)(8 * fileCount));
            ulong[] sections = DecryptSectionList(fileCount, sectionsData);
            byte[] unknownSectionData = reader.ReadBytes((int)(16 * unknownCount));

            List<QarEntry> entries = new List<QarEntry>();
            foreach (var section in sections)
            {
                ulong sectionBlock = section >> 40;
                ulong hash = section & 0xFFFFFFFFFF;
                ulong sectionOffset = sectionBlock << blockShiftBits;
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
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;
            const int headerSize = 32;
            int shift = (Flags & 0x800) > 0 ? 12 : 10;
            int alignment = 1 << shift;

            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            long headerPosition = output.Position;
            output.Skip(headerSize);
            long tableOffset = output.Position;
            output.Skip(8 * Entries.Count);
            //long unknownTableOffset = output.Position;
            //output.Skip(16 * UnknownEntries.Count);

            output.AlignWrite(alignment, 0x00);
            long dataOffset = output.Position;
            ulong[] sections = new ulong[Entries.Count];
            for (int i = 0; i < Entries.Count; i++)
            {
                output.AlignWrite(alignment, 0x00);
                QarEntry entry = Entries[i];
                ulong section = (ulong) (output.Position >> shift) << 40
                                | (entry.Hash & 0xFF) << 32
                                | entry.Hash >> 32 & 0xFFFFFFFFFF;
                sections[i] = section;
                entry.Write(output, inputDirectory);
                // TODO: Align 16?
            }
            long endPosition = output.Position;
            uint endPositionHead = (uint) (endPosition >> shift);

            output.Position = headerPosition;
            const uint qarMagicNumber = 0x52415153; // SQAR
            writer.Write(qarMagicNumber);
            writer.Write(Flags ^ xorMask1);
            writer.Write((uint)Entries.Count ^ xorMask2);
            writer.Write(xorMask3); // unknown count (not saved in the xml and output directory)
            writer.Write(endPositionHead ^ xorMask4); // unknown3
            writer.Write((uint)dataOffset ^ xorMask1); // offset first 
            writer.Write(1 ^ xorMask1);
            writer.Write(0 ^ xorMask2);

            // TODO: Refactor DEcrSectionList to take a byte array
            int bufferLenth = Buffer.ByteLength(sections);
            byte[] sectData = new byte[bufferLenth];
            Buffer.BlockCopy(sections, 0, sectData, 0, bufferLenth);
            ulong[] sect2 = DecryptSectionList((uint)Entries.Count, sectData);
            byte[] sectData2 = new byte[bufferLenth];
            Buffer.BlockCopy(sect2, 0, sectData2, 0, bufferLenth);
            output.Position = tableOffset;
            writer.Write(sectData2);

            output.Position = endPosition;
        }
    }
}
