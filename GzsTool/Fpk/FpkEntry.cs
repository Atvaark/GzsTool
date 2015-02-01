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

        private byte[] ReadData(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            long startPosition = input.Position;
            input.Position = DataOffset;
            byte[] data = reader.ReadBytes(DataSize);
            input.Position = startPosition;
            return data;
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

        public void WriteData(Stream output, string inputDirectory)
        {
            DataOffset = (uint) output.Position;
            string path = Path.Combine(inputDirectory, GetFpkEntryFileName());
            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                input.CopyTo(output);
                DataSize = (int) input.Position;
            }
        }

        public FileDataContainer Export(Stream input)
        {
            FileDataContainer container = new FileDataContainer
            {
                Data = ReadData(input),
                FileName = GetFpkEntryFileName()
            };
            return container;
        }
    }
}
