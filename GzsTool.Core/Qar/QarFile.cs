using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;

namespace GzsTool.Core.Qar
{
    [XmlType("QarFile")]
    public class QarFile : ArchiveFile
    {
        private const int QarMagicNumber = 0x52415153;
        
        [XmlAttribute("Flags")]
        public uint Flags { get; set; }

        /// <summary>
        ///     MGSV: 1
        ///     MGS: 2
        /// </summary>
        [XmlAttribute("Version")]
        public uint Version { get; set; }

        [XmlArray("Entries")]
        public List<QarEntry> Entries { get; set; }
        
        public static bool IsQarFile(Stream input)
        {
            long startPosition = input.Position;
            BinaryReader reader = new BinaryReader(input, Encoding.ASCII, true);
            int magicNumber = reader.ReadInt32();
            input.Position = startPosition;
            return magicNumber == QarMagicNumber;
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
            Version = reader.ReadUInt32() ^ xorMask1; // 1 2
            uint unknown2 = reader.ReadUInt32() ^ xorMask2; // 0

            // Determines the alignment block size.
            int blockShiftBits = (Flags & 0x800) > 0 ? 12 : 10;

            byte[] sectionsData = reader.ReadBytes((int)(8 * fileCount));
            ulong[] sections = DecryptSectionList(fileCount, sectionsData, Version);
            byte[] unknownSectionData = reader.ReadBytes((int)(16 * unknownCount));

            List<QarEntry> entries = new List<QarEntry>();
            foreach (var section in sections)
            {
                ulong sectionBlock = section >> 40;
                ulong hash = section & 0xFFFFFFFFFF;
                ulong sectionOffset = sectionBlock << blockShiftBits;
                reader.BaseStream.Position = (long)sectionOffset;

                var entry = new QarEntry();
                entry.Read(reader, Version);
                entries.Add(entry);
            }
            Entries = entries;
        }

        private static ulong[] DecryptSectionList(uint fileCount, byte[] sections, uint version, bool encrypt = false)
        {
            uint[] xorTable = 
            {
                0x41441043,
                0x11C22050,
                0xD05608C3,
                0x532C7319
            };
            
            ulong[] result = new ulong[fileCount];

            if (version != 2)
            {
                for (int i = 0; i < result.Length; i += 1)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    uint i1 = BitConverter.ToUInt32(sections, offset1);
                    uint i2 = BitConverter.ToUInt32(sections, offset2);
                    int index1 = (i + (offset1 / 5)) % 4;
                    int index2 = (i + (offset2 / 5)) % 4;
                    i1 ^= xorTable[index1];
                    i2 ^= xorTable[index2];

                    result[i] = (ulong)i2 << 32 | i1;
                }
            }
            else
            {
                uint xor = 0xA2C18EC3;
                for (int i = 0; i < result.Length; i += 1)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    uint section1 = BitConverter.ToUInt32(sections, offset1);
                    uint section2 = BitConverter.ToUInt32(sections, offset2);
                    uint index1 = (uint)((xor + (offset1 / 5)) % 4);
                    uint index2 = (uint)((xor + (offset2 / 5)) % 4);
                    uint i1 = section1 ^ xorTable[index1];
                    uint i2 = section2 ^xorTable[index2];
                    result[i] = (ulong)i2 << 32 | i1;
                    
                    if (encrypt)
                    {
                        i1 = section1;
                        i2 = section2;
                    }
                    
                    int rotation = (int)(i2 / 256) % 19;
                    uint rotated = (i1 >> rotation) | (i1 << (32 - rotation)); // ROR
                    xor ^= rotated;
                }
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
                QarEntry entry = Entries[i];
                entry.CalculateHash();
                ulong section = (ulong) (output.Position >> shift) << 40
                                | (entry.Hash & 0xFF) << 32
                                | entry.Hash >> 32 & 0xFFFFFFFFFF;
                sections[i] = section;
                entry.Write(output, inputDirectory);
                output.AlignWrite(alignment, 0x00);
            }
            long endPosition = output.Position;
            uint endPositionHead = (uint) (endPosition >> shift);

            output.Position = headerPosition;
            writer.Write(QarMagicNumber); // SQAR
            writer.Write(Flags ^ xorMask1);
            writer.Write((uint)Entries.Count ^ xorMask2);
            writer.Write(xorMask3); // unknown count (not saved in the xml and output directory)
            writer.Write(endPositionHead ^ xorMask4);
            writer.Write((uint)dataOffset ^ xorMask1);
            writer.Write(Version ^ xorMask1);
            writer.Write(0 ^ xorMask2);

            output.Position = tableOffset;
            byte[] encryptedSectionsData = EncryptSections(sections);
            writer.Write(encryptedSectionsData);

            output.Position = endPosition;
        }
        
        private byte[] EncryptSections(ulong[] sections)
        {
            int bufferLength = Buffer.ByteLength(sections);
            byte[] sectionsData = new byte[bufferLength];
            Buffer.BlockCopy(sections, 0, sectionsData, 0, bufferLength);
            ulong[] encryptedSections = DecryptSectionList((uint)Entries.Count, sectionsData, Version, encrypt: true);
            byte[] encryptedSectionsData = new byte[bufferLength];
            Buffer.BlockCopy(encryptedSections, 0, encryptedSectionsData, 0, bufferLength);
            return encryptedSectionsData;
        }
    }
}
