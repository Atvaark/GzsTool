using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Utility;

namespace GzsTool.Core.Qar
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

        [XmlAttribute("Compressed")]
        public bool Compressed { get; set; }

        [XmlAttribute("MetaFlag")]
        public bool MetaFlag { get; set; }

        [XmlAttribute("Version")]
        public uint Version { get; set; }

        [XmlIgnore]
        public bool FileNameFound { get; set; }

        [XmlIgnore]
        public uint UncompressedSize { get; private set; }

        [XmlIgnore]
        public uint CompressedSize { get; private set; }

        [XmlIgnore]
        public long DataOffset { get; set; }

        [XmlIgnore]
        public byte[] DataHash { get; set; }

        public bool ShouldSerializeHash()
        {
            return FileNameFound == false;
        }

        public bool ShouldSerializeKey()
        {
            return Key != 0;
        }

        public bool ShouldSerializeMetaFlag()
        {
            return MetaFlag;
        }

        public void CalculateHash()
        {
            if (Hash == 0)
            {
                Hash = Hashing.HashFileNameWithExtension(FilePath);
            }
            else
            {
                DebugAssertHashMatches();
            }

            if (MetaFlag)
            {
                Hash = Hash | Hashing.MetaFlag;
            }
        }

        [Conditional("DEBUG")]
        private void DebugAssertHashMatches()
        {
            ulong newHash = Hashing.HashFileNameWithExtension(FilePath);
            if (Hash != newHash)
            {
                Debug.WriteLine("Hash mismatch '{0}' {1:x}!={2:x}", FilePath, newHash, Hash);
            }
        }

        public void Read(BinaryReader reader, uint version)
        {
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;

            uint hashLow = reader.ReadUInt32() ^ xorMask1;
            uint hashHigh = reader.ReadUInt32() ^ xorMask1;
            Hash = (ulong) hashHigh << 32 | hashLow;
            MetaFlag = (Hash & Hashing.MetaFlag) > 0;
            uint size1 = reader.ReadUInt32() ^ xorMask2;
            uint size2 = reader.ReadUInt32() ^ xorMask3;
            Version = version;
            UncompressedSize = Version != 2 ? size1 : size2;
            CompressedSize = Version != 2 ? size2 : size1;
            Compressed = UncompressedSize != CompressedSize;

            uint md51 = reader.ReadUInt32() ^ xorMask4;
            uint md52 = reader.ReadUInt32() ^ xorMask1;
            uint md53 = reader.ReadUInt32() ^ xorMask1;
            uint md54 = reader.ReadUInt32() ^ xorMask2;
            byte[] md5Hash = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(md51), 0, md5Hash, 0, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(md52), 0, md5Hash, 4, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(md53), 0, md5Hash, 8, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(md54), 0, md5Hash, 12, sizeof(uint));
            DataHash = md5Hash;

            string filePath;
            FileNameFound = Hashing.TryGetFileNameFromHash(Hash, out filePath);
            FilePath = filePath;
            DataOffset = reader.BaseStream.Position;
        }

        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = Hashing.NormalizeFilePath(FilePath)
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
            
            int dataSize = (int)CompressedSize;
            byte[] data = reader.ReadBytes(dataSize);
            Decrypt1(data, hashLow: (uint) (Hash & 0xFFFFFFFF));
            uint magicEntry = BitConverter.ToUInt32(data, 0);
            if (magicEntry == 0xA0F8EFE6)
            {
                const int headerSize = 8;
                Key = BitConverter.ToUInt32(data, 4);
                dataSize -= headerSize;
                byte[] newData = new byte[dataSize];
                Array.Copy(data, headerSize, newData, 0, dataSize);
                Decrypt2(newData, Key);
                data = newData;
            }
            else if (magicEntry == 0xE3F8EFE6)
            {
                const int headerSize = 16;
                Key = BitConverter.ToUInt32(data, 4);
                dataSize -= headerSize;
                byte[] newData = new byte[dataSize];
                Array.Copy(data, headerSize, newData, 0, dataSize);
                Decrypt2(newData, Key);
                data = newData;
            }

            if (Compressed)
            {
                data = Compression.Uncompress(data);
            }

            return new MemoryStream(data);
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
            if (Version != 2)
            {
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int index = (int) (2 * ((hashLow + offset1 / 11) % 4));
                    uint u1 = BitConverter.ToUInt32(sectionData, offset1) ^ decryptionTable[index];
                    uint u2 = BitConverter.ToUInt32(sectionData, offset2) ^ decryptionTable[index + 1];
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, sectionData, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, sectionData, offset2, sizeof(uint));
                }

                int remaining = sectionData.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int index = (int) (2 * ((hashLow + (offset - (offset % sizeof(long))) / 11) % 4));
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte) ((xorMask >> (8 * decryptionIndex)) & 0xff);
                    byte b1 = (byte) (sectionData[offset] ^ xorMaskByte);
                    sectionData[offset] = b1;
                }
            }
            else
            {
                ulong seed = BitConverter.ToUInt64(DataHash, (int) (hashLow % 2) * 8);
                uint seedLow = (uint) seed & 0xFFFFFFFF;
                uint seedHigh = (uint) (seed >> 32);
                for (int i = 0; i < blocks; i++)
                {
                    int offset1 = i * sizeof(ulong);
                    int offset2 = i * sizeof(ulong) + sizeof(uint);
                    int index = 2 * (int) ((hashLow + seed + (ulong) (offset1 / 11)) % 4);
                    uint u1 = BitConverter.ToUInt32(sectionData, offset1) ^ decryptionTable[index] ^ seedLow;
                    uint u2 = BitConverter.ToUInt32(sectionData, offset2) ^ decryptionTable[index + 1] ^ seedHigh;
                    Buffer.BlockCopy(BitConverter.GetBytes(u1), 0, sectionData, offset1, sizeof(uint));
                    Buffer.BlockCopy(BitConverter.GetBytes(u2), 0, sectionData, offset2, sizeof(uint));
                }

                int remaining = sectionData.Length % sizeof(ulong);
                for (int i = 0; i < remaining; i++)
                {
                    int offset = blocks * sizeof(long) + i * sizeof(byte);
                    int offsetBlock = offset - (offset % sizeof(long));
                    int index = 2 * (int) ((hashLow + seed + (ulong) (offsetBlock / 11)) % 4);
                    int decryptionIndex = offset % sizeof(long);
                    uint xorMask = decryptionIndex < 4 ? decryptionTable[index] : decryptionTable[index + 1];
                    byte xorMaskByte = (byte) ((xorMask >> (8 * (decryptionIndex%4))) & 0xff);
                    uint seedMask = decryptionIndex < 4 ? seedLow : seedHigh;
                    byte seedByte = (byte) ((seedMask >> (8 * (decryptionIndex%4))) & 0xff);
                    sectionData[offset] = (byte) (sectionData[offset] ^ (byte) (xorMaskByte ^ seedByte));
                }
            }
        }

        private unsafe void Decrypt2(byte[] input, uint key)
        {
            int size = input.Length;
            uint currentKey = key | ((key ^ 25974) << 16);

            byte[] output = input.ToArray();
            fixed (byte* pDestBase = output, pSrcBase = input)
            {
                uint* pDest = (uint*) pDestBase;
                uint* pSrc = (uint*) pSrcBase;
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
                    } while (j > 0);
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

        public void Write(Stream output, IDirectory inputDirectory)
        {
            const ulong xorMask1Long = 0x4144104341441043;
            const uint xorMask1 = 0x41441043;
            const uint xorMask2 = 0x11C22050;
            const uint xorMask3 = 0xD05608C3;
            const uint xorMask4 = 0x532C7319;

            byte[] data = inputDirectory.ReadFile(Hashing.NormalizeFilePath(FilePath));
            DataHash = Hashing.Md5Hash(data);
            uint uncompressedSize = (uint) data.Length;
            uint compressedSize;
            if (Compressed)
            {
                data = Compression.Compress(data);
                compressedSize = (uint) data.Length;
            }
            else
            {
                compressedSize = uncompressedSize;
            }

            Decrypt1(data, hashLow: (uint) (Hash & 0xFFFFFFFF));
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Hash ^ xorMask1Long);
            writer.Write((Version != 2 ? uncompressedSize : compressedSize) ^ xorMask2);
            writer.Write((Version != 2 ? compressedSize : uncompressedSize) ^ xorMask3);

            writer.Write(BitConverter.ToUInt32(DataHash, 0) ^ xorMask4);
            writer.Write(BitConverter.ToUInt32(DataHash, 4) ^ xorMask1);
            writer.Write(BitConverter.ToUInt32(DataHash, 8) ^ xorMask1);
            writer.Write(BitConverter.ToUInt32(DataHash, 12) ^ xorMask2);

            // TODO: Maybe reencrypt the lua files.
            writer.Write(data);
        }
    }
}