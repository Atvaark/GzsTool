using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using GzsTool.Common;
using GzsTool.Common.Interfaces;

namespace GzsTool.Qar
{
    public class QarFile : ArchiveFile
    {
        public string Name { get; set; }

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
            uint flags = reader.ReadUInt32() ^ xorMask1;
            uint fileCount = reader.ReadUInt32() ^ xorMask2;
            uint unknownCount = reader.ReadUInt32() ^ xorMask3;
            uint unknown3 = reader.ReadUInt32() ^ xorMask4;
            uint unknown4 = reader.ReadUInt32() ^ xorMask1;
            uint unknown5 = reader.ReadUInt32() ^ xorMask1;
            uint unknown6 = reader.ReadUInt32() ^ xorMask2;

            int shift = (flags & 0x800) > 0 ? 12 : 10;

            byte[] sectionsData = reader.ReadBytes((int)(8 * fileCount));
            ulong[] sections = DecryptSectionList(fileCount, sectionsData);
            byte[] unknownSectionData = reader.ReadBytes((int)(16 * unknownCount));

            foreach (var section in sections)
            {
                ulong sectionOffset = section >> 40 << shift;
                reader.BaseStream.Position = (long)sectionOffset;
                uint hashLow = reader.ReadUInt32() ^ xorMask1;
                uint hashHigh = reader.ReadUInt32() ^ xorMask1;
                uint size1 = reader.ReadUInt32() ^ xorMask2;
                uint size2 = reader.ReadUInt32() ^ xorMask3;
                uint sectionUnknown5 = reader.ReadUInt32() ^ xorMask4;
                uint sectionUnknown6 = reader.ReadUInt32() ^ xorMask1;
                uint sectionUnknown7 = reader.ReadUInt32() ^ xorMask1;
                uint sectionUnknown8 = reader.ReadUInt32() ^ xorMask2;

                byte[] sectionData = reader.ReadBytes((int)size1);
                DecryptSection(sectionData, hashLow);

                uint magicEntry = BitConverter.ToUInt32(sectionData, 0);
                uint key = 0;
                if (magicEntry == 0xA0F8EFE6)
                {
                    key = BitConverter.ToUInt32(sectionData, 4);
                    size1 -= 8;
                    // TODO: Decrypt
                }
                else if (magicEntry == 0xE3F8EFE6)
                {
                    key = BitConverter.ToUInt32(sectionData, 4);
                    size1 -= 16;
                    // TODO: Decrypt
                }

                // TODO: Add these to a property that gets saved in the resulting XML.
            }
        }

        private void DecryptSection(byte[] sectionData, uint hashLow)
        {
            uint[] decryptionTable =
            {
                0xBB8ADEDB,
                0x65229958,
                0x08453206,
                0x88121302,
                0x4C344955,
                0x2C02F10C,
                0x4887F823,
                0xF3818583,
                //0x40C90FDB,
                //0x3FC90FDB,
                //0x3F490FDB,
                //0x3EA2F983,
                //0x3C8EFA35,
                //0x42652EE0,
                //0x40C90FDB,
                //0x3FC90FDB,
                //0x3F490FDB,
                //0x3EA2F983,
                //0x3C8EFA35,
                //0x42652EE0
            };

            int blocks = sectionData.Length / sizeof(ulong);
            for (int i = 0; i < blocks; i++)
            {
                int offset1 = i * sizeof(ulong);
                int offset2 = i * sizeof(ulong) + sizeof(uint);
                int index = (int)(2 * ((hashLow + offset1 / 11) % 4));
                uint u1 = BitConverter.ToUInt32(sectionData, offset1) ^ decryptionTable[index];
                uint u2 = BitConverter.ToUInt32(sectionData, offset2) ^ decryptionTable[index + 1];
                Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, sectionData, offset1, sizeof(uint));
                Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, sectionData, offset2, sizeof(uint));
            }

            int remaining = sectionData.Length % sizeof(ulong);
            for (int i = 0; i < remaining; i++)
            {
                int offset = blocks * sizeof(long) + i * sizeof(byte);
                int index = (int)(2 * ((hashLow + (offset - (offset % sizeof(long))) / 11) % 4));
                int decryptionIndex = offset % sizeof(long);
                uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                byte xorMaskByte = (byte) ((xorMask >> (8 * decryptionIndex)) & 0xff);
                byte b1 = (byte)(sectionData[offset] ^ xorMaskByte);
                sectionData[offset] = b1;
            }
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
            throw new System.NotImplementedException();
        }

        public override void Write(Stream output, IDirectory inputDirectory)
        {
            throw new System.NotImplementedException();
        }
    }
}
