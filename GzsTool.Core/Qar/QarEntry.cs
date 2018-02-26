using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Crypto;
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
            int dataSize = (int)CompressedSize;

            byte[] header = new byte[16];
            uint magicEntry;
            using (Stream headerStream = new Decrypt1Stream(input, (int)Version, dataSize, DataHash, hashLow: (uint)(Hash & 0xFFFFFFFF)))
            {
                headerStream.Read(header, 0, 4);
                magicEntry = BitConverter.ToUInt32(header, 0);
            }
            
            input.Position = DataOffset;
            Stream stream = new Decrypt1Stream(input, (int)Version, dataSize, DataHash, hashLow: (uint)(Hash & 0xFFFFFFFF));
            
            if (magicEntry == 0xA0F8EFE6)
            {
                stream.Read(header, 0, 8);
                dataSize -= 8;
                Key = BitConverter.ToUInt32(header, 4);

                stream = new Decrypt2Stream(stream, dataSize, Key);
            }
            else if (magicEntry == 0xE3F8EFE6)
            {
                stream.Read(header, 0, 16);
                dataSize -= 16;
                Key = BitConverter.ToUInt32(header, 4);

                stream = new Decrypt2Stream(stream, dataSize, Key);
            }
            
            if (Compressed)
            {
                stream = Compression.UncompressStream(stream);
            }

            return stream;
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

            Encryption.Decrypt1(data, hashLow: (uint) (Hash & 0xFFFFFFFF), version: Version, dataHash: DataHash);
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