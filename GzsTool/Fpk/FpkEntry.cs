using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Utility;

namespace GzsTool.Fpk
{
    [XmlType("Entry", Namespace = "Fpk")]
    public class FpkEntry
    {
        public FpkEntry()
        {
            FilePathFpkString = new FpkString();
        }

        [XmlIgnore]
        public byte[] Data { get; set; }

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

        public bool ShouldSerializeFilePath()
        {
            return FilePathFpkString.ValueEncrypted;
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

            long endPosition = input.Position;
            input.Position = DataOffset;
            Data = reader.ReadBytes(DataSize);
            input.Position = endPosition;
        }

        private string GetFpkEntryFileName()
        {
            string fileName = FilePathFpkString.Value;
            fileName = fileName.Replace("/", "\\");
            int index = fileName.IndexOf(":", StringComparison.Ordinal);
            if (index != -1)
            {
                fileName = fileName.Substring(index + 1, fileName.Length - index - 1);
            }
            fileName = fileName.StartsWith("\\") ? fileName.Substring(1, fileName.Length - 1) : fileName;
            return fileName;
        }

        public void WriteFilePath(Stream output)
        {
            if (Md5Hash == null)
                Md5Hash = Hashing.Md5HashText(FilePath);
            FilePathFpkString.WriteString(output);
        }

        public void WriteData(Stream output, string directory)
        {
            DataOffset = (uint) output.Position;
            string path = Path.Combine(directory, GetFpkEntryFileName());
            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                input.CopyTo(output);
                DataSize = (int) input.Position;
            }
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

        public FileDataContainer Export()
        {
            FileDataContainer container = new FileDataContainer
            {
                Data = Data,
                FileName = GetFpkEntryFileName()
            };
            return container;
        }
    }
}
