using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Utility;

namespace GzsTool.Core.Fpk
{
    [XmlType("Entry", Namespace = "Fpk")]
    public class FpkEntry
    {
        public FpkEntry()
        {
            FilePathFpkString = new FpkString();
        }

        [XmlIgnore]
        public uint DataOffset { get; set; }

        [XmlIgnore]
        public int DataSize { get; set; }

        [XmlIgnore]
        public FpkString FilePathFpkString { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath
        {
            get { return FilePathFpkString.Value; }
            set { FilePathFpkString.Value = value; }
        }

        [XmlAttribute("Hash")]
        public byte[] Md5Hash { get; set; }

        [XmlAttribute("EncryptedFilePath")]
        public byte[] EncryptedFilePath
        {
            get { return FilePathFpkString.EncryptedValue; }
            set { FilePathFpkString.EncryptedValue = value; }
        }

        public bool ShouldSerializeMd5Hash()
        {
            return FilePathFpkString.ValueResolved == false;
        }

        public static FpkEntry ReadFpkEntry(Stream input)
        {
            FpkEntry fpkEntry = new FpkEntry();
            fpkEntry.Read(input);
            return fpkEntry;
        }

        private void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            DataOffset = reader.ReadUInt32();
            reader.Skip(4);
            DataSize = reader.ReadInt32();
            reader.Skip(4);
            FpkString fileName = FpkString.ReadFpkString(input);
            Md5Hash = reader.ReadBytes(16);
            fileName.ResolveString(Md5Hash);
            FilePathFpkString = fileName;
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
            byte[] result = new byte[DataSize];
            input.Read(result, 0, DataSize);
            byte[] decryptedResult;
            if (DataSize > 0
                && (result[0] == 0x1B || result[0] == 0x1C)
                && TryDecryptData(result, out decryptedResult))
            {
                result = decryptedResult;
            }

            return new MemoryStream(result);
        }

        private string GetFpkEntryFileName()
        {
            string fileName = Hashing.NormalizeFilePath(FilePathFpkString.Value);

            // Some files are prefixed with a drive letter (e.g. "Z:")
            int index = fileName.IndexOf(":", StringComparison.Ordinal);
            if (index != -1)
            {
                fileName = fileName.Substring(index + 1, fileName.Length - index - 1);
            }

            return fileName;
        }

        public void WriteFilePath(Stream output)
        {
            if (Md5Hash == null)
                Md5Hash = Hashing.Md5HashText(FilePath);
            FilePathFpkString.WriteString(output);
        }

        public void Write(Stream output)
        {
            BinaryWriter writer = new BinaryWriter(output, Encoding.Default, true);
            writer.Write(DataOffset);
            writer.WriteZeros(4);
            writer.Write(DataSize);
            writer.WriteZeros(4);
            FilePathFpkString.Write(output);
            writer.Write(Md5Hash);
        }

        public void WriteData(Stream output, IDirectory inputDirectory)
        {
            DataOffset = (uint)output.Position;
            byte[] data = inputDirectory.ReadFile(GetFpkEntryFileName());
            DataSize = data.Length;
            output.Write(data, 0, data.Length);
        }

        public FileDataStreamContainer Export(Stream input)
        {
            FileDataStreamContainer fileDataStreamContainer = new FileDataStreamContainer
            {
                DataStream = ReadDataLazy(input),
                FileName = GetFpkEntryFileName()
            };
            return fileDataStreamContainer;
        }

        private bool TryDecryptData(byte[] data, out byte[] result)
        {
            result = new byte[data.Length - 1];
            var filename = Path.GetFileName(GetFpkEntryFileName().ToLower());
            var hash = Hashing.HashFileNameLegacy(filename, false);
            var key = BitConverter.GetBytes(~hash);

            for (int i = 0; i < data.Length - 1; i++)
            {
                key[i % sizeof(ulong)] ^= data[i + 1];
                result[i] = key[i % sizeof(ulong)];
            }

            if (result[result.Length - 1] != 0x00)
            {
                return false;
            }

            Array.Resize(ref result, result.Length - 1);
            return true;
        }
    }
}
