using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Common.Interfaces;
using GzsTool.Utility;

namespace GzsTool.Gzs
{
    [XmlType("Entry", Namespace = "Gzs")]
    public class GzsEntry
    {
        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlIgnore]
        public bool FileNameFound { get; set; }

        [XmlIgnore]
        public uint DataOffset { get; set; }

        [XmlIgnore]
        public uint DataSize { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        public bool ShouldSerializeHash()
        {
            return FileNameFound == false;
        }

        public static GzsEntry ReadGzsEntry(Stream input)
        {
            GzsEntry gzsEntry = new GzsEntry();
            gzsEntry.Read(input);
            return gzsEntry;
        }

        public void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            Hash = reader.ReadUInt64();
            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            string filePath;
            FileNameFound = TryGetFilePath(out filePath);
            FilePath = filePath;
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
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            input.Position = 16*DataOffset;
            byte[] data = reader.ReadBytes((int) DataSize);
            data = Encryption.DeEncryptQar(data, DataOffset);
            const uint keyConstant = 0xA0F8EFE6;
            uint peekData = BitConverter.ToUInt32(data, 0);
            if (peekData == keyConstant)
            {
                uint key = BitConverter.ToUInt32(data, 4);
                DataSize -= 8;
                byte[] data2 = new byte[data.Length - 8];
                Array.Copy(data, 8, data2, 0, data.Length - 8);
                data = Encryption.DeEncrypt(data2, key);
            }
            return new MemoryStream(data);
        }

        private string GetGzsEntryFileName()
        {
            string filePath = FilePath;
            if (filePath.StartsWith("/"))
                filePath = filePath.Substring(1, filePath.Length - 1);
            filePath = filePath.Replace("/", "\\");
            return filePath;
        }

        private bool TryGetFilePath(out string filePath)
        {
            int fileExtensionId = (int) (Hash >> 52 & 0xFFFF);
            bool fileNameFound = Hashing.TryGetFileNameFromHash(Hash, fileExtensionId, out filePath);
            return fileNameFound;
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            DataOffset = (uint) output.Position/16;
            byte[] data = inputDirectory.ReadFile(GetGzsEntryFileName());
            data = Encryption.DeEncryptQar(data, DataOffset);
            // TODO: Encrypt the data if a key is set for the entry.
            DataSize = (uint) data.Length;
            output.Write(data, 0, data.Length);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(Hash);
            writer.Write(DataOffset);
            writer.Write(DataSize);
        }

        public void CalculateHash()
        {
            if (Hash == 0)
                Hash = Hashing.HashFileNameWithExtension(FilePath);
        }

        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = GetGzsEntryFileName()
            };
            return fileDataStreamContainer;
        }
    }
}
