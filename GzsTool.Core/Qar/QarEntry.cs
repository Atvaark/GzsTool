using System;
using System.Diagnostics;
using System.IO;
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

        [XmlAttribute("Encryption")]
        public uint Encryption { get; set; }

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

        [XmlAttribute("DataHash")]
        public byte[] DataHash { get; set; }

        public bool ShouldSerializeHash()
        {
            return FileNameFound == false;
        }

        public bool ShouldSerializeKey()
        {
            return Key != 0;
        }

        public bool ShouldSerializeEncryption()
        {
            return Encryption != 0;
        }

        public bool ShouldSerializeMetaFlag()
        {
            return MetaFlag;
        }

        public bool ShouldSerializeDataHash()
        {
            return DataHash != null;
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

            byte[] header = new byte[8];
            using (Stream headerStream = new Decrypt1Stream(reader.BaseStream, (int)Version, header.Length, DataHash, hashLow: (uint)(Hash & 0xFFFFFFFF), streamMode: StreamMode.Read))
            {
                headerStream.Read(header, 0, header.Length);
                Encryption = BitConverter.ToUInt32(header, 0);
            }

            if (Encryption == Cryptography.Magic1 || Encryption == Cryptography.Magic2)
            {
                Key = BitConverter.ToUInt32(header, 4);
            }
            else
            {
                Encryption = 0;
            }
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
            Stream stream = new Decrypt1Stream(input, (int)Version, dataSize, DataHash, hashLow: (uint)(Hash & 0xFFFFFFFF), streamMode: StreamMode.Read);

            if (Encryption == Cryptography.Magic1 || Encryption == Cryptography.Magic2)
            {
                int headerSize = Cryptography.GetHeaderSize(Encryption);
                stream.Read(new byte[headerSize], 0, headerSize);
                dataSize -= headerSize;
                stream = new Decrypt2Stream(stream, dataSize, Key, StreamMode.Read);
            }

            if (Compressed)
            {
                stream = Compression.UncompressStream(stream);
            }

            return stream;
        }

        public void Write(Stream output, IDirectory inputDirectory)
        {
            using (Stream inputStream = inputDirectory.ReadFileStream(Hashing.NormalizeFilePath(FilePath)))
            using (Md5Stream md5OutputStream = new Md5Stream(output))
            {
                long headerPosition = output.Position;
                const int entryHeaderSize = 32;
                long dataStartPosition = headerPosition + entryHeaderSize;
                output.Position = dataStartPosition;

                uint uncompressedSize = (uint)inputStream.Length;

                Stream outputDataStream = md5OutputStream;
                Stream outputDataStreamCompressed = null;
                if (Compressed)
                {
                    outputDataStreamCompressed = Compression.CompressStream(outputDataStream);
                    outputDataStream = outputDataStreamCompressed;
                }

                if (Encryption != 0)
                {
                    int encryptionHeaderSize = Cryptography.GetHeaderSize(Encryption);
                    if (encryptionHeaderSize >= 8)
                    {
                        byte[] header = new byte[encryptionHeaderSize];
                        Buffer.BlockCopy(BitConverter.GetBytes(Encryption), 0, header, 0, sizeof(uint));
                        Buffer.BlockCopy(BitConverter.GetBytes(Key), 0, header, 4, sizeof(uint));
                        if (encryptionHeaderSize == 16)
                        {
                            Buffer.BlockCopy(BitConverter.GetBytes(uncompressedSize), 0, header, 8, sizeof(uint));
                            Buffer.BlockCopy(BitConverter.GetBytes(uncompressedSize), 0, header, 12, sizeof(uint));
                        }

                        using (var headerStream = new MemoryStream(header))
                        {
                            headerStream.CopyTo(outputDataStream);
                        }
                    }

                    outputDataStream = new Decrypt2Stream(outputDataStream, (int)uncompressedSize, Key, StreamMode.Write);
                }

                inputStream.CopyTo(outputDataStream);
                outputDataStreamCompressed?.Close();

                // TODO: HACK to support repacked files
                if (DataHash == null)
                {
                    md5OutputStream.Flush();
                    DataHash = md5OutputStream.Hash;
                }

                long dataEndPosition = output.Position;
                uint compressedSize = (uint)(dataEndPosition - dataStartPosition);
                uncompressedSize = Compressed ? uncompressedSize : compressedSize;
                using (var decrypt1Stream = new Decrypt1Stream(output, (int)Version, (int)compressedSize, DataHash, hashLow: (uint)(Hash & 0xFFFFFFFF), streamMode: StreamMode.Write))
                {				
                    CopyTo(output, decrypt1Stream, dataStartPosition, dataEndPosition);
                }

                output.Position = headerPosition;

                const ulong xorMask1Long = 0x4144104341441043;
                const uint xorMask1 = 0x41441043;
                const uint xorMask2 = 0x11C22050;
                const uint xorMask3 = 0xD05608C3;
                const uint xorMask4 = 0x532C7319;
                BinaryWriter writer = new BinaryWriter(output, Encoding.ASCII, true);
                writer.Write(Hash ^ xorMask1Long);
                writer.Write((Version != 2 ? uncompressedSize : compressedSize) ^ xorMask2);
                writer.Write((Version != 2 ? compressedSize : uncompressedSize) ^ xorMask3);
                writer.Write(BitConverter.ToUInt32(DataHash, 0) ^ xorMask4);
                writer.Write(BitConverter.ToUInt32(DataHash, 4) ^ xorMask1);
                writer.Write(BitConverter.ToUInt32(DataHash, 8) ^ xorMask1);
                writer.Write(BitConverter.ToUInt32(DataHash, 12) ^ xorMask2);

                output.Position = dataEndPosition;
            }
        }

        private void CopyTo(
            Stream input,
            Stream output,
            long dataStartPosition,
            long dataEndPosition)
        {
            long offset = dataStartPosition;
            byte[] buffer = new byte[4096];
            while (offset < dataEndPosition)
            {
                int blockSize = 4096;
                int remainingSize = (int) (dataEndPosition - offset);
                if (remainingSize < blockSize)
                {
                    blockSize = remainingSize;
                }
                
                input.Position = offset;
                input.Read(buffer, 0, blockSize);
                
                output.Position = offset;
                output.Write(buffer, 0, blockSize);

                offset += blockSize;
            }
        }
    }
}