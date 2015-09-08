using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;
using GzsTool.Utility;

namespace GzsTool.Qar
{
    [XmlType("Entry", Namespace = "Qar")]
    public class QarEntry
    {
        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlAttribute("Key")]
        public uint Key { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlIgnore]
        public bool FileNameFound { get; set; }

        [XmlIgnore]
        public uint Size1 { get; private set; }

        [XmlIgnore]
        public uint Size2 { get; private set; }

        [XmlIgnore]
        public uint Unknown1 { get; private set; }

        [XmlIgnore]
        public long DataOffset { get; set; }
        
        public void Read(BinaryReader reader)
        {
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;

            uint hashLow = reader.ReadUInt32() ^ xorMask1;
            uint hashHigh = reader.ReadUInt32() ^ xorMask1;
            Hash = (ulong)hashHigh << 32 | hashLow;
            Size1 = reader.ReadUInt32() ^ xorMask2;
            Size2 = reader.ReadUInt32() ^ xorMask3;

            uint md51 = reader.ReadUInt32() ^ xorMask4;
            uint md52 = reader.ReadUInt32() ^ xorMask1;
            uint md53 = reader.ReadUInt32() ^ xorMask1;
            uint md54 = reader.ReadUInt32() ^ xorMask2;

            string filePath;
            FileNameFound = TryGetFilePath(out filePath);
            FilePath = filePath;

            DataOffset = reader.BaseStream.Position;
        }
        
        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = FilePath
            };
            return fileDataStreamContainer;
        }
        
        private Func<Stream> ReadDataLazy(Stream input)
        {
            return () =>
            {
                lock (input)
                {
                    return ReadData(input);
                }
            };
        }

        private Stream ReadData(Stream input)
        {
            input.Position = DataOffset;
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);

            byte[] sectionData = reader.ReadBytes((int)Size1);
            Decrypt1(sectionData, hashLow: (uint) (Hash & 0xFFFFFFFF));
            uint magicEntry = BitConverter.ToUInt32(sectionData, 0);
            if (magicEntry == 0xA0F8EFE6)
            {
                const int headerSize = 8;
                Key = BitConverter.ToUInt32(sectionData, 4);
                Size1 -= headerSize;
                byte[] newSectionData = new byte[Size1];
                Array.Copy(sectionData, headerSize, newSectionData, 0, Size1);
                Decrypt2(newSectionData, Key);
            }
            else if (magicEntry == 0xE3F8EFE6)
            {
                const int headerSize = 16;
                Key = BitConverter.ToUInt32(sectionData, 4);
                Size1 -= headerSize;
                byte[] newSectionData = new byte[Size1];
                Array.Copy(sectionData, headerSize, newSectionData, 0, Size1);
                Decrypt2(newSectionData, Key);
                sectionData = newSectionData;
            }

            return new MemoryStream(sectionData);
        }
        
        private bool TryGetFilePath(out string filePath)
        {
            return Hashing.TryGetFileNameFromHash(Hash, out filePath);
        }
        
        private void Decrypt1(byte[] sectionData, uint hashLow)
        {
            // TODO: Use a ulong array instead.
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
                byte xorMaskByte = (byte)((xorMask >> (8 * decryptionIndex)) & 0xff);
                byte b1 = (byte)(sectionData[offset] ^ xorMaskByte);
                sectionData[offset] = b1;
            }
        }

        private unsafe void Decrypt2(byte[] input, uint key)
        {
            int size = input.Length;
            uint currentKey = key | ((key ^ 25974) << 16);

            byte[] output = input.ToArray();
            fixed (byte* pDestBase = output, pSrcBase = input)
            {
                uint* pDest = (uint*)pDestBase;
                uint* pSrc = (uint*)pSrcBase;
                uint i = 278 * key;
                for (; size >= 64; size -= 64)
                {
                    uint j = 16;
                    do
                    {
                        *pDest = currentKey ^ *pSrc;
                        currentKey = i + 48828125 * currentKey;

                        --j;
                        pDest++;
                        pSrc++;
                    }
                    while (j > 0);
                }
                for (; size >= 16; pSrc += 4)
                {
                    *pDest = currentKey ^ *pSrc;
                    uint v7 = i + 48828125 * currentKey;
                    *(pDest + 1) = v7 ^ *(pSrc + 1);
                    uint v8 = i + 48828125 * v7;
                    *(pDest + 2) = v8 ^ *(pSrc + 2);
                    uint v9 = i + 48828125 * v8;
                    *(pDest + 3) = v9 ^ *(pSrc + 3);

                    currentKey = i + 48828125 * v9;
                    size -= 16;
                    pDest += 4;
                }
                for (; size >= 4; pSrc++)
                {
                    *pDest = currentKey ^ *pSrc;

                    currentKey = i + 48828125 * currentKey;
                    size -= 4;
                    pDest++;
                }
            }

            Buffer.BlockCopy(output, 0, input, 0, input.Length);
        }


        private string GetQarEntryFilePath()
        {
            string filePath = FilePath;
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1, filePath.Length - 1);
            filePath = filePath.Replace("/", "\\");
            return filePath;
        }

        public void Write(Stream output, IDirectory inputDirectory)
        {
            const ulong xorMask1Long = 0x4144104341441043;
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;
            
            byte[] data = inputDirectory.ReadFile(GetQarEntryFilePath());
            byte[] hash = Hashing.Md5Hash(data);
            Decrypt1(data, hashLow: (uint)(Hash & 0xFFFFFFFF));
            
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Hash ^ xorMask1Long);
            writer.Write(data.Length ^ xorMask2);
            writer.Write(data.Length ^ xorMask3);
            
            writer.Write(BitConverter.ToUInt32(hash, 0) ^ xorMask4);
            writer.Write(BitConverter.ToUInt32(hash, 4) ^ xorMask1);
            writer.Write(BitConverter.ToUInt32(hash, 8) ^ xorMask1);
            writer.Write(BitConverter.ToUInt32(hash, 12) ^ xorMask2);

            // TODO: Encrypt lua data
            writer.Write(data);
        }
    }
}