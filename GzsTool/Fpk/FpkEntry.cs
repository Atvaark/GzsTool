using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace GzsTool.Fpk
{
    [XmlType("Entry")]
    public class FpkEntry
    {
        [XmlIgnore]
        public byte[] Data { get; set; }

        [XmlIgnore]
        public uint OffsetData { get; set; }

        [XmlIgnore]
        public int SizeDaza { get; set; }

        [XmlElement]
        public FpkString FileName { get; set; }

        [XmlElement("Hash")]
        public byte[] Md5Hash { get; set; }

        [XmlIgnore]
        public bool FileNameFound { get; set; }

        public bool ShouldSerializeMd5Hash()
        {
            return FileNameFound == false;
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
            OffsetData = reader.ReadUInt32();
            reader.Skip(4);
            SizeDaza = reader.ReadInt32();
            reader.Skip(4);
            FpkString fileName = FpkString.ReadFpkString(input);
            Md5Hash = reader.ReadBytes(16);
            FileNameFound = fileName.ResolveString(Md5Hash);
            FileName = fileName;

            long endPosition = input.Position;
            input.Position = OffsetData;
            Data = reader.ReadBytes(SizeDaza);
            input.Position = endPosition;
        }

        public string GetFpkEntryFileName()
        {
            string fileName = FileName.Value;


            fileName = fileName.Replace("/", "\\");
            int index = fileName.IndexOf(":", StringComparison.Ordinal);
            if (index != -1)
            {
                fileName = fileName.Substring(index + 1, fileName.Length - index - 1);
            }
            fileName = fileName.StartsWith("\\") ? fileName.Substring(1, fileName.Length - 1) : fileName;
            return fileName;
        }
    }
}
